using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Controller : MonoBehaviour
{
    public PhotonView PV;

    public int id;

    public bool lost;
    public bool turnEnded;

    public MainBase mainBase;

    public PlayerUIManager playerUIManager;

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
    public HashSet<Tile> spawnableTile;
    public Vector2[,] spawnDirection;
    public string toSpawnPath;
    public GameObject toSpawnImage;
    public GameObject toSpawnUnit;
    public int goldNeedToSpawn;

    public Dictionary<Vector2, SpawnInfo> spawnList = new Dictionary<Vector2, SpawnInfo>();
    public Dictionary<Vector2, SpawnInfo> spawnListSpell = new Dictionary<Vector2, SpawnInfo>();

    [Header("Gold")]
    public int gold;
    public int age;
    public int goldNeedToAdvance;

    [Header("Actions")]
    public List<IUnit> toSell = new List<IUnit>();
    public List<IUnit> toUpgrade = new List<IUnit>();

    #region ID

    [PunRPC]
    public virtual void startGame(int newID, Vector2 spawnLocation) { }

    [PunRPC]
    public void startGame_all(int newID)
    {
        id = newID;

        playerUIManager = UIManager.instance.playerUIManagerList[newID];
    }

    #endregion

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
                    if (spawnableTile.Contains(curTile))
                    {
                        //a land tile with no unit
                        if (curTile.terrain == "land" && curTile.unit == null)
                        {
                            return true;
                        }
                        //on a ship
                        else if (curTile.unit != null && curTile.unit.gameObject.GetComponent<Ship>() != null)
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
                else
                {
                    //only requirement of ship now is to be on water and no unit
                    if (curTile.terrain == "water" && curTile.unit == null)
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

    public virtual void stop() { }

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
            // want to see whole map for testing bot
            if (!Config.botTestMode)
            {
                List<Tile> tileList = visibleTiles.ToList();
                for (int i = tileList.Count - 1; i >= 0; i--)
                {
                    tileList[i].updateVisibility();
                }
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
            //skip skills and ship
            if (!spawnListSpell.ContainsKey(info.spawnTile.pos) &&
                info.unit.gameObject.GetComponent<Ship>() == null)
            {
                //spawn unit and initiate
                GameObject newUnit = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", info.unitName),
                info.spawnTile.gameObject.transform.position, Quaternion.identity);

                if (newUnit.CompareTag("Troop"))
                {
                    allTroops.Add(newUnit.GetComponent<Troop>());

                    //destroy arrowed used for path finding
                    Destroy(info.arrow);

                    // initialize
                    newUnit.GetComponent<Troop>().PV.RPC("Init", RpcTarget.All,
                        id, info.spawnTile.pos.x, info.spawnTile.pos.y,
                        spawnDirection[info.spawnTile.pos.x, info.spawnTile.pos.y],
                        info.unitName, info.age, info.sellGold);

                    //transfer path
                    if (info.targetPathTile != null)
                    {
                        newUnit.GetComponent<Troop>().findPath(info.targetPathTile);
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

                Destroy(info.spawnImage);
            }
        }

        // spawn ships afterwards so the path is found in the same way
        foreach (SpawnInfo info in spawnList.Values)
        {
            // ship only
            if (info.unit.gameObject.GetComponent<Ship>() != null)
            {
                //spawn unit and initiate
                GameObject newUnit = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", info.unitName),
                info.spawnTile.gameObject.transform.position, Quaternion.identity);

                allTroops.Add(newUnit.GetComponent<Troop>());

                //destroy arrowed used for path finding
                Destroy(info.arrow);

                // add to ship list
                allShips.Add(newUnit.GetComponent<Ship>());

                // initialize
                newUnit.GetComponent<Troop>().PV.RPC("Init", RpcTarget.All,
                    id, info.spawnTile.pos.x, info.spawnTile.pos.y,
                    spawnDirection[info.spawnTile.pos.x, info.spawnTile.pos.y],
                    info.unitName, info.age, info.sellGold);

                //transfer path
                if (info.targetPathTile != null)
                {
                    newUnit.GetComponent<Troop>().findPath(info.targetPathTile);
                }

                Destroy(info.spawnImage);
            }
        }

        //clear list
        spawnList = new Dictionary<Vector2, SpawnInfo>();

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

        UIManager.instance.updateGoldText();

        if (!PhotonNetwork.OfflineMode)
        {
            Hashtable playerProperties = new Hashtable();
            playerProperties.Add("Spawned", true);
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
    }

    [PunRPC]
    public void troopMove()
    {
        foreach (Troop troop in allTroops)
        {
            if (troop.GetComponent<Ship>() == null)
                troop.move();
        }

        // ships move after 
        foreach (Ship ship in allShips)
        {
            ship.move();
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

        if (!PhotonNetwork.OfflineMode)
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

        if (!PhotonNetwork.OfflineMode)
        {
            Hashtable playerProperties = new Hashtable();
            playerProperties.Add("Attacked", true);
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
    }

    [PunRPC]
    public void checkDeath()
    {
        if (Config.debugTestMode)
        {
            Debug.Log("resetMovement");
        }

        // troops check death and reset movement
        foreach (Troop troop in allTroops)
        {
            troop.checkDeath();
            troop.resetMovement();
        }

        // remove dead troop from list
        for (int i = allTroops.Count - 1; i >= 0; i--)
        {
            if (allTroops[i].health <= 0)
            {
                if (allTroops[i].gameObject.GetComponent<Ship>() != null)
                    allShips.Remove(allTroops[i].gameObject.GetComponent<Ship>());
                allTroops.Remove(allTroops[i]);
            }
        }

        // buildings
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

        if (Config.debugTestMode)
        {
            Debug.Log("end turn");
        }

        if (!PhotonNetwork.OfflineMode)
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

    public virtual void end()
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