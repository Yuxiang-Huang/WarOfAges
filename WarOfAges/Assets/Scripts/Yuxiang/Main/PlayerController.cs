using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.IO;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public PhotonView PV;

    public static PlayerController instance;

    public int id;

    public bool lost;
    public bool turnEnded;

    public MainBase mainBase;

    [Header("Mouse Interaction")]
    Tile highlighted;
    public string mode;
    public IUnit unitSelected;
    public SpawnInfo spawnInfoSelected;

    [Header("Belongings")]
    public List<Troop> allTroops = new List<Troop>();
    public List<Ship> allShips = new List<Ship>();
    public List<Building> allBuildings = new List<Building>();
    public List<Spell> allSpells = new List<Spell>();

    public HashSet<Tile> territory = new HashSet<Tile>();
    public int landTerritory;
    public HashSet<Tile> visibleTiles = new HashSet<Tile>();

    public int[,] extraViewTiles;

    [Header("Spawn")]
    public bool[,] canSpawn;
    public Vector2[,] spawnDirection;
    public string toSpawnPath;
    public GameObject toSpawnImage;
    public GameObject toSpawnUnit;
    public int goldNeedToSpawn;
     
    public Dictionary<Vector2, SpawnInfo> spawnList = new Dictionary<Vector2, SpawnInfo>();

    [Header("Gold")]
    public int gold;
    public int age;
    public int goldNeedToAdvance;

    private void Awake()
    {
        PV = GetComponent<PhotonView>();

        //keep track of all players
        GameManager.instance.playerList.Add(PV.OwnerActorNr, this);
        GameManager.instance.createPlayerList();

        if (!PV.IsMine) return;
        instance = this;

        //master client in charge making grid
        if (PhotonNetwork.IsMasterClient)
        {
            TileManager.instance.makeGrid();
        }

        if (Config.moreMoneyTextMode)
        {
            gold = 40000000;
        }
    }

    #region ID

    [PunRPC]
    public void startGame(int newID, Vector2 spawnLocation)
    {
        //assign id
        id = newID;

        PV.RPC(nameof(startGame_all), RpcTarget.AllViaServer, newID);

        mode = "start";

        //reveal starting territory
        Tile[,] tiles = TileManager.instance.tiles;

        Tile root = tiles[(int)spawnLocation.x, (int)spawnLocation.y];

        root.setDark(false);

        foreach (Tile neighbor in root.neighbors)
        {
            neighbor.setDark(false);
        }

        foreach (Tile neighbor in root.neighbors2)
        {
            neighbor.setDark(false);
        }
    }

    [PunRPC]
    void startGame_all(int newID)
    {
        id = newID;
    }

    #endregion

    // Update is called once per frame
    void Update()
    {
        if (!PV.IsMine) return;

        Tile newHighlighted = null;

        if (TileManager.instance.tiles != null)
        {
            //tile at mousePosition
            newHighlighted = TileManager.instance.getTile(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }

        //spawn castle in the start
        if (mode == "start")
        {
            //highlight revealed land tiles
            if (highlighted != newHighlighted)
            {
                if (highlighted != null)
                    highlighted.highlight(false);

                highlighted = newHighlighted;

                if (highlighted != null)
                {
                    if (!highlighted.dark.activeSelf && highlighted.terrain == "land")
                    {
                        highlighted.highlight(true);
                    }
                    else
                    {
                        highlighted = null;
                    }
                }
            }

            if (Input.GetMouseButtonDown(0) && highlighted != null)
            {
                Tile[,] tiles = TileManager.instance.tiles;

                //initialize double arrays
                extraViewTiles = new int[TileManager.instance.tiles.GetLength(0), TileManager.instance.tiles.GetLength(1)];
                canSpawn = new bool[TileManager.instance.tiles.GetLength(0), TileManager.instance.tiles.GetLength(1)];
                spawnDirection = new Vector2[TileManager.instance.tiles.GetLength(0), TileManager.instance.tiles.GetLength(1)];

                //spawn castle
                Vector2Int startingTile = highlighted.pos;

                mainBase = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Building/MainBase"),
                    TileManager.instance.getWorldPosition(highlighted), Quaternion.identity).
                    GetComponent<MainBase>();

                mainBase.gameObject.GetPhotonView().RPC("Init", RpcTarget.All, id, startingTile.x, startingTile.y,
                     "Building/MainBase", age, 0);

                mainBase.GetComponent<Building>().updateCanSpawn();
                allBuildings.Add(mainBase);

                mainBase.PV.RPC(nameof(mainBase.updateTerritory), RpcTarget.All);

                UIManager.instance.startGameLocal();

                GameManager.instance.endTurn();
            }
        }
        //select
        else if (mode == "select")
        {
            //highlight any revealed
            if (highlighted != newHighlighted)
            {
                if (highlighted != null)
                {
                    highlighted.highlight(false);

                    if (highlighted.unit != null)
                        highlighted.unit.setHealthBar(false);
                }

                highlighted = newHighlighted;

                if (highlighted != null && !highlighted.dark.activeSelf)
                {
                    highlighted.highlight(true);
                }
                else
                {
                    highlighted = null;
                }
            }

            //show healthbar if there is a unit here
            if (highlighted != null && highlighted.unit != null)
            {
                highlighted.unit.setHealthBar(true);
            }

            //select unit when mouse pressed and not after turn ended
            if (Input.GetMouseButtonDown(0) && !turnEnded)
            {
                UIManager.instance.hideInfoTab();

                //deselect if something is selected
                if (unitSelected != null)
                {
                    unitSelected.setImage(Color.white);
                    unitSelected = null;
                }

                //if a tile is highlighted
                if (highlighted != null)
                {
                    //if I am going to spawn a unit here
                    if (spawnList.ContainsKey(highlighted.pos))
                    {
                        spawnInfoSelected = spawnList[highlighted.pos];
                        UIManager.instance.updateInfoTab(spawnInfoSelected);
                    }
                    //else if a unit is on the tile
                    else if (highlighted.GetComponent<Tile>().unit != null)
                    {
                        //don't show health bar
                        highlighted.unit.setHealthBar(false);

                        //update info tab
                        UIManager.instance.updateInfoTab(highlighted.unit, highlighted.unit.ownerID == id);

                        //select unit
                        unitSelected = highlighted.GetComponent<Tile>().unit.gameObject.GetComponent<IUnit>();

                        //change color to show selection
                        unitSelected.setImage(Color.grey);

                        //if it is my unit
                        if (highlighted.GetComponent<Tile>().unit.ownerID == id)
                        {
                            //if movable and turn not ended
                            if ((highlighted.GetComponent<Tile>().unit.gameObject.CompareTag("Troop"))
                                && !turnEnded)
                            {
                                mode = "move";
                            }
                        }
                    }
                }
            }
        }
        //move
        else if (mode == "move")
        {
            //highlight any tile
            if (highlighted != newHighlighted)
            {
                if (highlighted != null)
                    highlighted.highlight(false);

                highlighted = newHighlighted;

                if (newHighlighted != null)
                {
                    highlighted.highlight(true);
                }
                else
                {
                    newHighlighted = null;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                //findPath
                if (highlighted != null)
                {
                    highlighted.highlight(false);
                    unitSelected.gameObject.GetComponent<Troop>().findPath(highlighted.GetComponent<Tile>());
                }

                //deselect
                unitSelected.setImage(Color.white);
                unitSelected = null;
                UIManager.instance.hideInfoTab();

                highlighted = null;

                mode = "select";
            }
        }
        //spawn
        else if (mode == "spawn")
        {
            //highlight spawnable tiles
            if (highlighted != newHighlighted)
            {
                if (highlighted != null)
                    highlighted.highlight(false);

                highlighted = newHighlighted;

                if (toSpawnUnit.CompareTag("Spell"))
                {
                    //only need to be visible and no unit spawn here for spells
                    if (highlighted != null && !highlighted.dark.activeSelf
                        && !spawnList.ContainsKey(highlighted.pos))
                    {
                        highlighted.highlight(true);
                    }
                    else
                    {
                        highlighted = null;
                    }
                }
                //if tile is not null and no unit is here and the tile is still my territory
                //and no units is going to be spawn here
                else if (highlighted != null && highlighted.unit == null
                    && territory.Contains(highlighted) && !spawnList.ContainsKey(highlighted.pos))
                {
                    //for troops
                    if (toSpawnUnit.CompareTag("Troop"))
                    {
                        //can only spawn on spawnable tiles and it had to be a land tile or a ship on water
                        if (canSpawn[highlighted.pos.x, highlighted.pos.y] &&
                            (
                            (toSpawnUnit.GetComponent<Ship>() == null &&
                            highlighted.terrain == "land")
                            ||
                            (toSpawnUnit.GetComponent<Ship>() != null
                            && highlighted.terrain == "water")))
                        {
                            highlighted.highlight(true);
                        }
                        else
                        {
                            highlighted = null;
                        }
                    }
                    //for buildings
                    else if (toSpawnUnit.CompareTag("Building") && highlighted.terrain == "land")
                    {
                        highlighted.highlight(true);
                    }
                    else
                    {
                        highlighted = null;
                    }
                }
                else
                {
                    highlighted = null;
                }
            }

            //click to spawn
            if (Input.GetMouseButtonDown(0))
            {
                //there is a highlighted tile and enough gold
                if (highlighted != null && gold >= goldNeedToSpawn)
                {
                    //deduct gold
                    gold -= goldNeedToSpawn;
                    UIManager.instance.updateGoldText();

                    //spawn an image
                    GameObject spawnImage = Instantiate(toSpawnImage,
                    highlighted.gameObject.transform.position, Quaternion.identity);
                    spawnImage.SetActive(true);

                    //add to spawn list
                    spawnList.Add(highlighted.pos, new SpawnInfo(highlighted, toSpawnPath, toSpawnUnit.GetComponent<IUnit>(),
                        spawnImage, age, goldNeedToSpawn, goldNeedToSpawn / 2));

                    //reset to prevent double spawn
                    highlighted.highlight(false);
                    highlighted = null;
                }
                else
                {
                    //only change mode when didn't spawn correctly
                    mode = "select";

                    //clear selection
                    SpawnManager.instance.resetSpawnButtonImage();

                    //clear gray
                    foreach (Tile tile in visibleTiles)
                    {
                        if (tile != null)
                            tile.setGray(false);
                    }

                    //info tab
                    UIManager.instance.hideInfoTab();
                }
            }
        }
    }

    #region Turn

    public void stop()
    {
        UIManager.instance.hideInfoTab();

        if (mode == "spawn")
        {
            //set color of the spawn button selected back to white
            SpawnManager.instance.resetSpawnButtonImage();

            //clear gray
            foreach (Tile tile in visibleTiles)
            {
                if (tile != null)
                    tile.setGray(false);
            }
        }

        else if (mode == "move")
        {
            //set color of the selected unit back to white and deselect
            unitSelected.setImage(Color.white);
            unitSelected = null;
        }

        //clear selection
        if (unitSelected != null)
        {
            unitSelected.setImage(Color.white);
            unitSelected = null;
        }

        mode = "select";
        turnEnded = true;
    }

    [PunRPC]
    public void spawn()
    {
        //update visibility if didn't lost
        if (!lost)
        {
            List<Tile> tileList = visibleTiles.ToList();
            for (int i = tileList.Count - 1; i >= 0; i--)
            {
                tileList[i].updateVisibility();
            }
        }

        foreach (SpawnInfo info in spawnList.Values)
        {
            //spawn unit and initiate
            GameObject newUnit = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", info.unitName),
            info.spawnTile.gameObject.transform.position, Quaternion.identity);

            if (newUnit.CompareTag("Troop"))
            {
                newUnit.GetComponent<Troop>().PV.RPC("Init", RpcTarget.All,
                    id, info.spawnTile.pos.x, info.spawnTile.pos.y,
                    spawnDirection[info.spawnTile.pos.x, info.spawnTile.pos.y],
                    info.unitName, info.age, info.sellGold);

                allTroops.Add(newUnit.GetComponent<Troop>());

                if (newUnit.GetComponent<Ship>() != null)
                {
                    allShips.Add(newUnit.GetComponent<Ship>());
                }
            }
            else if (newUnit.CompareTag("Building"))
            {
                newUnit.GetComponent<Building>().PV.RPC("Init", RpcTarget.All,
                    id, info.spawnTile.pos.x, info.spawnTile.pos.y,
                    info.unitName, info.age, info.sellGold);
                newUnit.GetComponent<Building>().updateCanSpawn();

                allBuildings.Add(newUnit.GetComponent<Building>());
            }
            else if (newUnit.CompareTag("Spell"))
            {
                newUnit.GetComponent<Spell>().PV.RPC("Init", RpcTarget.All,
                    id, info.spawnTile.pos.x, info.spawnTile.pos.y,
                    info.unitName, info.age, info.sellGold);

                allSpells.Add(newUnit.GetComponent<Spell>());
            }

            Destroy(info.spawnImage);
        }

        //clear list
        spawnList = new Dictionary<Vector2, SpawnInfo>();

        //income from territory
        if (!lost)
        {
            gold += landTerritory * (age + Config.ageIncomeOffset);
        }

        UIManager.instance.updateGoldText();

        if (PhotonNetwork.OfflineMode)
        {
            troopMove();
        }
        else
        {
            Hashtable playerProperties = new Hashtable();
            playerProperties.Add("Spawned", true);
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
    }

    [PunRPC]
    public void troopMove()
    {
        //ships move first
        foreach (Ship ship in allShips)
        {
            ship.move();
        }

        foreach (Troop troop in allTroops)
        {
            troop.move();
        }

        if (PhotonNetwork.OfflineMode)
        {
            attack();
        }
        else
        {
            Hashtable playerProperties = new Hashtable();
            playerProperties.Add("Moved", true);
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
    }

    [PunRPC]
    public void attack()
    {
        foreach (Troop troop in allTroops)
        {
            troop.attack();
        }

        foreach (Building building in allBuildings)
        {
            building.effect();
        }

        foreach (Spell spell in allSpells)
        {
            spell.countDown();
        }

        if (PhotonNetwork.OfflineMode)
        {
            checkDeath();
        }
        else
        {
            Hashtable playerProperties = new Hashtable();
            playerProperties.Add("Attacked", true);
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
    }

    [PunRPC]
    public void checkDeath()
    {
        //troops
        foreach (Troop troop in allTroops)
        {
            troop.checkDeath();
        }

        for (int i = allTroops.Count - 1; i >= 0; i--)
        {
            if (allTroops[i].health <= 0)
            {
                allTroops.Remove(allTroops[i]);
            }
        }

        //buildings
        foreach (Building building in allBuildings)
        {
            building.checkDeath();
        }

        for (int i = allBuildings.Count - 1; i >= 0; i--)
        {
            if (allBuildings[i].health <= 0)
            {
                if (allBuildings[i].GetComponent<MainBase>() != null)
                {
                    allBuildings.Remove(allBuildings[i]);
                    end();
                    break;
                }
                allBuildings.Remove(allBuildings[i]);
            }
        }

        if (PhotonNetwork.OfflineMode)
        {
            fillInfoTab();
            GameManager.instance.startTurn();
        }
        else
        {
            Hashtable playerProperties = new Hashtable();
            playerProperties.Add("CheckedDeath", true);
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
    }

    #endregion

    #region UI

    [PunRPC]
    public void fillInfoTab()
    {
        //main base will be null
        if (lost)
        {
            UIManager.instance.playerUIManagerList[id].PV.RPC("fillInfo", RpcTarget.All,
           UIManager.instance.ageNameList[age], gold, territory.Count, allTroops.Count, allBuildings.Count, 0f);
        }
        else
        {
            UIManager.instance.playerUIManagerList[id].PV.RPC("fillInfo", RpcTarget.All,
               UIManager.instance.ageNameList[age], gold, territory.Count, allTroops.Count, allBuildings.Count,
              (float)mainBase.health / mainBase.fullHealth);
        }
    }

    #endregion

    public void end()
    {
        //kill all troops
        foreach (Troop troop in allTroops)
        {
            troop.kill();
        }

        //destroy all buildings
        foreach (Building building in allBuildings)
        {
            building.kill();
        }

        //clear spawnList
        foreach (SpawnInfo spawnInfo in spawnList.Values)
        {
            Destroy(spawnInfo.spawnImage);
        }
        spawnList = new Dictionary<Vector2, SpawnInfo>();

        PV.RPC(nameof(resetTerritory), RpcTarget.All);

        //display all tiles
        foreach (Tile tile in TileManager.instance.tiles)
        {
            if (tile != null)
            {
                tile.setDark(false);
            }
        }

        //remove from gameManager playerlist
        PV.RPC(nameof(removeFromPlayerList), RpcTarget.All);

        //display leave button
        UIManager.instance.leaveBtn.SetActive(true);

        //reset everything
        gold = 0;
        territory = new HashSet<Tile>();
        UIManager.instance.updateGoldText();

        //only end turn if quit
        if (!GameManager.instance.turnEnded)
            GameManager.instance.endTurn();

        UIManager.instance.lost();

        lost = true;

        //last update in player info tab
        fillInfoTab();
    }

    [PunRPC]
    public void removeFromPlayerList()
    {
        GameManager.instance.allPlayers.Remove(this);
    }

    [PunRPC]
    public void resetTerritory()
    {
        //no territory
        foreach (Tile tile in territory)
        {
            tile.reset();
        }
    }
}