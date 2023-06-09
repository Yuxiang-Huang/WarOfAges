using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Text;
using System.Runtime.ConstrainedExecution;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using System.Collections;
using static UnityEngine.GraphicsBuffer;
using System.Linq;

public class TileManager : MonoBehaviourPunCallbacks
{
    public static TileManager instance;

    public PhotonView PV;

    public Tile[,] tiles;

    //for tile territory colors
    [SerializeField] List<Color> ownerColorsOrig;
    public List<Color> ownerColors;

    //building blocks
    readonly float tileSize = 1;
    // difference between consecutive tiles
    readonly float horizontalDif = 0.5f;
    readonly float verticalDif = Mathf.Sqrt(3);

    public int totalLandTiles;
    public int totalLandConquered;

    [SerializeField] GameObject landTilePrefab;
    [SerializeField] GameObject waterTilePrefab;

    public Dictionary<Vector2Int, int> neighborIndexOddRow;
    public Dictionary<Vector2Int, int> neighborIndexEvenRow;

    public List<Vector2> spawnLocations;

    [SerializeField] int mapRadius;

    void Awake()
    {
        instance = this;

        PV = GetComponent<PhotonView>();

        //create random colors if master client
        if (PhotonNetwork.IsMasterClient)
        {
            int[] randomIndices = new int[ownerColorsOrig.Count];

            //shuffle
            for (int i = 0; i < randomIndices.Length; i ++)
            {
                int index = Random.Range(0, ownerColorsOrig.Count);
                ownerColors.Add(ownerColorsOrig[index]);
                ownerColorsOrig.RemoveAt(index);
                randomIndices[i] = index;
            }

            //sync
            PV.RPC(nameof(createColorList), RpcTarget.Others, randomIndices);
        }

        //check borders in prefab
        neighborIndexOddRow = new Dictionary<Vector2Int, int>
        {
            { new Vector2Int(-2, 0), 3 },
            { new Vector2Int(2, 0), 5 },
            { new Vector2Int(-1, 0), 1 },
            { new Vector2Int(1, 0), 0 },
            { new Vector2Int(-1, 1), 2 },
            { new Vector2Int(1, 1), 4 }
        };

        neighborIndexEvenRow = new Dictionary<Vector2Int, int>
        {
            { new Vector2Int(-2, 0), 3 },
            { new Vector2Int(2, 0), 5 },
            { new Vector2Int(-1, 0), 2 },
            { new Vector2Int(1, 0), 4 },
            { new Vector2Int(-1, -1), 1 },
            { new Vector2Int(1, -1), 0 }
        };
    }

    [PunRPC]
    public void createColorList(int[] randomIndices)
    {
        //shuffle colors
        ownerColors = new List<Color>();

        foreach (int index in randomIndices)
        { 
            ownerColors.Add(ownerColorsOrig[index]);
            ownerColorsOrig.RemoveAt(index);
        }
    }

    public void startMakeGrid()
    {
        StartCoroutine(nameof(waitToGetRadius));
    }

    IEnumerator waitToGetRadius()
    {
        yield return new WaitUntil(() => PhotonNetwork.CurrentRoom.CustomProperties["mapRadius"] != null);

        makeGrid();
    }

