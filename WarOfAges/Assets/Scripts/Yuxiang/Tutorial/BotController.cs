using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEditor;
using static UnityEngine.GraphicsBuffer;

public class BotController : Controller
{
    public static BotController instance;

    [Header("Belongings")]
    public List<Tile> shipNeedTiles;

    [Header("Spawn")]
    [SerializeField] List<SpawnButton> spawnButtons;

    private void Start()
    {
        // destroy if not offline
        if (!PhotonNetwork.OfflineMode)
        {
            Destroy(gameObject);
            return;
        }

        PV = GetComponent<PhotonView>();

        instance = this;

        gold = 40;
    }

    #region ID

    [PunRPC]
    public override void startGame(int newID, Vector2 spawnLocation)
    {
        //assign id
        id = newID;

        PV.RPC(nameof(startGame_all), RpcTarget.AllViaServer, newID);

        // base tile is the tile with most land neighbors
        Tile[,] tiles = TileManager.instance.tiles;

        Tile root = tiles[(int)spawnLocation.x, (int)spawnLocation.y];

        Tile baseTile = root;
        int maxLandTile = countLandNeighbor(root);

        foreach (Tile neighbor in root.neighbors)
        {
            int landNeighbor = countLandNeighbor(neighbor);
            if (landNeighbor > maxLandTile)
            {
                baseTile = neighbor;
                maxLandTile = landNeighbor;
            }
        }

        foreach (Tile neighbor in root.neighbors2)
        {
            int landNeighbor = countLandNeighbor(neighbor);
            if (landNeighbor > maxLandTile)
            {
                baseTile = neighbor;
                maxLandTile = landNeighbor;
            }
        }

        //initialize double arrays
        extraViewTiles = new int[TileManager.instance.tiles.GetLength(0), TileManager.instance.tiles.GetLength(1)];
        spawnableTile = new HashSet<Tile>();
        spawnDirection = new Vector2[TileManager.instance.tiles.GetLength(0), TileManager.instance.tiles.GetLength(1)];

        //spawn castle
        mainBase = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Building/MainBase"),
            baseTile.transform.position, Quaternion.identity).
            GetComponent<MainBase>();

        mainBase.gameObject.GetPhotonView().RPC("Init", RpcTarget.All, id, baseTile.pos.x, baseTile.pos.y,
             "Building/MainBase", age, 0);

        mainBase.GetComponent<Building>().updateCanSpawn();
        allBuildings.Add(mainBase);

