using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviourPunCallbacks, IController
{
    public PhotonView PV { get; set; }

    public static PlayerController instance;

    public int id;

    public bool lost;
    public bool turnEnded;

    public MainBase mainBase;

    public PlayerUIManager playerUIManager;

    [Header("Mouse Interaction")]
    Tile highlighted;
    public string mode;
    public IUnit unitSelected;
    public SpawnInfo spawnInfoSelected;

    //[Header("Belongings")]
    public List<Troop> allTroops { get; set; } = new List<Troop>();
    public List<Ship> allShips { get; set; } = new List<Ship>();
    public List<Building> allBuildings { get; set; } = new List<Building>();
    public List<Spell> allSpells { get; set; } = new List<Spell>();

    public HashSet<Tile> territory { get; set; } = new HashSet<Tile>();
    public int landTerritory { get; set; }
    public HashSet<Tile> visibleTiles { get; set; } = new HashSet<Tile>();

    public int[,] extraViewTiles { get; set; }

    //[Header("Spawn")]
    public bool[,] spawnable { get; set; }
    public Vector2[,] spawnDirection { get; set; }
    public string toSpawnPath { get; set; }
    public GameObject toSpawnImage { get; set; }
    public GameObject toSpawnUnit { get; set; }
    public int goldNeedToSpawn { get; set; }
     
    public Dictionary<Vector2, SpawnInfo> spawnList { get; set; } = new Dictionary<Vector2, SpawnInfo>();
    public Dictionary<Vector2, SpawnInfo> spawnListSpell { get; set; } = new Dictionary<Vector2, SpawnInfo>();

    //[Header("Gold")]
    public int gold { get; set; }
    public int age { get; set; }
    public int goldNeedToAdvance { get; set; }

    //[Header("Actions")]
    public List<IUnit> toSell { get; set; } = new List<IUnit>();
    public List<IUnit> toUpgrade { get; set; } = new List<IUnit>();

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
            TileManager.instance.startMakeGrid();
        }

        if (Config.moreMoneyTestMode)
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

        playerUIManager = UIManager.instance.playerUIManagerList[newID];
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
                spawnable = new bool[TileManager.instance.tiles.GetLength(0), TileManager.instance.tiles.GetLength(1)];
                spawnDirection = new Vector2[TileManager.instance.tiles.GetLength(0), TileManager.instance.tiles.GetLength(1)];

                //spawn castle
                Vector2Int startingTile = highlighted.pos;

                mainBase = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Building/MainBase"),
                    highlighted.transform.position, Quaternion.identity).
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
                //change previous
                if (highlighted != null)
                {
                    highlighted.highlight(false);

                    if (highlighted.unit != null)
                        highlighted.unit.setHealthBar(false);
                }

                highlighted = newHighlighted;

                //change current
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
                if (spawnInfoSelected != null)
                {
                    spawnInfoSelected.setSpawnImageColor(Color.white);
                    spawnInfoSelected = null;
                }

                //if a tile is highlighted
                if (highlighted != null)
                {
                    //if I am going to spawn a unit here
                    if (spawnList.ContainsKey(highlighted.pos))
                    {
                        //select spawn info
                        spawnInfoSelected = spawnList[highlighted.pos];

                        //don't show health bar
                        if (spawnInfoSelected.spawnTile.unit != null)
                            spawnInfoSelected.spawnTile.unit.setHealthBar(false);

                        //update info tab
                        UIManager.instance.updateInfoTab(spawnInfoSelected);

                        //change color to show selection
                        spawnInfoSelected.setSpawnImageColor(Color.grey);

                        //if movable and turn not ended
                        if (spawnInfoSelected.unit.gameObject.CompareTag("Troop") && !turnEnded)
                        {
                            mode = "move";
                        }
                    }
                    //else if a unit is on the tile
                    else if (highlighted.GetComponent<Tile>().unit != null)
                    {
                        //select unit
                        unitSelected = highlighted.GetComponent<Tile>().unit.gameObject.GetComponent<IUnit>();

                        //update info tab
                        UIManager.instance.updateInfoTab(highlighted.unit, highlighted.unit.ownerID == id);

                        //don't show health bar
                        unitSelected.setHealthBar(false);

                        //change color to show selection
                        unitSelected.setImage(Color.grey);

                        //if it is my unit
                        if (highlighted.GetComponent<Tile>().unit.ownerID == id)
                        {
                            //if movable and turn not ended
                            if (highlighted.GetComponent<Tile>().unit.gameObject.CompareTag("Troop")
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
                //for existing troop
                if (spawnInfoSelected == null)
                {
                    //findPath
                    if (highlighted != null)
                    {
                        highlighted.highlight(false);
                        unitSelected.gameObject.GetComponent<Troop>().findPath(highlighted.GetComponent<Tile>());
                    }

                    //deselect
                    if (unitSelected != null)
                    {
                        unitSelected.setImage(Color.white);
                        unitSelected = null;
                    }
                }
                //for spawn troops
                else
                {
                    if (highlighted != null)
                    {
                        highlighted.highlight(false);
                        spawnInfoSelected.targetPathTile = highlighted;

                        //display arrow

                        // prevent edge case of despawn and find path
                        if (spawnList.ContainsValue(spawnInfoSelected))
                        {
                            if (spawnInfoSelected.arrow != null)
                                Destroy(spawnInfoSelected.arrow);

                            Troop cur = spawnInfoSelected.unit.gameObject.GetComponent<Troop>();
                            cur.displayArrowForSpawn(spawnInfoSelected.spawnTile, highlighted);
                            if (cur.arrow != null)
                            {
                                spawnInfoSelected.arrow = Instantiate(cur.arrow);
                                Destroy(cur.arrow);
                            }
                        }
                    }

                    //deselect
                    spawnInfoSelected.setSpawnImageColor(Config.spawnImageColor);
                    spawnInfoSelected = null;
                }

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

                if (canSpawn(highlighted, toSpawnUnit))
                {
                    highlighted.highlight(true);
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
                    SpawnInfo spawnInfo = new SpawnInfo(highlighted, toSpawnPath, toSpawnUnit.GetComponent<IUnit>(),
                        spawnImage, age, goldNeedToSpawn, goldNeedToSpawn / 2);

                    spawnList.Add(highlighted.pos, spawnInfo);

                    if (toSpawnUnit.GetComponent<Spell>() != null)
                    {
                        spawnListSpell.Add(highlighted.pos, spawnInfo);
                    }

                    //add ship if necessary
                    if (spawnInfo.unit.gameObject.CompareTag("Troop") && spawnInfo.unit.gameObject.GetComponent<Ship>() == null)
                    {
                        if (spawnInfo.spawnTile.unit != null && spawnInfo.spawnTile.terrain == "water")
                        {
                            spawnInfo.unit.gameObject.GetComponent<Troop>().ship = spawnInfo.spawnTile.unit.gameObject.GetComponent<Ship>();
                        }
                    }

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

                    //reset
                    toSpawnUnit = null;

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

    public bool canSpawn(Tile curTile, GameObject spawnUnit)
    {
        if (spawnUnit.CompareTag("Spell"))
        {
            //only need to be visible and no unit spawn here for spells
            if (curTile != null && !curTile.dark.activeSelf
                && !spawnList.ContainsKey(curTile.pos))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //Common condition for troop and building:
        //if tile is not null and the tile is still my territory
        //and no units is going to be spawn here
        else if (curTile != null && territory.Contains(curTile) && !spawnList.ContainsKey(curTile.pos))
        {
            //for troops
            if (spawnUnit.CompareTag("Troop"))
            {
                //if not a ship
                if (spawnUnit.GetComponent<Ship>() == null)
                {
                    //can only spawn on spawnable tiles 
                    if (spawnable[curTile.pos.x, curTile.pos.y])
                    {
                        //a land tile with no unit
                        if (curTile.terrain == "land" && curTile.unit == null)
                        {
                            return true;
                        }
                        //on a ship
                        else if (curTile.unit != null && curTile.unit.gameObject.GetComponent<Ship>() != null){
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    //only requirement of ship now is to be on water 
                    if (curTile.terrain == "water")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            //for buildings only need to be a land tile and no unit there
            else if (spawnUnit.CompareTag("Building") && curTile.terrain == "land" && curTile.unit == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
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
            if (unitSelected != null)
            {
                unitSelected.setImage(Color.white);
                unitSelected = null;
            }
        }

        //clear selection
        if (unitSelected != null)
        {
            unitSelected.setImage(Color.white);
            unitSelected = null;
        }

        if (spawnInfoSelected != null)
        {
            spawnInfoSelected.setSpawnImageColor(Color.white);
            spawnInfoSelected = null;
        }

        mode = "select";
        turnEnded = true;
    }

    [PunRPC]
    public void spawn()
    {
        //income
        if (!lost)
        {
            gold += calculateIncome();
        }

        //update visibility, upgrade and sell if didn't lost
        if (!lost)
        {
            List<Tile> tileList = visibleTiles.ToList();
            for (int i = tileList.Count - 1; i >= 0; i--)
            {
                tileList[i].updateVisibility();
            }

            //upgrade and sell for other players
            foreach (IUnit unit in toUpgrade)
            {
                unit.PV.RPC(nameof(unit.upgrade), RpcTarget.Others);
            }

            foreach (IUnit unit in toSell)
            {
                unit.PV.RPC("destroy", RpcTarget.Others);
            }

            toSell = new List<IUnit>();
            toUpgrade = new List<IUnit>();
        }

        //reset unsellable
        GameManager.instance.unsellableUnits = new HashSet<IUnit>();

        foreach (SpawnInfo info in spawnList.Values)
        {
            //skip skills
            if (!spawnListSpell.ContainsKey(info.spawnTile.pos))
            {
                //spawn unit and initiate
                GameObject newUnit = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", info.unitName),
                info.spawnTile.gameObject.transform.position, Quaternion.identity);

                if (newUnit.CompareTag("Troop"))
                {
                    allTroops.Add(newUnit.GetComponent<Troop>());

                    //destroy arrowed used for path finding
                    Destroy(info.arrow);

                    //a ship is spawned
                    if (newUnit.GetComponent<Ship>() != null)
                    {
                        allShips.Add(newUnit.GetComponent<Ship>());
                    }

                    newUnit.GetComponent<Troop>().PV.RPC("Init", RpcTarget.All,
                        id, info.spawnTile.pos.x, info.spawnTile.pos.y,
                        spawnDirection[info.spawnTile.pos.x, info.spawnTile.pos.y],
                        info.unitName, info.age, info.sellGold);

                    //transfer path
                    if (info.targetPathTile != null)
                    {
                        newUnit.GetComponent<Troop>().findPath(info.targetPathTile);
                    }

                    //reset unit
                    if (info.unit.gameObject.TryGetComponent<Troop>(out var mayeTroop))
                        mayeTroop.ship = null;
                }
                else if (newUnit.CompareTag("Building"))
                {
                    newUnit.GetComponent<Building>().PV.RPC("Init", RpcTarget.All,
                        id, info.spawnTile.pos.x, info.spawnTile.pos.y,
                        info.unitName, info.age, info.sellGold);
                    newUnit.GetComponent<Building>().updateCanSpawn();

                    allBuildings.Add(newUnit.GetComponent<Building>());
                }

                Destroy(info.spawnImage);
            }
        }

        //clear list
        spawnList = new Dictionary<Vector2, SpawnInfo>();

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
        //spawn spell now after all troops are spawned
        foreach (SpawnInfo info in spawnListSpell.Values)
        {
            //spawn unit and initiate
            GameObject newUnit = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", info.unitName),
            info.spawnTile.gameObject.transform.position, Quaternion.identity);

            newUnit.GetComponent<Spell>().PV.RPC("Init", RpcTarget.All,
                id, info.spawnTile.pos.x, info.spawnTile.pos.y,
                info.unitName, info.age, info.sellGold);

            allSpells.Add(newUnit.GetComponent<Spell>());

            Destroy(info.spawnImage);
        }

        spawnListSpell = new Dictionary<Vector2, SpawnInfo>();

        //ships move first
        foreach (Ship ship in allShips)
        {
            ship.move();
        }

        foreach (Troop troop in allTroops)
        {
            troop.move();
        }

        //recalculate path for followers
        foreach (Troop troop in allTroops)
        {
            troop.follow();
        }

        if (Config.debugTestMode)
        {
            Debug.Log("end move");
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
        //troops check death and reset movement
        foreach (Troop troop in allTroops)
        {
            troop.checkDeath();
            troop.resetMovement();
        }

        for (int i = allTroops.Count - 1; i >= 0; i--)
        {
            if (allTroops[i].health <= 0)
            {
                if (allTroops[i].gameObject.GetComponent<Ship>() != null)
                    allShips.Remove(allTroops[i].gameObject.GetComponent<Ship>());
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
        if (!lost)
        {
            UIManager.instance.playerUIManagerList[id].PV.RPC("fillInfo", RpcTarget.All,
               UIManager.instance.ageNameList[age], gold, territory.Count, allTroops.Count, allBuildings.Count,
              (float)mainBase.health / mainBase.fullHealth);
        }
    }

    public int calculateIncome()
    {
        int sum = calculateLandIncome(landTerritory, TileManager.instance.totalLandConquered);

        //income from extra money
        foreach (Building building in allBuildings)
        {
            if (building.GetComponent<ExtraMoney>() != null)
            {
                sum += building.GetComponent<ExtraMoney>().income();
            }
        }

        return sum;
    }

    public int calculateMarginalIncome()
    {
        int cur = calculateLandIncome(landTerritory, TileManager.instance.totalLandConquered);

        int next = calculateLandIncome(landTerritory + 1, TileManager.instance.totalLandConquered + 1);

        return next - cur;
    }

    public int calculateLandIncome(int landNum, int landConquered)
    {
        ////territory income: 5 (x - 0.375 * playerNum * x^2 / mapSize)
        //int sum = (int)(Config.goldFactor * (landTerritory - Config.goldPercent * GameManager.instance.allPlayers.Count *
        //    landTerritory * landTerritory / TileManager.instance.totalLandTiles));

        //territory income: total possible income * square root of land percentage * total conquered land percent
        return (int)((
            (Config.goldFactor * TileManager.instance.totalLandTiles) *
            Mathf.Sqrt((float)landNum / TileManager.instance.totalLandTiles)) *
            ((float)landConquered / TileManager.instance.totalLandTiles));
    }

    public void startFirstIndicator(bool status)
    {
        playerUIManager.PV.RPC("setOrderIndicator", RpcTarget.All, status);
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

        //reset everything
        gold = 0;
        territory = new HashSet<Tile>();
        UIManager.instance.updateGoldText();

        toSell = new List<IUnit>();
        toUpgrade = new List<IUnit>();

        UIManager.instance.lost();

        lost = true;

        //last update in player info tab
        UIManager.instance.playerUIManagerList[id].PV.RPC("fillInfo", RpcTarget.All,
            UIManager.instance.ageNameList[age], gold, territory.Count, allTroops.Count, allBuildings.Count, 0f);
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
            tile.lostReset();
        }
    }
}