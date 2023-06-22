using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEditor;

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

        // reveal all tiles in bot test mode
        if (Config.botTestMode)
        {
            foreach (Tile tile in TileManager.instance.tiles)
            {
                if (tile != null)
                    tile.setDark(false);
            }
        }
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
        if (age < 5)
        {
            if (gold >= goldNeedToAdvance)
            {
                gold -= goldNeedToAdvance;
                age++;
                goldNeedToAdvance *= Config.ageCostFactor;
                mainBase.upgrade();
            }
        }
        else
        {
            // try spawn AOE
            spawnButtons[7].selectSpawnUnitBot();

            // try attack main base first
            Tile bestTile = PlayerController.instance.mainBase.tile;
            foreach (Tile neighbor in bestTile.neighbors)
            {
                if (neighbor.ownerID == id && canSpawn(bestTile, toSpawnUnit))
                {
                    // attack if possible
                    addToSpawnList(bestTile);

                    gold -= goldNeedToSpawn;

                    break;
                }
            }

            if (gold >= goldNeedToSpawn)
            {
                // check for enemy building
                foreach (Building building in PlayerController.instance.allBuildings)
                {
                    // attack tower / extra view
                    if (building.GetComponent<ExtraView>() != null)
                    {
                        bool canAttack = false;

                        foreach (Tile neighbor in building.tile.neighbors)
                        {
                            if (neighbor.ownerID == id)
                            {
                                canAttack = true;
                            }
                        }

                        // attack if possible
                        if (canAttack && canSpawn(building.tile, toSpawnUnit))
                        {
                            addToSpawnList(building.tile);

                            gold -= goldNeedToSpawn;

                            // not enough gold
                            if (gold < goldNeedToSpawn)
                                break;
                        }
                    }
                }
            }
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

        // spawn ships where it is needed if second age
        for (int i = shipNeedTiles.Count - 1; i >= 0; i--)
        {
            if (gold >= goldNeedToSpawn && canSpawn(shipNeedTiles[i], toSpawnUnit))
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
                if (troop.age < age && gold >= troop.upgradeGold)
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
                if (building.age < age && gold >= building.upgradeGold)
                {
                    gold -= building.upgradeGold;
                    building.upgrade();
                }
            }
        }

        // spawn troops and buildings
        foreach (Tile curTile in spawnableTile)
        {
            int randomNum = Random.Range(0, age + 2);

            // not troop index, go again
            while (randomNum == 2 || randomNum > 4)
            {
                // ship turn to money
                if (randomNum == 2)
                {
                    spawnButtons[5].selectSpawnUnitBot();

                    // spawn money building farthest away from player base
                    Tile safestTile = findLandTileUsingComparator(farther);
                    if (gold >= goldNeedToSpawn && canSpawn(safestTile, toSpawnUnit))
                        addToSpawnList(safestTile);
                }
                // the tower
                else if (randomNum == 6)
                {
                    spawnButtons[6].selectSpawnUnitBot();

                    // spawn money building closest to player base
                    Tile closeTile = findLandTileUsingComparator(closer);
                    if (gold >= goldNeedToSpawn && canSpawn(closeTile, toSpawnUnit))
                        addToSpawnList(closeTile);
                }

                // money or AOE -> choose either speed or health
                if (randomNum == 5 || randomNum == 7)
                {
                    randomNum = Random.Range(3, 5);
                }
                // random pick again
                else
                {
                    randomNum = Random.Range(0, age + 3);
                }
            }

            // select the corresponding troop
            spawnButtons[randomNum].selectSpawnUnitBot();

            // check condition for spawning
            if (gold >= goldNeedToSpawn && canSpawn(curTile, toSpawnUnit))// && allTroops.Count < 2)
            {
                addToSpawnList(curTile);

                // set destination for newly spawned troop
                SpawnInfo spawnInfoSelected = spawnList[curTile.pos];

                // set path
                if (randomNum == 3)
                {
                    // speed go far
                    spawnInfoSelected.targetPathTile = findFarthestUnconqueredLandTile(curTile);
                }
                else
                {
                    // other troop go close
                    spawnInfoSelected.targetPathTile = findClosestUnconqueredLandTile(curTile);
                }

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

        // the green ready icon
        UIManager.instance.setEndTurn(id, true);
    }

    void addToSpawnList(Tile spawnTile)
    {
        //deduct gold
        gold -= goldNeedToSpawn;
        UIManager.instance.updateGoldText();

        GameObject spawnImage = null;
        //spawn an image only in bot test mode
        if (Config.botTestMode)
        {
            //spawn an image
            spawnImage = Instantiate(toSpawnImage,
            spawnTile.gameObject.transform.position, Quaternion.identity);
            //set all ages inactive except the current one (need to do because age can be different from player's age)
            foreach (Transform cur in spawnImage.transform)
            {
                cur.gameObject.SetActive(false);
            }
            spawnImage.transform.GetChild(Config.numAges - age - 1).gameObject.SetActive(true);
            spawnImage.SetActive(true);
        }

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
        //< to be in bot test mode
        if (Input.GetKey(KeyCode.Comma) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            Config.botTestMode = true;
            foreach (Tile tile in TileManager.instance.tiles)
            {
                if (tile != null)
                    tile.setDark(false);
            }
        }

        //> to end tutorial
        if (Input.GetKey(KeyCode.Period) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            if (TutorialManager.instance != null)
            {
                UIManager.instance.timerPaused = false;
                TutorialManager.instance.tutorialCanvas.gameObject.SetActive(false);
                Destroy(TutorialManager.instance);
            }
        }
        //}
    }

    #region Finding Tile depending on need

    delegate bool ComparingFunction(float orig, float cur);

    public bool farther(float orig, float cur)
    {
        return cur - orig > 0;
    }

    public bool closer(float orig, float cur)
    {
        return cur - orig < 0;
    }

    Tile findLandTileUsingComparator(ComparingFunction function)
    {
        Tile bestTile = mainBase.tile;
        float dist = Vector3.Magnitude(bestTile.transform.position - PlayerController.instance.mainBase.transform.position);

        foreach (Tile curTile in territory)
        {
            if (curTile.terrain == "land" && canSpawn(curTile, toSpawnUnit))
            {
                float curDist = Vector3.Magnitude(curTile.transform.position - PlayerController.instance.mainBase.transform.position);
                if (function(dist, curDist))
                {
                    dist = curDist;
                    bestTile = curTile;
                }
            }
        }
        return bestTile;
    }

    Tile findClosestUnconqueredLandTile(Tile curTile)
    {
        //initiated a queue
        Queue<Tile> tilesToLook = new Queue<Tile>();
        tilesToLook.Enqueue(curTile);

        bool[,] visited = new bool[TileManager.instance.tiles.GetLength(0),
                                   TileManager.instance.tiles.GetLength(1)];

        //bfs
        while (tilesToLook.Count != 0)
        {
            Tile nextTile = tilesToLook.Dequeue();

            // return if land and unconquered
            if (nextTile.terrain == "land" && nextTile.ownerID != id)
                return nextTile;

            foreach (Tile neighbor in nextTile.neighbors)
            {
                //not visited
                if (!visited[neighbor.pos.x, neighbor.pos.y])
                {
                    visited[neighbor.pos.x, neighbor.pos.y] = true;
                    tilesToLook.Enqueue(neighbor);
                }
            }
        }

        return PlayerController.instance.mainBase.tile;
    }

    Tile findFarthestUnconqueredLandTile(Tile startingTile)
    {
        Tile bestTile = PlayerController.instance.mainBase.tile;
        float dist = Vector3.Magnitude(bestTile.transform.position - startingTile.transform.position);

        foreach (Tile curTile in territory)
        {
            if (curTile.terrain == "land" && curTile.ownerID != id)
            {
                float curDist = Vector3.Magnitude(curTile.transform.position - startingTile.transform.position);
                if (curDist > dist)
                {
                    dist = curDist;
                    bestTile = curTile;
                }
            }
        }
        return bestTile;
    }

    #endregion

    public override void stop()
    {
        turnEnded = true;
    }

    public override void end()
    {
        base.end();
        UIManager.instance.displayWinScreen("YOU");
    }
}