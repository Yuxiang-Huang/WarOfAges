using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public interface IController
{
    PhotonView PV { get; }

    public HashSet<Tile> territory { get; }

    public int landTerritory { get; set; }

    public List<Spell> allSpells { get; }

    public void startFirstIndicator(bool status);

    public void startGame(int newID, Vector2 spawnLocation);

    public void fillInfoTab();

    public void attack();

    public void checkDeath();
}
