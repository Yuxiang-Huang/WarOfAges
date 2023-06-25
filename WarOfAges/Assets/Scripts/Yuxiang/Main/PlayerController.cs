using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEditor;

public class PlayerController : Controller
{
    public static PlayerController instance;

    [Header("Mouse Interaction")]
    Tile highlighted;
    public string mode;
    public IUnit unitSelected;
    public SpawnInfo spawnInfoSelected;

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

    // Update is called once per frame
    void Update()
    {
        if (!PV.IsMine) return;

        Tile newHighlighted = null;

        // no action when turn ended
        if (turnEnded)
            return;

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
                spawnableTile = new HashSet<Tile>();
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

                UIManager.instance.endTurnUI();
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
                            cur.displayArrowForSpawn(spawnInfoSelected.spawnTile, highlighted, id);
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

                    // clear ship arrow if spawn on ship
                    if (highlighted.unit != null && highlighted.unit.gameObject.GetComponent<Ship>() != null)
                    {
                        Destroy(highlighted.unit.gameObject.GetComponent<Ship>().arrow);
                    }

                    //add to spawn list
                    SpawnInfo spawnInfo = new SpawnInfo(highlighted, toSpawnPath, toSpawnUnit.GetComponent<IUnit>(),
                    spawnImage, age, goldNeedToSpawn, goldNeedToSpawn / 2);

                    spawnList.Add(highlighted.pos, spawnInfo);

                    // add to spell list if necessary
                    if (toSpawnUnit.GetComponent<Spell>() != null)
                    {
                        spawnListSpell.Add(highlighted.pos, spawnInfo);
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

    [PunRPC]
    public override void startGame(int newID, Vector2 spawnLocation)
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

    public override void stop()
    {
        UIManager.instance.hideInfoTab();

        UIManager.instance.noSurredner();

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
}