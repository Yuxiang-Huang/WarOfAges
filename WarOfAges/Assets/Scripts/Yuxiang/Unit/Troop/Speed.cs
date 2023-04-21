using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;
using UnityEngine;

public class Speed : Troop
{
    private int numberOfTileMoved;

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
            //damage proportioned to number of tiles moved
            targets.Values.Last().unit.PV.RPC(nameof(takeDamage), RpcTarget.AllViaServer, damage * numberOfTileMoved);
        }
    }

    public override void move()
    {
        base.move();
        base.move();
    }

    [PunRPC]
    public override void moveUpdate_RPC(int nextTileX, int nextTileY)
    {
        //leave water
        if (tile.terrain == "water" && TileManager.instance.tiles[nextTileX, nextTileY].terrain == "land")
        {
            //edge case when exchange tile
            if (tile.unit == null)
                tile.unit = ship;
            ship.tile = tile;
            ship = null;
        }

        //leave land
        else if (tile.terrain == "land" && TileManager.instance.tiles[nextTileX, nextTileY].terrain == "water")
        {
            //board a ship
            ship = TileManager.instance.tiles[nextTileX, nextTileY].unit.gameObject.GetComponent<Ship>();
        }

        //update tile
        if (tile != TileManager.instance.tiles[nextTileX, nextTileY])
        {
            numberOfTileMoved++;
        }

        tile = TileManager.instance.tiles[nextTileX, nextTileY];
        tile.updateStatus(ownerID, this);

        //also try to conquer all water tiles around if moved to land tile
        if (tile.terrain == "land")
        {
            foreach (Tile neighbor in tile.neighbors)
            {
                neighbor.tryWaterConquer();
            }
        }

        //owner so animate movement
        if (ownerID == PlayerController.instance.id)
        {
            StartCoroutine(TranslateOverTime(transform.position, tile.transform.position, Config.troopMovementTime));
            if (ship != null)
            {
                ship.StartCoroutine(ship.TranslateOverTime(ship.transform.position, tile.transform.position, Config.troopMovementTime));
            }
        }
        else
        {
            //update position
            transform.position = tile.transform.position;
            healthbar.gameObject.transform.position = transform.position + offset;
            if (ship != null)
            {
                ship.transform.position = transform.position;
                ship.healthbar.gameObject.transform.position = ship.transform.position + offset;
            }
        }
    }

    public override void resetMovement()
    {
        base.resetMovement();
        numberOfTileMoved = 0;
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
