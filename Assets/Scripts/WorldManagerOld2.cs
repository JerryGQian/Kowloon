using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public enum BorderState { Undefined, Closed, Opened };

public class WorldManagerOld2 : MonoBehaviour {

    public GameObject player;
    private Transform playerTransform;
    public GameObject[] tilePref;
    public GameObject cube;
    public GameObject[] buildingPref;
    public GameObject[] aveBuildingPref;
    public GameObject[] aveGroundPref;
    public GameObject indicubeRed;
    public GameObject indicubeGreen;

    public int seed = 100;
    public int zoneDim = 4;
    public int tileDim = 10; //should be even!
    private int halfTileDim;
    private Vector3 coordAdj;
    private int gridDim = 1000;
    private CoordRandom zoneRand;
    private CoordRandom tileRand;
    

    private int curr = 0;
    //private GameObject[][] tiles = new GameObject[1000][1000]();

    public TileData[,] tiles;
    public bool[,] instantiated;
    
    private ArrayList tileList;

    private int width = 50;
    private int height = 50;


    // Start is called before the first frame update
    void Start() {
        playerTransform = player.GetComponent<Transform>();

        halfTileDim = (int)(tileDim / 2);
        coordAdj = new Vector3(halfTileDim, 0, halfTileDim);
        //coordAdj = new Vector3(0, 0, 0);

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
        //Negative coords need adjustment by -1
        if (playerTransform.position.x < 0) 
            currX--;
        if (playerTransform.position.z < 0)
            currZ--;

        //Debug.Log(playerTransform.position.x + " " + currX);

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

        //Debug.Log("Curr Tile: " + currX + " " + currZ);
        //instantiate new tiles
        InstantiateTile(currX, currZ, tileList);
        /*InstantiateTile(currX+1, currZ, tileList);
        InstantiateTile(currX+1, currZ + 1, tileList);
        InstantiateTile(currX, currZ + 1, tileList);
        InstantiateTile(currX-1, currZ + 1, tileList);
        InstantiateTile(currX-1, currZ, tileList);
        InstantiateTile(currX-1, currZ - 1, tileList);
        InstantiateTile(currX, currZ - 1, tileList);
        InstantiateTile(currX+1, currZ - 1, tileList);*/

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
            Debug.Log(x + " " + y + " L:" + tdata.aveLRatio + " R:" + tdata.aveRRatio);
            //Avenue construction
            if (tdata.hasAve) {
                Instantiate(
                    indicubeRed,
                    new Vector3((x - gridDim / 2) * 10, 3, (y - gridDim / 2) * 10) + coordAdj,
                    Quaternion.AngleAxis(0, Vector3.up));

                instantiated[x, y] = true;
                float midRatio = (tdata.aveLRatio + tdata.aveRRatio) / 2;

                GameObject crevasseFloor = Instantiate(
                    cube,
                    new Vector3((x - gridDim / 2) * 10, -50, (y - gridDim / 2 + midRatio) * 10) + coordAdj,
                    Quaternion.AngleAxis(tdata.aveAngle, Vector3.up));
                crevasseFloor.transform.localScale = new Vector3(10, 1, 15);

                //build bridge
                if (tdata.hasBridge) {
                    GameObject bridge = Instantiate(
                        cube,
                        new Vector3((x - gridDim / 2) * 10 - tileDim/2, 0, (y - gridDim / 2 + midRatio) * 10) + coordAdj,
                        Quaternion.AngleAxis(0, Vector3.up));
                    bridge.transform.localScale = new Vector3(4, 0.3f, 15);
                }

                GameObject[] aveGroundTiles = new GameObject[10];
                GameObject[] aveBuildingTiles = new GameObject[10];
                
                for (int i = 0; i < 5; i++) {
                    int adj = i - 2;

                    //topside
                    aveGroundTiles[i] = Instantiate(
                        aveGroundPref[0],
                        new Vector3((x - gridDim / 2) * 10 + (2 * adj), 0, (y - gridDim / 2 + midRatio) * 10 - (tileDim / 2) + 6f + (adj * 2f * tdata.aveSlope)) + coordAdj,
                        Quaternion.AngleAxis(tdata.aveAngle, Vector3.up));
                    aveGroundTiles[i].transform.localScale = new Vector3(1, 1, 0.3f * Mathf.Abs(tdata.aveSlope) + 1f);

                    if ((i == 0 && !tdata.path[1]) || (i == 4 && !tdata.path[3]) || (i != 0 && i != 4)) {
                        aveBuildingTiles[i] = Instantiate(
                            aveBuildingPref[0],
                            new Vector3((x - gridDim / 2) * 10 + (2 * adj), 0, (y - gridDim / 2 + midRatio) * 10 - (tileDim / 2) + 6f + (2 * adj * tdata.aveSlope)) + coordAdj,
                            Quaternion.AngleAxis(tdata.aveAngle, Vector3.up));
                        aveBuildingTiles[i].transform.localScale = new Vector3(1, 1, 0.3f * Mathf.Abs(tdata.aveSlope) + 1f);
                    }


                    //bottom side
                    aveGroundTiles[i + 5] = Instantiate(
                        aveGroundPref[0],
                        new Vector3((x - gridDim / 2) * 10 + (2 * adj), 0, (y - gridDim / 2 + midRatio) * 10 - (tileDim/2) - 6f + (adj * 2f * tdata.aveSlope)) + coordAdj,
                        Quaternion.AngleAxis(tdata.aveAngle + 180, Vector3.up));
                    aveGroundTiles[i + 5].transform.localScale = new Vector3(1, 1, 0.3f * Mathf.Abs(tdata.aveSlope) + 1f);

                    if ((i == 0 && !tdata.path[1]) || (i == 4 && !tdata.path[3]) || (i != 0 && i != 4)) {
                        aveBuildingTiles[i + 5] = Instantiate(
                            aveBuildingPref[0],
                            new Vector3((x - gridDim / 2) * 10 + (2 * adj), 0, (y - gridDim / 2 + midRatio) * 10 - (tileDim / 2) - 6f + (2 * adj * tdata.aveSlope)) + coordAdj,
                            Quaternion.AngleAxis(tdata.aveAngle + 180, Vector3.up));
                        aveBuildingTiles[i + 5].transform.localScale = new Vector3(1, 1, 0.3f * Mathf.Abs(tdata.aveSlope) + 1f);
                    }
                }
            }
            else if (tdata.affectedAve) {
                instantiated[x, y] = true;

                Instantiate(
                    indicubeGreen,
                    new Vector3((x - gridDim / 2) * 10 +0.5f, 3, (y - gridDim / 2) * 10) + coordAdj,
                    Quaternion.AngleAxis(0, Vector3.up));

            }
            else { 
                instantiated[x, y] = true;
                int prefIdx = 12;

                newTile = Instantiate(
                    tilePref[prefIdx],
                    new Vector3((x - gridDim / 2) * 10, 0, (y - gridDim / 2) * 10) + coordAdj,
                    Quaternion.identity);


                if (tdata.path[3]) {
                    GameObject wE2 = Instantiate(
                        buildingPref[0],
                        new Vector3((x - gridDim / 2) * 10 + 3, 0, (y - gridDim / 2) * 10 + 2) + coordAdj,
                        Quaternion.identity);
                    GameObject wE3 = Instantiate(
                        buildingPref[0],
                        new Vector3((x - gridDim / 2) * 10 + 3, 0, (y - gridDim / 2) * 10) + coordAdj,
                        Quaternion.identity);
                    GameObject wE4 = Instantiate(
                        buildingPref[0],
                        new Vector3((x - gridDim / 2) * 10 + 3, 0, (y - gridDim / 2) * 10 - 2) + coordAdj,
                        Quaternion.identity);

                    if (!tdata.path[0]) {
                        GameObject wE1 = Instantiate(
                            buildingPref[0],
                            new Vector3((x - gridDim / 2) * 10 + 3, 0, (y - gridDim / 2) * 10 + 4) + coordAdj,
                            Quaternion.identity);
                    }

                    if (!tdata.path[2]) {
                        GameObject wE5 = Instantiate(
                            buildingPref[0],
                            new Vector3((x - gridDim / 2) * 10 + 3, 0, (y - gridDim / 2) * 10 - 4) + coordAdj,
                            Quaternion.identity);
                    }
                }

                if (tdata.path[2]) {
                    GameObject wS2 = Instantiate(
                        buildingPref[0],
                        new Vector3((x - gridDim / 2) * 10 + 2, 0, (y - gridDim / 2) * 10 - 3) + coordAdj,
                        Quaternion.AngleAxis(90, Vector3.up));
                    GameObject wS3 = Instantiate(
                        buildingPref[0],
                        new Vector3((x - gridDim / 2) * 10, 0, (y - gridDim / 2) * 10 - 3) + coordAdj,
                        Quaternion.AngleAxis(90, Vector3.up));
                    GameObject wS4 = Instantiate(
                        buildingPref[0],
                        new Vector3((x - gridDim / 2) * 10 - 2, 0, (y - gridDim / 2) * 10 - 3) + coordAdj,
                        Quaternion.AngleAxis(90, Vector3.up));

                    if (!tdata.path[3]) {
                        GameObject wS1 = Instantiate(
                            buildingPref[0],
                            new Vector3((x - gridDim / 2) * 10 + 4, 0, (y - gridDim / 2) * 10 -3) + coordAdj,
                            Quaternion.identity);
                    }

                    if (!tdata.path[1]) {
                        GameObject wS5 = Instantiate(
                            buildingPref[0],
                            new Vector3((x - gridDim / 2) * 10 - 4, 0, (y - gridDim / 2) * 10 - 3) + coordAdj,
                            Quaternion.identity);
                    }
                }

                GameObject wW2 = Instantiate(
                    buildingPref[0],
                    new Vector3((x - gridDim / 2) * 10 - 3, 0, (y - gridDim / 2) * 10 + 2) + coordAdj,
                    Quaternion.AngleAxis(180, Vector3.up));
                GameObject wW3 = Instantiate(
                    buildingPref[0],
                    new Vector3((x - gridDim / 2) * 10 - 3, 0, (y - gridDim / 2) * 10) + coordAdj,
                    Quaternion.AngleAxis(180, Vector3.up));
                GameObject wW4 = Instantiate(
                    buildingPref[0],
                    new Vector3((x - gridDim / 2) * 10 - 3, 0, (y - gridDim / 2) * 10 - 2) + coordAdj,
                    Quaternion.AngleAxis(180, Vector3.up));

                GameObject wN2 = Instantiate(
                    buildingPref[0],
                    new Vector3((x - gridDim / 2) * 10 + 2, 0, (y - gridDim / 2) * 10 + 3) + coordAdj,
                    Quaternion.AngleAxis(90, Vector3.up));
                GameObject wN3 = Instantiate(
                    buildingPref[0],
                    new Vector3((x - gridDim / 2) * 10, 0, (y - gridDim / 2) * 10 + 3) + coordAdj,
                    Quaternion.AngleAxis(90, Vector3.up));
                GameObject wN4 = Instantiate(
                    buildingPref[0],
                    new Vector3((x - gridDim / 2) * 10 - 2, 0, (y - gridDim / 2) * 10 + 3) + coordAdj,
                    Quaternion.AngleAxis(90, Vector3.up));



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

            tile.aveAngle = -1 * Mathf.Atan2(vertDiff, zoneDim) * Mathf.Rad2Deg + 90;
            tile.aveSlope = slope;
            
            int relX = x % zoneDim;
            int relY = y % zoneDim;

            float loc1 = endPoints[0] + (relX * slope);
            tile.aveLRatio = loc1 - relY;
            float loc2 = endPoints[0] + ((relX+1) * slope);
            tile.aveRRatio = loc2 - relY;

            for (int i = 0; i < 4; i++) {
                tile.path[i] = true;
            }

            if ((tile.aveLRatio >= 0f && tile.aveLRatio <= 1f) ||
                (tile.aveRRatio >= 0f && tile.aveRRatio <= 1f)) {
                tile.hasAve = true;

                if (relX == 0) {
                    tile.hasBridge = true;
                }
            }
            if ((tile.aveLRatio >= -1f && tile.aveLRatio <= 2f) || 
                (tile.aveRRatio >= -1f && tile.aveRRatio <= 2f)) {
                tile.affectedAve = true;
                tile.path[0] = false;
                tile.path[2] = false;
            }

            

        }
        else {
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
        public bool affectedAve;
        public bool hasBridge;
        public float aveAngle; //0=horizontal -> 90=vertical
        public float aveSlope;
        public float aveLRatio;
        public float aveRRatio;

        public TileData() {
            path = new bool[4] { false, false, false, false };
            hasAve = false;
            
        }
    }

}