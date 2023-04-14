using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;
using UnityEngine;

public class Speed : Troop
{
    public override void attack()
    {
        SortedDictionary<float, Tile> targets = new SortedDictionary<float, Tile>();

        //check all surrounding tiles
        foreach (Tile curTile in tile.neighbors)
        {
            //if can see this tile and there is enemy unit on it
            if (!curTile.dark.activeSelf && curTile.unit != null && curTile.unit.ownerID != ownerID)
            {
                //attack order depending on dot product
                targets.TryAdd(Vector2.Dot(direction,
                    curTile.transform.position - tile.transform.position),
                    curTile);
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

    public override void move()
    {
        base.move();
        base.move();
    }

    public override void displayArrow()
    {
        //destroy arrow
        if (arrow != null)
        {
            Destroy(arrow);
        }

        //show arrow if there is two tile to go
        if (path.Count > 1)
        {
            arrow = Instantiate(UIManager.instance.arrowPrefab, transform.position, Quaternion.identity);

            arrow.transform.localScale = new Vector3(2.5f, 1, 1);

            Vector2 arrowDirection = path[1].transform.position - tile.transform.position;

            float angle = Mathf.Atan2(arrowDirection.y, arrowDirection.x);

            arrow.transform.Rotate(Vector3.forward, angle * 180 / Mathf.PI);
        }
        //show arrow if there is only one tile to go
        else if (path.Count > 0)
        {
            arrow = Instantiate(UIManager.instance.arrowPrefab, transform.position, Quaternion.identity);

            Vector2 arrowDirection = path[0].transform.position - tile.transform.position;

            float angle = Mathf.Atan2(arrowDirection.y, arrowDirection.x);

            arrow.transform.Rotate(Vector3.forward, angle * 180 / Mathf.PI);
        }
    }
}