    public void makeGrid()
    {
        mapRadius = (int)PhotonNetwork.CurrentRoom.CustomProperties["mapRadius"];

        int rows = mapRadius * 4;
        int cols = mapRadius * 4;

        string[,] instructionGrid = new string[rows, cols];

        //fill with null
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                instructionGrid[row, col] = "0";
            }
        }

        //initiate
        float waterLikelihood = 1f;

        Queue<Vector2Int> tileToGenerated = new Queue<Vector2Int>();
        tileToGenerated.Enqueue(new Vector2Int(rows / 2, cols / 2));

        //for every radius
        for (int r = 0; r < mapRadius; r++)
        {
            //find spawn spots here
            if (r == mapRadius - 3)
                findSpawnLocation((Vector2Int[])tileToGenerated.ToArray().Clone());

            int size = tileToGenerated.Count;

            for (int i = 0; i < size; i++)
            {
                Vector2Int curCor = tileToGenerated.Dequeue();

                //skip if assigned
                if (instructionGrid[curCor.x, curCor.y] == "0")
                {
                    //assign terrain
                    if ((string)PhotonNetwork.CurrentRoom.CustomProperties["Mode"] == "Water")
                    {
                        //assign terrain
                        if (Random.Range(0, 1f) < waterLikelihood)
                        {
                            instructionGrid[curCor.x, curCor.y] = "2";
                        }
                        else
                        {
                            instructionGrid[curCor.x, curCor.y] = "1";
                        }
                    }
                    else
                    {
                        instructionGrid[curCor.x, curCor.y] = "1";
                    }

                    //append tiles
                    if (curCor.x % 2 == 0)
                    {
                        foreach (Vector2Int offset in neighborIndexEvenRow.Keys)
                        {
                            tileToGenerated.Enqueue(new Vector2Int(curCor.x + offset.x, curCor.y + offset.y));
                        }
                    }
                    else
                    {
                        foreach (Vector2Int offset in neighborIndexOddRow.Keys)
                        {
                            tileToGenerated.Enqueue(new Vector2Int(curCor.x + offset.x, curCor.y + offset.y));
                        }
                    }
                }
            }

            waterLikelihood -= 1f / mapRadius;
        }

        //store type of tiles using bit
        StringBuilder instruction = new();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                instruction.Append(instructionGrid[row, col]);
            }
        }

        PV.RPC(nameof(makeGrid_RPC), RpcTarget.AllViaServer, mapRadius, rows, cols, instruction.ToString());
    }

    public void findSpawnLocation(Vector2Int[] positions)
    {
        Vector2Int loxPair = new Vector2Int(int.MaxValue, 0);
        Vector2Int hixPair = new Vector2Int(0, 0);

        float loy = getWorldPosition(positions[0]).y;
        float hiy = getWorldPosition(positions[0]).y;

        //find extreme x and extreme y
        foreach (Vector2Int cur in positions)
        {
            if (getWorldPosition(cur).x >= getWorldPosition(hixPair).x)
                hixPair = cur;

            if (getWorldPosition(cur).x <= getWorldPosition(loxPair).x)
                loxPair = cur;

            hiy = Mathf.Max(hiy, getWorldPosition(cur).y);
            loy = Mathf.Min(loy, getWorldPosition(cur).y);
        }

        Vector2Int loxWithLoyPair = new Vector2Int(int.MaxValue, 0);
        Vector2Int hixWithLoyPair = new Vector2Int(0, 0);
        Vector2Int loxWithHiyPair = new Vector2Int(int.MaxValue, 0);
        Vector2Int hixWithHiyPair = new Vector2Int(0, 0);

        //find upper and lower corners
        foreach (Vector2Int cur in positions)
        {
            if (getWorldPosition(cur).y >= hiy)
            {
                if (getWorldPosition(cur).x >= getWorldPosition(hixWithHiyPair).x)
                    hixWithHiyPair = cur;

                if (getWorldPosition(cur).x <= getWorldPosition(loxWithHiyPair).x)
                    loxWithHiyPair = cur;
            }

            if (getWorldPosition(cur).y <= loy)
            {
                if (getWorldPosition(cur).x >= getWorldPosition(hixWithLoyPair).x)
                    hixWithLoyPair = cur;

                if (getWorldPosition(cur).x <= getWorldPosition(loxWithLoyPair).x)
                    loxWithLoyPair = cur;
            }
        }

        //update allSpawnLocations
        List<Vector2> allSpawnLocations = new List<Vector2>
        {
            loxPair,
            hixPair,
            loxWithLoyPair,
            hixWithLoyPair,
            loxWithHiyPair,
            hixWithHiyPair
        };

        int numPlayer = PhotonNetwork.CurrentRoom.PlayerCount;

        //choose spawn locations base on number of player
        if (numPlayer == 1 || numPlayer == 2 || numPlayer == 5 || numPlayer == 6 || PhotonNetwork.OfflineMode)
        {
            spawnLocations = new List<Vector2>(allSpawnLocations);
        }
        else if (numPlayer == 3)
        {
            if (Random.Range(0, 2) == 0)
            {
                spawnLocations.Add(loxPair);
                spawnLocations.Add(hixWithHiyPair);
                spawnLocations.Add(hixWithLoyPair);
            }
            else
            {
                spawnLocations.Add(hixPair);
                spawnLocations.Add(loxWithHiyPair);
                spawnLocations.Add(loxWithLoyPair);
            }
        }
        else if (numPlayer == 4)
        {
            int randomChoose = Random.Range(0, 3);
            if (randomChoose == 0)
            {
                spawnLocations.Add(loxPair);
                spawnLocations.Add(hixPair);
                spawnLocations.Add(hixWithHiyPair);
                spawnLocations.Add(loxWithLoyPair);
            }
            else if (randomChoose == 1)
            {
                spawnLocations.Add(loxPair);
                spawnLocations.Add(hixPair);
                spawnLocations.Add(hixWithLoyPair);
                spawnLocations.Add(loxWithHiyPair);
            }
            else
            {
                spawnLocations.Add(hixWithLoyPair);
                spawnLocations.Add(loxWithHiyPair);
                spawnLocations.Add(hixWithHiyPair);
                spawnLocations.Add(loxWithLoyPair);
            }
        }
    }

    [PunRPC]
    public void makeGrid_RPC(int mapRadius, int rows, int cols, string instruction)
    {
        //setting camera
        Camera.main.orthographicSize = (mapRadius + 1);
        Camera.main.transform.position = new Vector3(-mapRadius / 5, 0, -10);
        CameraController.maxZoom = Camera.main.orthographicSize;

        //make map
        tiles = new Tile[rows, cols];

        GameObject parent = new GameObject("Map");

        int count = 0;

        //generate the grid using instruction
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {                
                //skip null
                if (instruction[count] != '0')
                {
                    float xPos = (i - rows/2) * horizontalDif * tileSize;
                    float yPos = (j - cols /2) * verticalDif * tileSize + (i % 2 * verticalDif / 2 * tileSize);

                    Vector3 pos = new Vector3(xPos, yPos, 0);

                    //instantiate
                    if (instruction[count] == '1')
                    {
                        tiles[i, j] = Instantiate(landTilePrefab, pos, Quaternion.identity).GetComponent<Tile>();
                        tiles[i, j].terrain = "land";

                        totalLandTiles++;
                    }
                    else if (instruction[count] == '2')
                    {
                        tiles[i, j] = Instantiate(waterTilePrefab, pos, Quaternion.identity).GetComponent<Tile>();
                        tiles[i, j].terrain = "water";
                    }

                    //set tile stats
                    tiles[i, j].transform.SetParent(parent.transform);

                    tiles[i, j].GetComponent<Tile>().pos = new Vector2Int(i, j);
                }

                count++;
            }
        }

        //set neighbors
        for (int row = 0; row < tiles.GetLength(0); row++)
        {
            for (int col = 0; col < tiles.GetLength(1); col++)
            {
                //skip null
                if (tiles[row, col] != null)
                {
                    List<Tile> neighbors = tiles[row, col].neighbors;

                    //left and right
                    if (row >= 2)
                    {
                        neighbors.Add(tiles[row - 2, col]);
                    }
                    if (row < tiles.GetLength(0) - 2)
                    {
                        neighbors.Add(tiles[row + 2, col]);
                    }

                    if (row % 2 == 0)
                    {
                        //there is a row before it
                        if (row > 0)
                        {
                            neighbors.Add(tiles[row - 1, col]);

                            //even row decrease col
                            if (col >= 1)
                            {
                                neighbors.Add(tiles[row - 1, col - 1]);
                            }
                        }
                        //there is a row after it
                        if (row < tiles.GetLength(0) - 1)
                        {
                            neighbors.Add(tiles[row + 1, col]);

                            //even row decrease col
                            if (col >= 1)
                            {
                                neighbors.Add(tiles[row + 1, col - 1]);
                            }
                        }
                    }
                    else
                    {
                        //there is a row before it
                        if (row > 0)
                        {
                            neighbors.Add(tiles[row - 1, col]);

                            //odd row increase col
                            if (col < tiles.GetLength(1) - 1)
                            {
                                neighbors.Add(tiles[row - 1, col + 1]);
                            }
                        }
                        //there is a row after it
                        if (row < tiles.GetLength(0) - 1)
                        {
                            neighbors.Add(tiles[row + 1, col]);

                            //odd row increase col
                            if (col < tiles.GetLength(1) - 1)
                            {
                                neighbors.Add(tiles[row + 1, col + 1]);
                            }
                        }
                    }

                    //remove null tiles
                    for (int i = neighbors.Count - 1; i >= 0; i--)
                    {
                        if (neighbors[i] == null)
                        {
                            neighbors.RemoveAt(i);
                        }
                    }
                }
            }
        }

        //set neighbors2
        for (int row = 0; row < tiles.GetLength(0); row++)
        {
            for (int col = 0; col < tiles.GetLength(1); col++)
            {
                //skip null
                if (tiles[row, col] != null)
                {
                    tiles[row, col].neighbors2 = findNeighbors2(tiles[row, col]);
                }
            }
        }

        var hash = PhotonNetwork.LocalPlayer.CustomProperties;
        hash["Ready"] = true;
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    }

    //all tiles two tiles away
    public List<Tile> findNeighbors2(Tile tile)
    {
        List<Tile> ans = new List<Tile>();

        foreach (Tile neighbor in tile.neighbors)
        {
            ans.Add(neighbor);

            foreach (Tile neighbor2 in neighbor.neighbors)
            {
                if (!ans.Contains(neighbor2))
                {
                    ans.Add(neighbor2);
                }
            }
        }

        //remove inside tiles
        foreach (Tile neighbor in tile.neighbors)
        {
            ans.Remove(neighbor);
        }

        ans.Remove(tile);

        return ans;
    }

    //all tiles three tiles away
    public List<Tile> findNeighbors3(Tile tile)
    {
        List<Tile> neighbors3 = new List<Tile>();

        //also update extra view tiles
        foreach (Tile neighbor2 in tile.neighbors2)
        {
            foreach (Tile neighbor in neighbor2.neighbors)
            {
                neighbors3.Add(neighbor);
            }
        }

        foreach (Tile neighbor in tile.neighbors)
        {
            neighbors3.Remove(neighbor);
        }

        return neighbors3;
    }

    //get the tile depending on world position
    public Tile getTile(Vector2 pos)
    {
        //simple division to find rough x and y
        int roundX = (int) (pos.x / horizontalDif / tileSize) + tiles.GetLength(0) / 2;

        int roundY = (int) (pos.y / verticalDif / tileSize) + tiles.GetLength(1) / 2;

        SortedDictionary<float, Tile> candidates = new SortedDictionary<float, Tile>();

        // try all tiles in proximity
        for (int i = roundX - 1; i <= roundX + 1; i++)
        {
            for (int j = roundY - 1; j <= roundY + 1; j++)
            {
                if (i < 0 || i >= tiles.GetLength(0) || j < 0 || j >= tiles.GetLength(1))
                {
                    continue;
                }

                //compare with all neighbors
                Tile oneTile = tiles[i, j];

                if (oneTile == null)
                {
                    continue;
                }

                Tile bestTile = oneTile;

                float minDist = dist(pos, oneTile.transform.position);

                foreach (Tile neighbor in oneTile.neighbors)
                {
                    float mayDist = dist(pos, neighbor.transform.position);
                    if (mayDist < minDist)
                    {
                        minDist = mayDist;
                        bestTile = neighbor;
                    }
                }
                foreach (Tile neighbor in oneTile.neighbors2)
                {
                    float mayDist = dist(pos, neighbor.transform.position);
                    if (mayDist < minDist)
                    {
                        minDist = mayDist;
                        bestTile = neighbor;
                    }
                }

                candidates.TryAdd(minDist, bestTile);
            }
        }

        if (candidates.Count == 0)
            return null;

        Tile candidate = candidates.Values.First();

        // check if the point is inside the tile using vector math

        float halfSizeLength = 0.5f / Mathf.Sqrt(3);

        // find all vertices
        Vector3 myPos = candidate.transform.position;
        List<Vector2> vertices = new List<Vector2>() {
            // top
            new Vector2(myPos.x, myPos.y + halfSizeLength * 2),

            // top left
            new Vector2(myPos.x - horizontalDif, myPos.y + halfSizeLength),

            // bottom left
            new Vector2(myPos.x - horizontalDif, myPos.y - halfSizeLength),

            // bottom
            new Vector2(myPos.x, myPos.y - halfSizeLength * 2),

            // bottom right
            new Vector2(myPos.x + horizontalDif, myPos.y - halfSizeLength),

            // top right
            new Vector2(myPos.x + horizontalDif, myPos.y + halfSizeLength),

            // top again
            new Vector2(myPos.x, myPos.y + halfSizeLength * 2),
        };

        for (int i = 0; i < vertices.Count - 1; i++)
        {
            // find vectors for cross product
            Vector2 v0 = vertices[i] - pos;
            Vector2 v1 = vertices[i + 1] - pos;
            if (Vector3.Cross(v0, v1).z < 0)
                return null;
        }
        return candidate;
    }

    //get world position from row col
    public Vector2 getWorldPosition(Vector2 indices)
    {
        return new Vector2(indices.x * horizontalDif, indices.y * verticalDif + (indices.x % 2 * verticalDif / 2));
    }

    //find distance between two vector2
    public float dist(Vector2 v1, Vector2 v2)
    {
        return Mathf.Sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.y - v2.y) * (v1.y - v2.y));
    }

    //find distance between two tiles
    public float dist(Tile t1, Tile t2)
    {
        Vector2 p1 = t1.transform.position;
        Vector2 p2 = t2.transform.position;
        return Mathf.Sqrt((p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y));
    }
}
