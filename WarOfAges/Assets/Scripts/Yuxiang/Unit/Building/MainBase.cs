using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class MainBase : Building
{
    [PunRPC]
    public void updateTerritory()
    {
        foreach (Tile neigbhor in tile.neighbors)
        {
            neigbhor.updateStatus(ownerID, null);
        }
    }
}