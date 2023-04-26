using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Photon.Pun;
using System.Text;

public class TileManager : MonoBehaviourPunCallbacks
{
    public static TileManager instance;

    public PhotonView PV;

    public Tile[,] tiles;

    //building blocks
    public const float tileSize = 1;

    [SerializeField] GameObject landTilePrefab;
    [SerializeField] GameObject waterTilePrefab;

    void Awake()
    {
        instance = this;

        PV = GetComponent<PhotonView>();
    }

    public void makeGrid()
    {
        //store type of tiles using bit
        StringBuilder instruction = new StringBuilder();

        // I will need a seed (int or long), a frequency (an int > 1), and maybe an amplitude if I want to scale up values to integers
        var trueSeed = new Random();
        int seed = trueSeed.Next(0, Int32.MaxValue);
        float frequency = 10f;
        int amplitude = 1;




        int rows = -1;
        int cols = -1;

        //decide map
        if ((bool)PhotonNetwork.CurrentRoom.CustomProperties["Water"])
        {
            rows = 25;
            cols = 8;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    float noiseNum = Math.PerlinNoise(frequency *  col / cols + seed, frequency * row / rows + seed);
                        //assign type
                    if (noiseNum >= 0){
                        instruction.Append(0);
                    }
                    else{
                        instruction.Append(1);
                    }
                }
            }
        }
        else
        {
            rows = 19;
            cols = 6;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    instruction.Append(0);
                }
            }
        }

        PV.RPC(nameof(makeGrid_RPC), RpcTarget.AllViaServer, rows, cols, instruction.ToString());
    }

    [PunRPC]
    public void makeGrid_RPC(int rows, int cols, string instruction)
    {
        //setting camera
        if ((bool)PhotonNetwork.CurrentRoom.CustomProperties["Water"])
        {
            Camera.main.orthographicSize = 8.5f;
            Camera.main.transform.position = new Vector3(5, 7f, -10);
        }
        else
        {
            Camera.main.orthographicSize = 6.5f;
            Camera.main.transform.position = new Vector3(4, 5.25f, -10);
        }

        //make map
        tiles = new Tile[rows, cols];

        GameObject parent = new GameObject("Map");

        int count = 0;

        //generate the grid using instruction
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                //skip bottom row
                if (!(i % 2 == 0 && j == 0))
                {
                    float xPos = i * 0.5f * tileSize;
                    float yPos = j * Mathf.Sqrt(3f) * tileSize + (i % 2 * Mathf.Sqrt(3f) / 2 * tileSize);

                    Vector3 pos = new Vector3(xPos, yPos, 0);

                    //instantiate
                    if (instruction[count] == '0')
                    {
                        tiles[i, j] = Instantiate(landTilePrefab, pos, Quaternion.identity).GetComponent<Tile>();
                        tiles[i, j].terrain = "land";
                    }
                    else
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
                //skip bottom row
                if (!(row % 2 == 0 && col == 0))
                {
                    List<Tile> neighbors = tiles[row, col].GetComponent<Tile>().neighbors;

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

                    //remove null due to skip bottom row
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
                //skip bottom row
                if (!(row % 2 == 0 && col == 0))
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

    //get the tile depending on world position
    public Tile getTile(Vector2 pos)
    {
        //simple division to find rough x and y
        int roundX = (int) (pos.x / 0.5f / tileSize);

        int roundY = (int) (pos.y / Mathf.Sqrt(3f) / tileSize);

        if (roundX < 0 || roundX >= tiles.GetLength(0) || roundY < 0 || roundY >= tiles.GetLength(1))
        {
            return null;
        }

        //compensate for the row skipped
        if (roundY == 0)
        {
            roundY++;
        }

        //compare with all neighbors
        Tile oneTile = tiles[roundX, roundY];

        Tile bestTile = oneTile;

        float minDist = dist(pos, getWorldPosition(oneTile));

        foreach (Tile neighbor in oneTile.neighbors)
        {
            float mayDist = dist(pos, getWorldPosition(neighbor));
            if (mayDist < minDist)
            {
                minDist = mayDist;
                bestTile = neighbor;
            }
        }
        foreach (Tile neighbor in oneTile.neighbors2)
        {
            float mayDist = dist(pos, getWorldPosition(neighbor));
            if (mayDist < minDist)
            {
                minDist = mayDist;
                bestTile = neighbor;
            }
        }
        return bestTile;
    }

    //get world position from row col
    public Vector2 getWorldPosition(Tile tile)
    {
        return new Vector2(tile.pos.x * 0.5f, tile.pos.y * Mathf.Sqrt(3f) + (tile.pos.x % 2 * Mathf.Sqrt(3f) / 2));
    }

    //find distance between two vector2
    float dist(Vector2 v1, Vector2 v2)
    {
        return Mathf.Sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.y - v2.y) * (v1.y - v2.y));
    }


    public List<int[]> getNeighbors (int rows, int cols){
        List<int[]> neighbors = new List<int[]>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                //skip bottom row
                if (!(row % 2 == 0 && col == 0))
                {
                    //left and right
                    if (row >= 2)
                    { neighbors.Add(new int []{row - 2, col});}
                    if (row < rows - 2)
                    {neighbors.Add(new int[]{row + 2, col}); }

                    if (row % 2 == 0)
                    {//there is a row before it
                        if (row > 0)
                        {   neighbors.Add(new int[]{row - 1, col});
                            //even row decrease col
                            if (col >= 1)
                            { neighbors.Add(new int[]{row - 1, col - 1}); }
                        }
                        //there is a row after it
                        if (row < rows - 1)
                        {
                            neighbors.Add(new int[]{row + 1, col});

                            //even row decrease col
                            if (col >= 1)
                            {
                                neighbors.Add(new int[]{row + 1, col - 1});
                            }
                        }
                    }
                    else
                    {
                        //there is a row before it
                        if (row > 0)
                        {
                            neighbors.Add(new int[]{row - 1, col});

                            //odd row increase col
                            if (col < cols - 1)
                            {
                                neighbors.Add(new int[]{row - 1, col + 1});
                            }
                        }
                        //there is a row after it
                        if (row < rows - 1)
                        {
                            neighbors.Add(new int[]{row + 1, col});

                            //odd row increase col
                            if (col < cols - 1)
                            {
                                neighbors.Add(new int[]{row + 1, col + 1});
                            }
                        }
                    }

                    //remove null due to skip bottom row
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
        return neighbors;
    }
}
