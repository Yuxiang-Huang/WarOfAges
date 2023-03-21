using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ExtraHealth : Troop
{
    //attack all enemies on tiles around it
    public override void attack()
    {
        List<Tile> targets = new List<Tile>();

        //attack all enemies around whose health is more than 0
        foreach (Tile curTile in tile.neighbors)
        {
            //if can see this tile and there is enemy unit on it
            if (!curTile.dark.activeSelf && curTile.unit != null && curTile.unit.ownerID != ownerID)
            {
                if (curTile.unit.health > 0)
                {
                    curTile.unit.PV.RPC(nameof(takeDamage), RpcTarget.AllViaServer, damage);
                }
            }
        }
    }
}
