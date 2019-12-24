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


    // The number of polygons/sites we want
    public int polygonNumber = 200;

    // This is where we will store the resulting data
    private Dictionary<Vector2f, Site> sites;
    private List<Edge> edges;

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


        //////////////////////////
        ///
        // Create your sites (lets call that the center of your polygons)
        List<Vector2f> points = CreateRandomPoint();

        // Create the bounds of the voronoi diagram
        // Use Rectf instead of Rect; it's a struct just like Rect and does pretty much the same,
        // but like that it allows you to run the delaunay library outside of unity (which mean also in another tread)
        Rectf bounds = new Rectf(0, 0, 512, 512);

        // There is a two ways you can create the voronoi diagram: with or without the lloyd relaxation
        // Here I used it with 2 iterations of the lloyd relaxation
        Voronoi voronoi = new Voronoi(points, bounds, 5);

        // But you could also create it without lloyd relaxtion and call that function later if you want
        //Voronoi voronoi = new Voronoi(points,bounds);
        //voronoi.LloydRelaxation(5);

        // Now retreive the edges from it, and the new sites position if you used lloyd relaxtion
        sites = voronoi.SitesIndexedByLocation;
        edges = voronoi.Edges;
        Debug.Log(sites);
        Debug.Log(edges[0]);

        InstantiateVoronoi(edges, sites);
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

    private List<Vector2f> CreateRandomPoint() {
        // Use Vector2f, instead of Vector2
        // Vector2f is pretty much the same than Vector2, but like you could run Voronoi in another thread
        List<Vector2f> points = new List<Vector2f>();
        for (int i = 0; i < polygonNumber; i++) {
            points.Add(new Vector2f(UnityEngine.Random.Range(0, 512), UnityEngine.Random.Range(0, 512)));
        }

        return points;
    }

    private void InstantiateVoronoi(List<Edge> edges, Dictionary<Vector2f,Site> sites) {
        foreach (KeyValuePair<Vector2f, Site> kv in sites) {
            //tx.SetPixel((int)kv.Key.x, (int)kv.Key.y, Color.red);
            Instantiate(
                    indicubeRed,
                    new Vector3(kv.Key.x, 0, kv.Key.y),
                    Quaternion.AngleAxis(0, Vector3.up));
        }
        foreach (Edge edge in edges) {
            // if the edge doesn't have clippedEnds, if was not within the bounds, dont draw it
            if (edge.ClippedEnds == null) continue;

            InstantiateLine(edge.ClippedEnds[LR.LEFT], edge.ClippedEnds[LR.RIGHT]);
        }

    }

    // Bresenham line algorithm
    private void InstantiateLine(Vector2f p0, Vector2f p1) {
        float x0 = (float)p0.x;
        float y0 = (float)p0.y;
        float x1 = (float)p1.x;
        float y1 = (float)p1.y;

        /*int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;*/

        float x = (float)(x0 + x1) / 2;
        float y = (float)(y0 + y1) / 2;

        Vector2 vec = new Vector2(x1 - x0, y1 - y0);

        float angle = Mathf.Atan2(x1-x0,y1-y0) * Mathf.Rad2Deg + 90;

        GameObject line = Instantiate(
                    cube,
                    new Vector3(x,0,y),
                    Quaternion.AngleAxis(angle, Vector3.up));
        line.transform.localScale = new Vector3(vec.magnitude, 1, 1);

        /*while (true) {
            tx.SetPixel(x0 + offset, y0 + offset, c);

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx) {
                err += dx;
                y0 += sy;
            }
        }*/
    }

    private GameObject InstantiateTile(int x, int y, ArrayList list) {
        GameObject newTile = null;
        //TileData tdata = GetTileData(x, y);

        if (!instantiated[x, y]) {
            

        }

        return newTile;
    }





}