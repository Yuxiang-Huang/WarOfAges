using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class AOE : Spell
{
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
