using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ExtraHealth : Troop
{
    [PunRPC]
    public override void Init(int playerID, int startingtTileX, int startingtTileY, Vector2 startDirection,
        string path, int age, int sellGold)
    {
        base.Init(playerID, startingtTileX, startingtTileY, startDirection, path, age, sellGold);

        if (ownerID == PlayerController.instance.id)
        {
            //reveal tiles
            foreach (Tile neighbor in tile.neighbors2)
            {
                PlayerController.instance.extraViewTiles[neighbor.pos.x, neighbor.pos.y]++;
                neighbor.setDark(false);
            }
        }
    }

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

        //attack all enemies two tiles around whose health is more than 0
        foreach (Tile curTile in tile.neighbors2)
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

    public override void move()
    {
        //update visibility
        foreach (Tile neighbor in tile.neighbors2)
        {
            PlayerController.instance.extraViewTiles[neighbor.pos.x, neighbor.pos.y]--;
            neighbor.updateVisibility();
        }

        base.move();

        //update visibility
        foreach (Tile neighbor in tile.neighbors2)
        {
            neighbor.setDark(false);
            PlayerController.instance.extraViewTiles[neighbor.pos.x, neighbor.pos.y]++;
            neighbor.updateVisibility();
        }
    }
}
