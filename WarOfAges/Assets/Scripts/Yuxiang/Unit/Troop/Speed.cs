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
            // flip direction to the attacking direction
            GameObject target = targets.Values.Last().unit.gameObject;
            PV.RPC(nameof(flipDirection), RpcTarget.All, target.transform.position.x < transform.position.x);
            //damage proportioned to number of tiles moved
            targets.Values.Last().unit.PV.RPC(nameof(takeDamage), RpcTarget.AllViaServer, damage * numOfTileMoved);
        }
    }

    // don't go where units are
    public override void findPathBot(Tile target)
    {
        //same tile reset
        if (target == tile)
        {
            path = new List<Tile>();

            Destroy(arrow);

            return;
        }

        float minDist = TileManager.instance.dist(target, tile);

        //initiated a queue
        Queue<List<Tile>> allPath = new Queue<List<Tile>>();

        List<Tile> root = new() { tile };

        allPath.Enqueue(root);


        bool[,] visited = new bool[TileManager.instance.tiles.GetLength(0),
                                   TileManager.instance.tiles.GetLength(1)];

        bool reach = false;

        //bfs
        while (allPath.Count != 0 && !reach)
        {
            List<Tile> cur = allPath.Dequeue();
            Tile lastTile = cur[cur.Count - 1];

            foreach (Tile curTile in lastTile.neighbors)
            {
                //not visited and no unit (Doesn't matter what terrain)
                if (!visited[curTile.pos.x, curTile.pos.y] && curTile.unit == null)
                {
                    //no team building
                    if (curTile.unit == null || !curTile.unit.gameObject.CompareTag("Building") ||
                        curTile.unit.ownerID != ownerID)
                    {
                        visited[curTile.pos.x, curTile.pos.y] = true;

                        //check this tile dist
                        List<Tile> dup = new List<Tile>(cur)
                        {
                            curTile
                        };

                        float curDist = TileManager.instance.dist(target, curTile);

                        if (curDist < 0.01)
                        {
                            reach = true;
                            path = dup;
                            minDist = curDist;
                        }
                        else if (curDist < minDist)
                        {
                            minDist = curDist;
                            path = dup;
                        }

                        allPath.Enqueue(dup);
                    }
                }
            }
        }

        //a path is found
        if (path.Count != 0)
        {
            //remove first tile
            path.RemoveAt(0);
        }

        // add all water tile to be ship needed tiles
        foreach (Tile curTile in path)
        {
            // if no water and no ship
            if (curTile.terrain == "water" && curTile.unit == null
                && !BotController.instance.spawnList.ContainsKey(curTile.pos))
            {
                BotController.instance.shipNeedTiles.Add(curTile);
            }
        }

        // recalculate path (edge case where a ship can't be spawned on time)
        path = new List<Tile>();
        refindPathBot(target);
    }

    // adding no unit on tile condition
    void refindPathBot(Tile target)
    {
        //follow this troop if in my team
        if (target.unit != null && target.unit.ownerID == ownerID)
        {
            toFollow = target.unit;
        }
        else
        {
            toFollow = null;
        }

        //same tile reset
        if (target == tile)
        {
            path = new List<Tile>();

            Destroy(arrow);

            return;
        }

        float minDist = TileManager.instance.dist(target, tile);

        //initiated a queue
        Queue<List<Tile>> allPath = new Queue<List<Tile>>();

        List<Tile> root = new() { tile };

        allPath.Enqueue(root);


        bool[,] visited = new bool[TileManager.instance.tiles.GetLength(0),
                                   TileManager.instance.tiles.GetLength(1)];

        bool reach = false;

        //bfs
        while (allPath.Count != 0 && !reach)
        {
            List<Tile> cur = allPath.Dequeue();
            Tile lastTile = cur[cur.Count - 1];

            foreach (Tile curTile in lastTile.neighbors)
            {
                //not visited and land tile or
                //water tile on ship or with ship on it
                if (!visited[curTile.pos.x, curTile.pos.y] &&
                    (curTile.terrain == "land" ||
                    (curTile.terrain == "water" &&
                    (ship != null ||
                    curTile.unit != null && curTile.unit.ownerID == ownerID))))
                {
                    // no unit here
                    if (curTile.unit == null)
                    {
                        //no team building
                        if (curTile.unit == null || !curTile.unit.gameObject.CompareTag("Building") ||
                            curTile.unit.ownerID != ownerID)
                        {
                            visited[curTile.pos.x, curTile.pos.y] = true;

                            //check this tile dist
                            List<Tile> dup = new List<Tile>(cur)
                        {
                            curTile
                        };

                            float curDist = TileManager.instance.dist(target, curTile);

                            if (curDist < 0.01)
                            {
                                reach = true;
                                path = dup;
                                minDist = curDist;
                            }
                            else if (curDist < minDist)
                            {
                                minDist = curDist;
                                path = dup;
                            }

                            allPath.Enqueue(dup);
                        }
                    }
                }
            }
        }

        //a path is found
        if (path.Count != 0)
        {
            //remove first tile
            path.RemoveAt(0);
        }

        displayArrow();
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
