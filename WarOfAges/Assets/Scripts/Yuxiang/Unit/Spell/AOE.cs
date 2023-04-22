using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class AOE : Spell
{
    [PunRPC]
    public override void Init(int playerID, int startingtTileX, int startingtTileY,
        string path, int age, int sellGold)
    {
        base.Init(playerID, startingtTileX, startingtTileY, path, age, sellGold);

        if (tile.unit != null && tile.unit != PlayerController.instance.mainBase.GetComponent<IUnit>())
        {
            GameManager.instance.unsellableUnits.Add(tile.unit);
        }

        //make all units in range unsellable
        foreach (Tile neighbor in tile.neighbors)
        {
            if (neighbor.unit != null && neighbor.unit != PlayerController.instance.mainBase.GetComponent<IUnit>())
            {
                GameManager.instance.unsellableUnits.Add(neighbor.unit);
            }
        }
        foreach (Tile neighbor in tile.neighbors2)
        {
            if (neighbor.unit != null && neighbor.unit != PlayerController.instance.mainBase.GetComponent<IUnit>())
            {
                GameManager.instance.unsellableUnits.Add(neighbor.unit);
            }
        }
    }

    public override void effect()
    {
        base.effect();

        if (tile.unit != null)
        {
            tile.unit.PV.RPC(nameof(takeDamage), RpcTarget.AllViaServer, damage * 3);
        }

        foreach (Tile neighbor in tile.neighbors)
        {
            neighbor.setDark(false);

            if (neighbor.unit != null)
            {
                neighbor.unit.PV.RPC(nameof(takeDamage), RpcTarget.AllViaServer, damage * 2);
            }
        }

        foreach (Tile neighbor in tile.neighbors2)
        {
            neighbor.setDark(false);

            if (neighbor.unit != null)
            {
                neighbor.unit.PV.RPC(nameof(takeDamage), RpcTarget.AllViaServer, damage);
            }
        }

        PV.RPC(nameof(kill), RpcTarget.All);
    }
}
