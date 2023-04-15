using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;
using UnityEngine;

public class ExtraView : Building
{
    public override void updateCanSpawn()
    {
        base.updateCanSpawn();

        List<Tile> neighbors3 = TileManager.instance.findNeighbors3(tile);

        //reveal tiles
        foreach (Tile neighbor in neighbors3)
        {
            PlayerController.instance.extraViewTiles[neighbor.pos.x, neighbor.pos.y]++;
            neighbor.setDark(false);
        }

        foreach (Tile neighbor in tile.neighbors2)
        {
            PlayerController.instance.extraViewTiles[neighbor.pos.x, neighbor.pos.y]++;
            neighbor.setDark(false);
        }
    }

    //attack one enemy closest to main base
    public override void effect()
    {
        SortedDictionary<float, Tile> targets = new SortedDictionary<float, Tile>();

        //check all surrounding tiles
        foreach (Tile curTile in tile.neighbors)
        {
            //if can see this tile and there is enemy unit on it
            if (!curTile.dark.activeSelf && curTile.unit != null && curTile.unit.ownerID != ownerID)
            {
                //attack order depending on distance to mainbase
                targets.TryAdd(TileManager.instance.dist(tile, curTile), curTile);
            }
        }

        foreach (Tile curTile in tile.neighbors2)
        {
            //if can see this tile and there is enemy unit on it
            if (!curTile.dark.activeSelf && curTile.unit != null && curTile.unit.ownerID != ownerID)
            {
                //attack order depending on distance to mainbase
                targets.TryAdd(TileManager.instance.dist(tile, curTile), curTile);
            }
        }

        //don't attack already dead troop
        while (targets.Count > 0 && targets.Values.Last().unit.health <= 0)
        {
            targets.Remove(targets.Keys.Last());
        }

        if (targets.Count > 0)
        {
            targets.Values.Last().unit.PV.RPC(nameof(takeDamage), RpcTarget.AllViaServer, damage);
        }
    }

    public override void checkDeath()
    {
        if (health <= 0)
        {
            //check visibility
            List<Tile> neighbors3 = TileManager.instance.findNeighbors3(tile);

            foreach (Tile neighbor in neighbors3)
            {
                PlayerController.instance.extraViewTiles[neighbor.pos.x, neighbor.pos.y]--;
                neighbor.updateVisibility();
            }

            foreach (Tile neighbor in tile.neighbors2)
            {
                PlayerController.instance.extraViewTiles[neighbor.pos.x, neighbor.pos.y]--;
                neighbor.updateVisibility();
            }
        }

        base.checkDeath();
    }

    public override int getHealGold()
    {
        //3 * basic cost
        return 3 * (int)(Config.basicGoldUnit * Mathf.Pow(Config.ageCostFactor, age));
    }
}
