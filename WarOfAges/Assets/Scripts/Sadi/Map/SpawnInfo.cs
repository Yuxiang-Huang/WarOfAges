using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpawnInfo
{
    public Tile spawnTile;

    public string unitName;

    public GameObject spawnImage;

    public int spawnGold;

    public int sellGold;

    public IUnit unit;

    public int age;

    public SpawnInfo(Tile spawnTile, string unitName, IUnit unit, GameObject spawnImage, int age, int spawnGold, int sellGold)
    {
        this.spawnTile = spawnTile;
        this.unitName = unitName;
        this.spawnImage = spawnImage;
        this.spawnGold = spawnGold;
        this.sellGold = sellGold;
        this.unit = unit;
        this.age = age;
    }
}
