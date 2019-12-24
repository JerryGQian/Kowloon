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

        /*
        //testing perlin
        for (int i = 0; i < 10; i++) {
            Debug.Log(i + " " + Mathf.PerlinNoise(0.1f, ((float)i / 10)));
        }
        */
        zoneRand = new CoordRandom(seed);
        tileRand = new CoordRandom(seed + 1);
        

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

        
        InstantiateTile(currX, currZ, tileList);
    }

    private GameObject InstantiateTile(int x, int y, ArrayList list) {
        GameObject newTile = null;
        //TileData tdata = GetTileData(x, y);

        if (!instantiated[x, y]) {
            

        }

        return newTile;
    }



}