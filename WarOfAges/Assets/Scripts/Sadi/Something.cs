using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using System.Text;

public class Something : MonoBehaviour
{
    public static Something instance;


    public Tile[,] tiles;

    //building blocks
    public const float tileSize = 1;

    [SerializeField] GameObject landTilePrefab;
    [SerializeField] GameObject waterTilePrefab;

    float rowHexDiff = 0.75F;
    float colHexDiff = (float)(Math.Sqrt(3.0F) / 2.0F);
    

    public void makeGrid_RPC(int rows, int cols)
    {

        System.Random trueSeed = new System.Random();
        int seed = trueSeed.Next(0, 1000000);
        float frequency = 3.0F;

        //make map
        tiles = new Tile[rows, cols];

        GameObject parent = new GameObject("Map");

        //generate the grid using instruction
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                Vector2 subpos = worldFromIndices(i, j);

                Vector3 pos = new Vector3(subpos.x, subpos.y, 0);
                    
                float someY = frequency *  i / rows + seed;
                float someX = frequency * j / cols + seed;

                float noiseNum = Mathf.PerlinNoise( someX, someY);

                // Debug.Log("(" + (someX) + ", " + (someY) + "): " + noiseNum);
                //instantiate
                if (noiseNum >= 0.33)
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

                tiles[i, j].GetComponent<Tile>().pos = new Vector2Int(j, i);
            }
        }

        for (int row = 0; row < tiles.GetLength(0); row++)
        {
            for (int col = 0; col < tiles.GetLength(1); col++){
                tiles[row, col].neighbors = getNeighbors(row, col);
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
                    tiles[row, col].neighbors2 = findNeighbors2(row, col);
                }
            }
        }
    }

    //get the tile depending on world position TBContinued
    public Tile getTile(Vector2 pos)
    {
        //simple division to find rough x and y
        int roundRow = (int)((pos.y - this.transform.position.y) / (0.75 * tileSize));
        int roundCol = (int)((pos.x - this.transform.position.x) / (tileSize * colHexDiff) - ((roundRow%2) * 0.5F));
        //compare with all neighbors
        Tile oneTile = tiles[roundRow, roundCol];

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

    //get world position from row col Changed
    public Vector2 getWorldPosition(Tile tile)
    {
        Vector3 basePlace = this.transform.position;
        float xShift = tileSize * colHexDiff * (tile.pos.x + (tile.pos.y % 2) * 0.5F);
        float yShift = rowHexDiff * tile.pos.y * tileSize;
        return new Vector2(basePlace.x + xShift, basePlace.y + yShift);
    }
    public Vector2 worldFromIndices(int row, int col) // Can be used to get theoretical indices and null spots
    {
        Vector3 basePlace = this.transform.position;
        float xShift = tileSize * colHexDiff * (col + (row % 2) * 0.5F);
        float yShift = rowHexDiff * row * tileSize;
        return new Vector2(basePlace.x + xShift, basePlace.y + yShift);
    }

    //find distance between two vector2
    float dist(Vector2 v1, Vector2 v2)
    {
        return Mathf.Sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.y - v2.y) * (v1.y - v2.y));
    }


    public List<int[]> getNeighborsIndex (int row, int col){ // Edited for new paradigm
        List<int[]> neighbors = new List<int[]>();
        Vector2 source = worldFromIndices(row, col);
        for (int i = row - 1; i <= row + 1 && i < tiles.GetLength(0); i++){
            for (int j = col - 1; j <= col + 1 && j < tiles.GetLength(1); j++){
                // Debug.Log("Looking at: " + i + ", " + j);
                if (i >= 0 && j >= 0){
                    Vector2 testing = worldFromIndices(i, j);
                    // Debug.Log("Distance = " + dist(source, testing));
                    if (dist(source, testing) < 0.87 * tileSize && dist(source, testing) > 0){
                        neighbors.Add(new int[] {i, j});
                    }
                }
            }
        }
        return neighbors;
    }

    public List<Tile> getNeighbors (int row, int col){ // done
        List<int[]> potential = getNeighborsIndex(row, col);
        List<Tile> neighbors = new List<Tile>();
        foreach (int[] test in potential){
            if(tiles[test[0], test[1]] != null){
                neighbors.Add(tiles[test[0], test[1]]);
            }
        }
        return neighbors;
    }

    public List<Tile> findNeighbors2 (int row, int col){ // issue at 0,0 but works otherwise
        List<int[]> potential = new List<int[]>();
        Vector2 source = getWorldPosition(tiles[row, col]);
        for (int i = row - 2; i <= row + 2 && i < tiles.GetLength(0); i++){
            for (int j = col - 2; j <= col + 2 && j < tiles.GetLength(1); j++){
                // Debug.Log("Looking at: " + i + ", " + j);
                if (i >= 0 && j >= 0){
                    Vector2 testing = worldFromIndices(i, j);
                    // Debug.Log("Distance = " + dist(source, testing));
                    if (dist(source, testing) < 0.87 * tileSize * 2 && dist(source, testing) > 0){
                        potential.Add(new int[] {i, j});
                    }
                }
            }
        }
        List<Tile> neighbors = new List<Tile>();
        foreach (int[] test in potential){
            if(tiles[test[0], test[1]] != null){
                neighbors.Add(tiles[test[0], test[1]]);
            }
        }
        return neighbors;
    }

    public void hexMap(int radius){
        int size = radius * 2;
        tiles = new Tile[size, size];

        System.Random trueSeed = new System.Random();
        int seed = trueSeed.Next(0, 1000000);
        float frequency = 3.0F;

        LinkedList<int[]> points = new LinkedList<int[]>();
        int mid = size / 2;
        points.AddLast(new int[] {mid,mid});
        int count = 1;
        for (int gen = 0; gen < radius; gen++){
            int nextCount = 0;
            for(int i = 0; i < count; i++){
                int[] point = points.First.Value;

                Vector2 subpos = worldFromIndices(point[0], point[1]);
                Vector3 pos = new Vector3(subpos.x, subpos.y, 0);

                float someY = frequency *  point[0] / size + seed;
                float someX = frequency * point[1] / size +  seed;

                float noiseNum = Mathf.PerlinNoise( someX, someY);
                Debug.Log(noiseNum);

                if (noiseNum > 0.33){
                    tiles[point[0], point[1]] = Instantiate(landTilePrefab, pos, Quaternion.identity).GetComponent<Tile>();
                    tiles[point[0], point[1]].terrain = "land";
                }
                else{
                    tiles[point[0], point[1]] = Instantiate(waterTilePrefab, pos, Quaternion.identity).GetComponent<Tile>();
                    tiles[point[0], point[1]].terrain = "water";
                }
/*
                if (gen > (2.0F/3.0F) * radius){
                    tiles[point[0], point[1]] = Instantiate(landTilePrefab, pos, Quaternion.identity).GetComponent<Tile>();
                    tiles[point[0], point[1]].terrain = "land";
                }
                else{
                    tiles[point[0], point[1]] = Instantiate(waterTilePrefab, pos, Quaternion.identity).GetComponent<Tile>();
                    tiles[point[0], point[1]].terrain = "water";
                }
*/
                List<int[]> mayNeigh = getNeighborsIndex(point[0], point[1]);
                foreach (int[] neigh in mayNeigh){
                    if (tiles[neigh[0], neigh[1]] == null){
                       //  Debug.Log(neigh[0] + ", " + neigh[1]);
                        points.AddLast(new int[]{neigh[0], neigh[1]});
                        nextCount += 1;
                    }
                }
                tiles[point[0], point[1]].neighbors = getNeighbors(point[0], point[1]);
                points.RemoveFirst();
            }
            count = nextCount;
        }
    }

    void Start(){
      /*
        makeGrid_RPC(3, 3);
        foreach (Tile spot in tiles){
            Vector2 testy = getWorldPosition(tiles[spot.pos.y, spot.pos.x]);
            Vector2 testb = worldFromIndices(spot.pos.y, spot.pos.x);
            Debug.Log(testy.x + ", " + testy.y +" OR "+ testb.x + ", " + testb.y);
            // List<int[]> someNeighs = getNeighborsIndex(spot.pos.y, spot.pos.x);
            // Debug.Log("Count of potential: " + someNeighs.Count);
            Debug.Log("Neighbor Count: " + spot.neighbors.Count);
            Debug.Log("Neighbor 2 Count: " + spot.neighbors2.Count);
        }
      */
        // makeGrid_RPC(25, 20);
        hexMap(10);
    }
}
