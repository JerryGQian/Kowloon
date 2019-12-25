using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using csDelaunay;

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

    public int polygonNumber = 50;

    public int seed = 1337;
    public int zoneDim = 100;
    private int storageDim = 1000;
    private int halfStorageDim;
    public float streetWidth = 3;
    public float wallHeight = 3;

    private CoordRandom pointRand;

    public TileData[,] tiles;
    public bool[,] instantiated;
    private ArrayList tileList;


    // The number of polygons/sites we want
    

    // This is where we will store the resulting data
    private Dictionary<Vector2f, Site> sites;
    private List<Edge> edges;

    // Start is called before the first frame update
    void Start() {
        playerTransform = player.GetComponent<Transform>();

        halfStorageDim = (int)(storageDim / 2);

        //tiles = GeneratePaths(gridDim);
        instantiated = new bool[storageDim, storageDim];
        for (int i = 0; i < storageDim; i++) {
            for (int j = 0; j < storageDim; j++) {
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

        pointRand = new CoordRandom(seed);
    }

    // Update is called once per frame
    void Update() {
        //convert world coords to zoneIdx
        int currX = ((int)playerTransform.position.x / zoneDim);
        int currY = ((int)playerTransform.position.z / zoneDim);
        //Negative coords need adjustment by -1
        if (playerTransform.position.x < 0)
            currX--;
        if (playerTransform.position.z < 0)
            currY--;

        //check if in a zone that needs new Voronoi
        bool needsNewVoronoi = false;
        for (int i = -1; i <= 1; i++) {
            for (int j = -1; j <= 1; j++) {
                if (!instantiated[currX + i + halfStorageDim, currY + j + halfStorageDim])
                    needsNewVoronoi = true;
            }
        }

        needsNewVoronoi = !instantiated[currX + halfStorageDim, currY + halfStorageDim];

        if (needsNewVoronoi) {
            Voronoi voronoi = GenerateVoronoi(currX, currY);

            InstantiateVoronoi(voronoi, currX, currY);
            Debug.Log("Instantiating: " + currX + " " + currY);
            instantiated[currX + halfStorageDim, currY + halfStorageDim] = true;
        }
    }

    private void CreatePoints(int zoneX, int zoneY, List<Vector2f> points) {
        int[] arr = pointRand.GetInts(zoneX, zoneY, 1, zoneDim - 1, polygonNumber*2);

        Debug.Log("---------- " + zoneX * zoneDim + " " + zoneY * zoneDim);
        for (int i = 0; i < arr.Length; i+=2) {
            Debug.Log((float)((zoneX * zoneDim) + arr[i]) + " " + (float)((zoneY * zoneDim) + arr[i + 1]));
            points.Add(new Vector2f(
                (float)((zoneX * zoneDim) + arr[i]), 
                (float)((zoneY * zoneDim) + arr[i+1]) ));
        }
    }

    private Voronoi GenerateVoronoi(int zoneX, int zoneY) {
        // Create sites (the center of polygons)
        List<Vector2f> points = new List<Vector2f>();

        //generates points for 25 zones (5x5 around current)
        for (int i = -2; i <= 2; i++) {
            for (int j = -2; j <= 2; j++) {
                CreatePoints(zoneX + i, zoneY + j, points);
            }
        }

        Rectf bounds = new Rectf(
            (zoneX * zoneDim) - 2*zoneDim, (zoneY * zoneDim) - 2*zoneDim,
            5*zoneDim, 5*zoneDim);


        // There is a two ways you can create the voronoi diagram: with or without the lloyd relaxation
        // Here I used it with 2 iterations of the lloyd relaxation
        Voronoi voronoi = new Voronoi(points, bounds, 0);

        return voronoi;
    }

    private void InstantiateVoronoi(Voronoi voronoi, int zoneX, int zoneY) {
        Dictionary<Vector2f, Site> sites = voronoi.SitesIndexedByLocation;
        List<Edge> edges = voronoi.Edges;

        Vector2f lowerBound = new Vector2f((zoneX-1)*zoneDim, (zoneY-1)*zoneDim);
        Vector2f upperBound = new Vector2f((zoneX + 2) * zoneDim, (zoneY + 2) * zoneDim);

        foreach (KeyValuePair<Vector2f, Site> kv in sites) {
            Instantiate(
                    indicubeRed,
                    new Vector3(kv.Key.x, 0, kv.Key.y),
                    Quaternion.AngleAxis(0, Vector3.up));
        }
        foreach (Edge edge in edges) {
            // if the edge doesn't have clippedEnds, if was not within the bounds, dont draw it
            if (edge.ClippedEnds == null) continue;
            if (!EdgeInBound(edge, lowerBound, upperBound)) continue;

            InstantiateRoad(edge.ClippedEnds[LR.LEFT], edge.ClippedEnds[LR.RIGHT]);
            InstantiateWall(edge.ClippedEnds[LR.LEFT], edge.ClippedEnds[LR.RIGHT]);
        }

    }

    // Check if both corners are within the 3x3 zones to ensure no funkiness
    private bool EdgeInBound(Edge edge, Vector2f lower, Vector2f upper) {
        float leftX = edge.ClippedEnds[LR.LEFT].x;
        float leftY = edge.ClippedEnds[LR.LEFT].y;
        float rightX = edge.ClippedEnds[LR.RIGHT].x;
        float rightY = edge.ClippedEnds[LR.RIGHT].y;

        return !(leftX < lower.x || leftX > upper.x ||
            leftY < lower.y || leftY > upper.y ||
            rightX < lower.x || rightX > upper.x ||
            rightY < lower.y || rightY > upper.y);
    }

    private void InstantiateRoad(Vector2f p0, Vector2f p1) {
        float x0 = (float)p0.x;
        float y0 = (float)p0.y;
        float x1 = (float)p1.x;
        float y1 = (float)p1.y;

        float x = (float)(x0 + x1) / 2;
        float y = (float)(y0 + y1) / 2;

        Vector2 vec = new Vector2(x1 - x0, y1 - y0);

        float angle = Mathf.Atan2(x1-x0,y1-y0) * Mathf.Rad2Deg + 90;

        GameObject line = Instantiate(
                    cube,
                    new Vector3(x,0,y),
                    Quaternion.AngleAxis(angle, Vector3.up));
        line.transform.localScale = new Vector3(vec.magnitude, 1, streetWidth);
    }

    private void InstantiateWall(Vector2f p0, Vector2f p1) {
        float x0 = (float)p0.x;
        float y0 = (float)p0.y;
        float x1 = (float)p1.x;
        float y1 = (float)p1.y;

        float angle = Mathf.Atan2(x1 - x0, y1 - y0) * Mathf.Rad2Deg + 90;

        Vector2f diff = p1 - p0;
        float magnitude = diff.magnitude;
        Vector3 perpAdj = new Vector3(diff.y, 0, diff.x);
        perpAdj.Normalize();

        for (int i = 2; i < magnitude/2 -2; i++) {
            GameObject wall1 = Instantiate(
                    cube,
                    new Vector3(
                        p0.x + diff.x*(i*2/magnitude), 
                        wallHeight/2, 
                        p0.y + diff.y * (i * 2 / magnitude)) 
                    + perpAdj*streetWidth/2,
                    Quaternion.AngleAxis(angle, Vector3.up));
            wall1.transform.localScale = new Vector3(2, wallHeight, 0.5f);

            GameObject wall2 = Instantiate(
                    cube,
                    new Vector3(
                        p0.x + diff.x * (i * 2 / magnitude),
                        wallHeight / 2,
                        p0.y + diff.y * (i * 2 / magnitude))
                    - perpAdj * streetWidth / 2,
                    Quaternion.AngleAxis(angle, Vector3.up));
            wall2.transform.localScale = new Vector3(2, wallHeight, 0.5f);
        }

        
    }





}