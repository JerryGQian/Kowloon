using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public enum BorderState { Undefined, Closed, Opened };

public class WorldManager : MonoBehaviour {

    public GameObject player;
    private Transform playerTransform;
    public GameObject[] tilePref;
    public GameObject cube;

    public int zoneDim = 4;
    private CoordRandom zoneRand;
    private CoordRandom tileRand;
    public int seed = 100;

    private int curr = 0;
    //private GameObject[][] tiles = new GameObject[1000][1000]();

    public TileData[,] tiles;
    public bool[,] instantiated;
    private int gridDim = 40;
    private ArrayList tileList;

    private int width = 50;
    private int height = 50;


    // Start is called before the first frame update
    void Start() {
        playerTransform = player.GetComponent<Transform>();

        //tiles = GeneratePaths(gridDim);
        instantiated = new bool[gridDim, gridDim];
        for (int i = 0; i < gridDim; i++) {
            for (int j = 0; j < gridDim; j++) {
                instantiated[i, j] = false;
            }
        }

        tileList = new ArrayList();


        //testing perlin
        for (int i = 0; i < 10; i++) {
            Debug.Log(i + " " + Mathf.PerlinNoise(0.1f, ((float)i/10)));
        }

        zoneRand = new CoordRandom(seed);
        tileRand = new CoordRandom(seed+1);
        //Debug.Log(i + " " + crand.GetInt(i, j, 0, 4));

    }

    // Update is called once per frame
    void Update() {
        //convert world coords to index
        int currX = ((int)playerTransform.position.x / 10) + gridDim / 2;
        int currZ = ((int)playerTransform.position.z / 10) + gridDim / 2;

        /*
        //destroying and marking tiles for list deletion
        ArrayList toDelete = new ArrayList();
        foreach (Tuple<GameObject,int,int> tup in tileList) {
            GameObject currTile = tup.Item1;
            Vector3 diff = playerTransform.position - currTile.GetComponent<Transform>().position;
            
            if (diff.magnitude > 25f) {
                instantiated[tup.Item2, tup.Item3] = false;
                toDelete.Add(tup);
                Destroy(currTile);
            }
        }*/

        //instantiate new tiles
        InstantiateTile(currX, currZ, tileList);
        InstantiateTile(currX+1, currZ, tileList);
        InstantiateTile(currX+1, currZ + 1, tileList);
        InstantiateTile(currX, currZ + 1, tileList);
        InstantiateTile(currX-1, currZ + 1, tileList);
        InstantiateTile(currX-1, currZ, tileList);
        InstantiateTile(currX-1, currZ - 1, tileList);
        InstantiateTile(currX, currZ - 1, tileList);
        InstantiateTile(currX+1, currZ - 1, tileList);

        /*
        //remove from list existing list
        foreach (Tuple<GameObject, int, int> tup in toDelete) {
            tileList.Remove(tup);
        }*/
    }

    private GameObject InstantiateTile(int x, int y, ArrayList list) {
        GameObject newTile = null;
        TileData tdata = GetTileData(x,y);

        if (!instantiated[x, y]) {
            /*int prefIdx = DetermineTileType(tiles[x, y].path);
            Debug.Log("prefab idx " + prefIdx);*/
            

            if (tdata.hasAve) {
                float midRatio = (tdata.aveLRatio + tdata.aveRRatio)/ 2;
                GameObject cb1 = Instantiate(
                    cube,
                    new Vector3((x - gridDim / 2) * 10, -25, (y - gridDim / 2 + midRatio) * 10 + 2.5f),
                    Quaternion.AngleAxis(tdata.aveAngle, Vector3.up));

                GameObject cb2 = Instantiate(
                    cube,
                    new Vector3((x - gridDim / 2) * 10, -25, (y - gridDim / 2 + midRatio) * 10 - 2.5f),
                    Quaternion.AngleAxis(tdata.aveAngle, Vector3.up));
                GameObject cb3 = Instantiate(
                    cube,
                    new Vector3((x - gridDim / 2) * 10, -50, (y - gridDim / 2 + midRatio) * 10),
                    Quaternion.AngleAxis(tdata.aveAngle, Vector3.up));

                cb1.transform.localScale = new Vector3(1, 50, 10);
                cb2.transform.localScale = new Vector3(1, 50, 10);
                cb3.transform.localScale = new Vector3(10, 1, 15);
            }
            else {
                int prefIdx = 12;

                newTile = Instantiate(
                    tilePref[prefIdx],
                    new Vector3((x - gridDim / 2) * 10, 0, (y - gridDim / 2) * 10),
                    Quaternion.identity);

                instantiated[x, y] = true;

                list.Add(new Tuple<GameObject, int, int>(newTile, x, y));
            }
            
        }

        return newTile;
    }

    
    private TileData GetTileData(int x, int y) {
        TileData tile = new TileData();

        int zoneX = x >= 0 ? (x / 4) : (((x + 1) / 4) - 1);
        int zoneY = y >= 0 ? (y / 4) : (((y + 1) / 4) - 1);

        //if in even ave zone
        if ( zoneY % 2 == 0 ) {
            int[] endPoints = GetAveEndPoints(zoneX, zoneY);
            float vertDiff = (endPoints[1] - endPoints[0]);
            float slope = vertDiff / zoneDim;

            
            int relX = x % zoneDim;
            int relY = y % zoneDim;

            float loc1 = endPoints[0] + (relX * slope);
            tile.aveLRatio = loc1 - relY;
            float loc2 = endPoints[0] + ((relX+1) * slope);
            tile.aveRRatio = loc2 - relY;

            tile.aveAngle = -1 * Mathf.Atan2(vertDiff, zoneDim) * Mathf.Rad2Deg + 90;

            if ((tile.aveLRatio >= 0 && tile.aveLRatio <= 1) || 
                (tile.aveRRatio >= 0 && tile.aveRRatio <= 1)) {
                tile.hasAve = true;
            }

            for (int i = 0; i < 4; i++) {
                tile.path[i] = true;
            }

        }

        return tile;
    }

    private int[] GetAveEndPoints(int zoneX, int zoneY) {
        int loc1 = zoneRand.GetInt(zoneX, zoneY, 1, 3);
        int loc2 = zoneRand.GetInt(zoneX+1, zoneY, 1, 3);

        return new int[] { loc1, loc2 };
    }

    public class TileData {
        public bool[] path;
        public bool hasAve;
        public float aveAngle; //0=horizontal -> 90=vertical
        public float aveLRatio;
        public float aveRRatio;

        public TileData() {
            path = new bool[4] { false, false, false, false };
            hasAve = false;
            
        }
    }

}