        mainBase.PV.RPC(nameof(mainBase.updateTerritory), RpcTarget.All);
    }

    // count number of land tile neighbors, return 0 is tile is water
    public int countLandNeighbor (Tile tile)
    {
        if (tile.terrain != "land")
            return 0;

        int ans = 0;
        foreach (Tile neighbor in tile.neighbors)
        {
            if (neighbor.terrain == "land")
                ans++;
        }
        return ans;
    }

    #endregion

    public void takeActions()
    {
        // upgrade age if possible
        if (gold > goldNeedToAdvance && age < 5)
        {
            gold -= goldNeedToAdvance;
            age++;
            goldNeedToAdvance *= Config.ageCostFactor;
            mainBase.upgrade();
        }

        // troops directions (can be improved by not going somewhere another troop is already going)
        foreach (Troop troop in allTroops)
        {
            // don't move ships
            if (troop.gameObject.GetComponent<Ship>() == null)
                troop.findPathBot(findClosestUnconqueredLandTile(troop.tile));
        }

        // select ship
        spawnButtons[2].selectSpawnUnitBot();

        // spawn ships where it is needed
        for (int i = shipNeedTiles.Count - 1; i >= 0; i--)
        {
            if (gold > goldNeedToSpawn && canSpawn(shipNeedTiles[i], toSpawnUnit))
            {
                addToSpawnList(shipNeedTiles[i]);
                shipNeedTiles.Remove(shipNeedTiles[i]);
            }
        }

        // upgrade troops
        foreach (Troop troop in allTroops)
        {
            // don't upgrade ships
            if (!allShips.Contains(troop))
            {
                // check condition
                if (troop.age < age && gold > troop.upgradeGold)
                {
                    gold -= troop.upgradeGold;
                    troop.upgrade();
                }
            }
        }

        // upgrade buildings
        foreach (Building building in allBuildings)
        {
            // ignore main base
            if (building.gameObject.GetComponent<MainBase>() == null)
            {
                // check condition
                if (building.age < age && gold > building.upgradeGold)
                {
                    gold -= building.upgradeGold;
                    building.upgrade();
                }
            }
        }

        // spawn troops
        foreach (Tile curTile in spawnableTile)
        {
            int randomNum = Random.Range(0, age + 2);

            spawnButtons[randomNum].selectSpawnUnitBot();

            if (gold > goldNeedToSpawn && canSpawn(curTile, toSpawnUnit))
            {
                addToSpawnList(curTile);

                // set destination for newly spawned troop
                SpawnInfo spawnInfoSelected = spawnList[curTile.pos];

                // set path
                spawnInfoSelected.targetPathTile = findClosestUnconqueredLandTile(curTile);

                // arrows in bot test mode
                if (Config.botTestMode)
                {
                    if (spawnInfoSelected.arrow != null)
                        Destroy(spawnInfoSelected.arrow);

                    Troop cur = spawnInfoSelected.unit.gameObject.GetComponent<Troop>();
                    cur.displayArrowForSpawn(spawnInfoSelected.spawnTile, spawnInfoSelected.targetPathTile);
                    if (cur.arrow != null)
                    {
                        spawnInfoSelected.arrow = Instantiate(cur.arrow);
                        Destroy(cur.arrow);
                    }
                }
            }
        }

        UIManager.instance.setEndTurn(id, true);
    }

    Tile findClosestUnconqueredLandTile(Tile startingTile)
    {
        Tile bestTile = PlayerController.instance.mainBase.tile;
        float shortestDist = Vector3.Magnitude(bestTile.transform.position - startingTile.transform.position);

        foreach (Tile curTile in TileManager.instance.tiles)
        {
            if (curTile != null && curTile.terrain == "land" && curTile.ownerID != id)
            {
                float curDist = Vector3.Magnitude(curTile.transform.position - startingTile.transform.position);
                if (curDist < shortestDist)
                {
                    shortestDist = curDist;
                    bestTile = curTile;
                }
            }
        }
        return bestTile;
    }

    void addToSpawnList(Tile spawnTile)
    {
        //deduct gold
        gold -= goldNeedToSpawn;
        UIManager.instance.updateGoldText();

        //spawn an image
        GameObject spawnImage = Instantiate(toSpawnImage,
        spawnTile.gameObject.transform.position, Quaternion.identity);
        //set all ages inactive except the current one (need to do because age can be different from player's age)
        foreach (Transform cur in spawnImage.transform)
        {
            cur.gameObject.SetActive(false);
        }
        spawnImage.transform.GetChild(Config.numAges - age - 1).gameObject.SetActive(true);
        spawnImage.SetActive(true);

        //add to spawn list
        SpawnInfo spawnInfo = new SpawnInfo(spawnTile, toSpawnPath, toSpawnUnit.GetComponent<IUnit>(),
            spawnImage, age, goldNeedToSpawn, goldNeedToSpawn / 2);

        spawnList.Add(spawnTile.pos, spawnInfo);

        // add to spell list if necessary
        if (toSpawnUnit.GetComponent<Spell>() != null)
        {
            spawnListSpell.Add(spawnTile.pos, spawnInfo);
        }

        // add ship if necessary
        if (spawnInfo.unit.gameObject.CompareTag("Troop") && spawnInfo.unit.gameObject.GetComponent<Ship>() == null)
        {
            if (spawnInfo.spawnTile.unit != null && spawnInfo.spawnTile.terrain == "water")
            {
                spawnInfo.unit.gameObject.GetComponent<Troop>().ship = spawnInfo.spawnTile.unit.gameObject.GetComponent<Ship>();
            }
        }
    }

    void Update()
    {
        //if (Config.botTestMode)
        //{
            if (Input.GetKeyDown(KeyCode.RightShift))
            {
                Config.botTestMode = true;
                foreach (Tile tile in TileManager.instance.tiles)
                {
                    if (tile != null)
                        tile.setDark(false);
                }
            }
        //}
    }
    //{
    //    if (!PV.IsMine) return;

    //    Tile newHighlighted = null;

    //    if (TileManager.instance.tiles != null)
    //    {
    //        //tile at mousePosition
    //        newHighlighted = TileManager.instance.getTile(Camera.main.ScreenToWorldPoint(Input.mousePosition));
    //    }

    //    //spawn castle in the start
    //    if (mode == "start")
    //    {
    //        //highlight revealed land tiles
    //        if (highlighted != newHighlighted)
    //        {
    //            if (highlighted != null)
    //                highlighted.highlight(false);

    //            highlighted = newHighlighted;

    //            if (highlighted != null)
    //            {
    //                if (!highlighted.dark.activeSelf && highlighted.terrain == "land")
    //                {
    //                    highlighted.highlight(true);
    //                }
    //                else
    //                {
    //                    highlighted = null;
    //                }
    //            }
    //        }

    //        if (Input.GetMouseButtonDown(0) && highlighted != null)
    //        {
    //            Tile[,] tiles = TileManager.instance.tiles;

    //            //initialize double arrays
    //            extraViewTiles = new int[TileManager.instance.tiles.GetLength(0), TileManager.instance.tiles.GetLength(1)];
    //            spawnable = new bool[TileManager.instance.tiles.GetLength(0), TileManager.instance.tiles.GetLength(1)];
    //            spawnDirection = new Vector2[TileManager.instance.tiles.GetLength(0), TileManager.instance.tiles.GetLength(1)];

    //            //spawn castle
    //            Vector2Int startingTile = highlighted.pos;

    //            mainBase = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Building/MainBase"),
    //                highlighted.transform.position, Quaternion.identity).
    //                GetComponent<MainBase>();

    //            mainBase.gameObject.GetPhotonView().RPC("Init", RpcTarget.All, id, startingTile.x, startingTile.y,
    //                 "Building/MainBase", age, 0);

    //            mainBase.GetComponent<Building>().updateCanSpawn();
    //            allBuildings.Add(mainBase);

    //            mainBase.PV.RPC(nameof(mainBase.updateTerritory), RpcTarget.All);

    //            UIManager.instance.startGameLocal();

    //            GameManager.instance.endTurn();
    //        }
    //    }
    //    //select
    //    else if (mode == "select")
    //    {
    //        //highlight any revealed
    //        if (highlighted != newHighlighted)
    //        {
    //            //change previous
    //            if (highlighted != null)
    //            {
    //                highlighted.highlight(false);

    //                if (highlighted.unit != null)
    //                    highlighted.unit.setHealthBar(false);
    //            }

    //            highlighted = newHighlighted;

    //            //change current
    //            if (highlighted != null && !highlighted.dark.activeSelf)
    //            {
    //                highlighted.highlight(true);
    //            }
    //            else
    //            {
    //                highlighted = null;
    //            }
    //        }

    //        //show healthbar if there is a unit here
    //        if (highlighted != null && highlighted.unit != null)
    //        {
    //            highlighted.unit.setHealthBar(true);
    //        }

    //        //select unit when mouse pressed and not after turn ended
    //        if (Input.GetMouseButtonDown(0) && !turnEnded)
    //        {
    //            UIManager.instance.hideInfoTab();

    //            //deselect if something is selected
    //            if (unitSelected != null)
    //            {
    //                unitSelected.setImage(Color.white);
    //                unitSelected = null;
    //            }
    //            if (spawnInfoSelected != null)
    //            {
    //                spawnInfoSelected.setSpawnImageColor(Color.white);
    //                spawnInfoSelected = null;
    //            }

    //            //if a tile is highlighted
    //            if (highlighted != null)
    //            {
    //                //if I am going to spawn a unit here
    //                if (spawnList.ContainsKey(highlighted.pos))
    //                {
    //                    //select spawn info
    //                    spawnInfoSelected = spawnList[highlighted.pos];

    //                    //don't show health bar
    //                    if (spawnInfoSelected.spawnTile.unit != null)
    //                        spawnInfoSelected.spawnTile.unit.setHealthBar(false);

    //                    //update info tab
    //                    UIManager.instance.updateInfoTab(spawnInfoSelected);

    //                    //change color to show selection
    //                    spawnInfoSelected.setSpawnImageColor(Color.grey);

    //                    //if movable and turn not ended
    //                    if (spawnInfoSelected.unit.gameObject.CompareTag("Troop") && !turnEnded)
    //                    {
    //                        mode = "move";
    //                    }
    //                }
    //                //else if a unit is on the tile
    //                else if (highlighted.GetComponent<Tile>().unit != null)
    //                {
    //                    //select unit
    //                    unitSelected = highlighted.GetComponent<Tile>().unit.gameObject.GetComponent<IUnit>();

    //                    //update info tab
    //                    UIManager.instance.updateInfoTab(highlighted.unit, highlighted.unit.ownerID == id);

    //                    //don't show health bar
    //                    unitSelected.setHealthBar(false);

    //                    //change color to show selection
    //                    unitSelected.setImage(Color.grey);

    //                    //if it is my unit
    //                    if (highlighted.GetComponent<Tile>().unit.ownerID == id)
    //                    {
    //                        //if movable and turn not ended
    //                        if (highlighted.GetComponent<Tile>().unit.gameObject.CompareTag("Troop")
    //                            && !turnEnded)
    //                        {
    //                            mode = "move";
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    //move
    //    else if (mode == "move")
    //    {
    //        //highlight any tile
    //        if (highlighted != newHighlighted)
    //        {
    //            if (highlighted != null)
    //                highlighted.highlight(false);

    //            highlighted = newHighlighted;

    //            if (newHighlighted != null)
    //            {
    //                highlighted.highlight(true);
    //            }
    //            else
    //            {
    //                newHighlighted = null;
    //            }
    //        }

    //        if (Input.GetMouseButtonDown(0))
    //        {
    //            //for existing troop
    //            if (spawnInfoSelected == null)
    //            {
    //                //findPath
    //                if (highlighted != null)
    //                {
    //                    highlighted.highlight(false);
    //                    unitSelected.gameObject.GetComponent<Troop>().findPath(highlighted.GetComponent<Tile>());
    //                }

    //                //deselect
    //                if (unitSelected != null)
    //                {
    //                    unitSelected.setImage(Color.white);
    //                    unitSelected = null;
    //                }
    //            }
    //            //for spawn troops
    //            else
    //            {
    //                if (highlighted != null)
    //                {
    //                    highlighted.highlight(false);
    //                    spawnInfoSelected.targetPathTile = highlighted;

    //                    //display arrow

    //                    // prevent edge case of despawn and find path
    //                    if (spawnList.ContainsValue(spawnInfoSelected))
    //                    {
    //                        if (spawnInfoSelected.arrow != null)
    //                            Destroy(spawnInfoSelected.arrow);

    //                        Troop cur = spawnInfoSelected.unit.gameObject.GetComponent<Troop>();
    //                        cur.displayArrowForSpawn(spawnInfoSelected.spawnTile, highlighted);
    //                        if (cur.arrow != null)
    //                        {
    //                            spawnInfoSelected.arrow = Instantiate(cur.arrow);
    //                            Destroy(cur.arrow);
    //                        }
    //                    }
    //                }

    //                //deselect
    //                spawnInfoSelected.setSpawnImageColor(Config.spawnImageColor);
    //                spawnInfoSelected = null;
    //            }

    //            UIManager.instance.hideInfoTab();

    //            highlighted = null;

    //            mode = "select";
    //        }
    //    }
    //    //spawn
    //    else if (mode == "spawn")
    //    {
    //        //highlight spawnable tiles
    //        if (highlighted != newHighlighted)
    //        {
    //            if (highlighted != null)
    //                highlighted.highlight(false);

    //            highlighted = newHighlighted;

    //            if (canSpawn(highlighted, toSpawnUnit))
    //            {
    //                highlighted.highlight(true);
    //            }
    //            else
    //            {
    //                highlighted = null;
    //            }
    //        }

    //        //click to spawn
    //        if (Input.GetMouseButtonDown(0))
    //        {
    //            //there is a highlighted tile and enough gold
    //            if (highlighted != null && gold >= goldNeedToSpawn)
    //            {
    //                //deduct gold
    //                gold -= goldNeedToSpawn;
    //                UIManager.instance.updateGoldText();

    //                //spawn an image
    //                GameObject spawnImage = Instantiate(toSpawnImage,
    //                highlighted.gameObject.transform.position, Quaternion.identity);
    //                spawnImage.SetActive(true);

    //                //add to spawn list
    //                SpawnInfo spawnInfo = new SpawnInfo(highlighted, toSpawnPath, toSpawnUnit.GetComponent<IUnit>(),
    //                    spawnImage, age, goldNeedToSpawn, goldNeedToSpawn / 2);

    //                spawnList.Add(highlighted.pos, spawnInfo);

    //                if (toSpawnUnit.GetComponent<Spell>() != null)
    //                {
    //                    spawnListSpell.Add(highlighted.pos, spawnInfo);
    //                }

    //                //add ship if necessary
    //                if (spawnInfo.unit.gameObject.CompareTag("Troop") && spawnInfo.unit.gameObject.GetComponent<Ship>() == null)
    //                {
    //                    if (spawnInfo.spawnTile.unit != null && spawnInfo.spawnTile.terrain == "water")
    //                    {
    //                        spawnInfo.unit.gameObject.GetComponent<Troop>().ship = spawnInfo.spawnTile.unit.gameObject.GetComponent<Ship>();
    //                    }
    //                }

    //                //reset to prevent double spawn
    //                highlighted.highlight(false);
    //                highlighted = null;
    //            }
    //            else
    //            {
    //                //only change mode when didn't spawn correctly
    //                mode = "select";

    //                //clear selection
    //                SpawnManager.instance.resetSpawnButtonImage();

    //                //reset
    //                toSpawnUnit = null;

    //                //clear gray
    //                foreach (Tile tile in visibleTiles)
    //                {
    //                    if (tile != null)
    //                        tile.setGray(false);
    //                }

    //                //info tab
    //                UIManager.instance.hideInfoTab();
    //            }
    //        }
    //    }
    //}

    public override void stop()
    {
        turnEnded = true;
    }

    public override void end()
    {
        base.end();
        UIManager.instance.displayWinScreen("Name");
    }
}