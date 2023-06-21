using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public interface IController
{
    PhotonView PV { get; }

    public int id { get; }

    public GameObject gameObject { get; }

    public MainBase mainBase { get; }

    //[Header("Belongings")]
    public List<Troop> allTroops { get; set; }
    public List<Ship> allShips { get; set; }
    public List<Building> allBuildings { get; set; }
    public List<Spell> allSpells { get; set; }

    public HashSet<Tile> territory { get; set; }
    public int landTerritory { get; set; }
    public HashSet<Tile> visibleTiles { get; set; }

    public int[,] extraViewTiles { get; set; }

    //[Header("Spawn")]
    public HashSet<Tile> spawnableTile { get; set; }
    public Vector2[,] spawnDirection { get; set; }
    public string toSpawnPath { get; set; }
    public GameObject toSpawnImage { get; set; }
    public GameObject toSpawnUnit { get; set; }
    public int goldNeedToSpawn { get; set; }

    public Dictionary<Vector2, SpawnInfo> spawnList { get; set; }
    public Dictionary<Vector2, SpawnInfo> spawnListSpell { get; set; }

    //[Header("Gold")]
    public int gold { get; set; }
    public int age { get; set; }
    public int goldNeedToAdvance { get; set; }

    //[Header("Actions")]
    public List<IUnit> toSell { get; set; }
    public List<IUnit> toUpgrade { get; set; }

    public void startFirstIndicator(bool status);

    public void startGame(int newID, Vector2 spawnLocation);

    public void fillInfoTab();

    public void spawn();

    public void troopMove();

    public void attack();

    public void checkDeath();

    public void end();
}
