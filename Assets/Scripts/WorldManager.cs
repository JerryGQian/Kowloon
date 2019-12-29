using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using csDelaunay;

//public enum BorderState { Undefined, Closed, Opened };

public class WorldManager : MonoBehaviour {

    public GameObject player;
    private Transform playerTransform;

    public GameObject cube;
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

    //private List<Tuple<Tuple<int,int>,List<GameObject>>> zoneObjects;
    private LinkedList<GameObject>[,] zoneObjects; //to remove...
    private LinkedList<GameObject>[,] zoneStreetObjects;
    private LinkedList<GameObject>[,] zoneWallObjects;

    private HashSet<Vector2> zonesInstantiated;
    private bool[,] isInstantiated;

    // Object pool ////////////////////
    private Vector3 poolLocation;

    public int numStreets = 200;
    private Queue<GameObject> streetQueue;
    public GameObject streetObj;

    public int numWalls = 6000;
    private Queue<GameObject> wallQueue;
    public GameObject wallObj;

    // Start is called before the first frame update
    void Start() {
        playerTransform = player.GetComponent<Transform>();
        pointRand = new CoordRandom(seed);
        halfStorageDim = (int)(storageDim / 2);

        zoneObjects = new LinkedList<GameObject>[storageDim, storageDim]; //to remove...
        zoneStreetObjects = new LinkedList<GameObject>[storageDim, storageDim];
        zoneWallObjects = new LinkedList<GameObject>[storageDim, storageDim];
        isInstantiated = new bool[storageDim, storageDim];
        for (int i = 0; i < storageDim; i++) {
            for (int j = 0; j < storageDim; j++) {
                zoneObjects[i, j] = new LinkedList<GameObject>(); //to remove...
                zoneStreetObjects[i, j] = new LinkedList<GameObject>();
                zoneWallObjects[i, j] = new LinkedList<GameObject>();

                isInstantiated[i, j] = false;
            }
        }

        zonesInstantiated = new HashSet<Vector2>();

        // Object Pool Initialization
        poolLocation = new Vector3(0, -10, 0);
        // initialize street object pool
        streetQueue = new Queue<GameObject>();
        for (int i = 0; i < numStreets; i++) {
            streetQueue.Enqueue(
                Instantiate(streetObj, poolLocation, Quaternion.identity));
        }
        // initialize wall object pool
        wallQueue = new Queue<GameObject>();
        for (int i = 0; i < numWalls; i++) {
            wallQueue.Enqueue(
                Instantiate(wallObj, poolLocation, Quaternion.identity));
        }

        /*
        //testing perlin
        for (int i = 0; i < 10; i++) {
            Debug.Log(i + " " + Mathf.PerlinNoise(0.1f, ((float)i / 10)));
        }
        */

        //BuildWall(new Vector2f(0,0), new Vector2f(16,16), 0, 0);
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


        // INSTANTIATION /////////////////////

        //check if in a zone that needs new Voronoi
        bool needsNewVoronoi = false;
        for (int i = -1; i <= 1; i++) {
            for (int j = -1; j <= 1; j++) {
                if (!isInstantiated[currX + i + halfStorageDim, currY + j + halfStorageDim])
                    needsNewVoronoi = true;
            }
        }

        needsNewVoronoi = !isInstantiated[currX + halfStorageDim, currY + halfStorageDim];

        if (needsNewVoronoi) {
            Voronoi voronoi = GenerateVoronoi(currX, currY);

            InstantiateVoronoi(voronoi, currX, currY);
            isInstantiated[currX + halfStorageDim, currY + halfStorageDim] = true;
        }

        // DELETION //////////////////////////

        Vector2 curr = new Vector2(currX, currY);
        List<Vector2> toRemove = new List<Vector2>();
        //search for existing zones that are too far
        foreach (Vector2 zone in zonesInstantiated) {
            Vector2 diff = zone - curr;
            if (diff.x < -1 || diff.x > 1 ||
                diff.y < -1 || diff.y > 1) {
                RecycleZone((int)zone.x, (int)zone.y);
                toRemove.Add(zone);
            }
        }
        //avoid concurrent modification
        foreach (Vector2 zone in toRemove) {
            zonesInstantiated.Remove(zone);
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////
    /// Custom functions...
    //////////////////////////////////////////////////////////////////////////////////////////

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

    private void CreatePoints(int zoneX, int zoneY, List<Vector2f> points) {
        int[] arr = pointRand.GetInts(zoneX, zoneY, 1, zoneDim - 1, polygonNumber * 2);

        for (int i = 0; i < arr.Length; i += 2) {
            points.Add(new Vector2f(
                (float)((zoneX * zoneDim) + arr[i]),
                (float)((zoneY * zoneDim) + arr[i + 1])));
        }
    }

    private void InstantiateVoronoi(Voronoi voronoi, int zoneX, int zoneY) {
        Dictionary<Vector2f, Site> sites = voronoi.SitesIndexedByLocation;
        List<Edge> edges = voronoi.Edges;

        Vector2f lowerBound = new Vector2f((zoneX-1)*zoneDim, (zoneY-1)*zoneDim);
        Vector2f upperBound = new Vector2f((zoneX + 2) * zoneDim, (zoneY + 2) * zoneDim);

        RegisterInstantiatedZones(zoneX, zoneY);

        foreach (KeyValuePair<Vector2f, Site> kv in sites) {
            //if (!SiteInBound(kv.Value, lowerBound, upperBound)) continue;
            Instantiate(
                    indicubeRed,
                    new Vector3(kv.Key.x, 0, kv.Key.y),
                    Quaternion.AngleAxis(0, Vector3.up));

            //Debug.Log("Edges of" + kv.Key + " = " + kv.Value.Edges);
            string s = "" + kv.Value.Edges.Count;
            int len = kv.Value.Edges.Count;
            int i = 0;

            //Dictionary<Vector2f, Tuple<Vector2f, Vector2f>> map = new Dictionary<Vector2f, Tuple<Vector2f, Vector2f>>();
            
            //This implementation is the ugliest code I have ever written, I apologize, dear reader...
            List<Tuple<Vector2f, Vector2f, Vector2f>> cornerRelationList = new List<Tuple<Vector2f, Vector2f, Vector2f>>();
            List<Tuple<Vector2f, Vector2f>> cornerPositionList = new List<Tuple<Vector2f, Vector2f>>();
            //Dictionary<Vector2f, Vector2f> cornerMap = new Dictionary<Vector2f, Vector2f>();

            foreach (Edge edge in kv.Value.Edges) {
                s += " | " + i + "->";
                i++;
                
                if (edge.ClippedEnds == null) continue;
                //if (!EdgeInBound(edge, lowerBound, upperBound)) continue;

                Vector2f leftV = edge.ClippedEnds[LR.LEFT];
                Vector2f rightV = edge.ClippedEnds[LR.RIGHT];
                s += edge.ClippedEnds[LR.LEFT];
                s += edge.ClippedEnds[LR.RIGHT];
                //Debug.Log(kv.Key + " this edge: " + leftV + " " + map.ContainsKey(leftV) + " " + rightV + " " + map.ContainsKey(rightV));

                //Update list
                bool leftFound = false;
                bool rightFound = false;
                Tuple<Vector2f, Vector2f, Vector2f> toRemoveLeft = null;
                Tuple<Vector2f, Vector2f, Vector2f> toRemoveRight = null;
                foreach (Tuple<Vector2f, Vector2f, Vector2f> tup in cornerRelationList) {
                    Vector2f diffLeft = tup.Item1 - leftV;
                    Vector2f diffRight = tup.Item1 - rightV;
                    if (diffLeft.magnitude < 0.05f) {
                        toRemoveLeft = tup;
                        //tup = new Tuple<Vector2f, Vector2f, Vector2f>(leftV, tup.Item2, rightV);
                        leftFound = true;
                    }
                    if (diffRight.magnitude < 0.05f) {
                        toRemoveRight = tup;
                        //tup = new Tuple<Vector2f, Vector2f, Vector2f>(rightV, tup.Item2, leftV);
                        rightFound = true;
                    }
                }
                if (leftFound) {
                    cornerRelationList.Remove(toRemoveLeft);
                    cornerRelationList.Add(new Tuple<Vector2f, Vector2f, Vector2f>(leftV, toRemoveLeft.Item2, rightV));
                }
                else {
                    cornerRelationList.Add(new Tuple<Vector2f, Vector2f, Vector2f>(leftV, rightV, rightV));
                }

                if (rightFound) {
                    cornerRelationList.Remove(toRemoveRight);
                    cornerRelationList.Add(new Tuple<Vector2f, Vector2f, Vector2f>(rightV, toRemoveRight.Item2, leftV));
                }
                else {
                    cornerRelationList.Add(new Tuple<Vector2f, Vector2f, Vector2f>(rightV, leftV, leftV));
                }

                //
                /*if (map.ContainsKey(leftV))
                    map[leftV] = new Tuple<Vector2f, Vector2f>(map[leftV].Item1, rightV);
                else
                    map[leftV] = new Tuple<Vector2f, Vector2f>(rightV, rightV);

                if (map.ContainsKey(rightV))
                    map[rightV] = new Tuple<Vector2f, Vector2f>(map[rightV].Item1, leftV);
                else
                    map[rightV] = new Tuple<Vector2f, Vector2f>(leftV, leftV);*/
            }
            Debug.Log(kv.Key + "-> edges::: " + s);

            //Calculate corner positions for wall ends
            //foreach (KeyValuePair<Vector2f, Tuple<Vector2f,Vector2f>> kv2 in map) {
            foreach (Tuple<Vector2f, Vector2f, Vector2f> relation in cornerRelationList) {
                Vector2f orig = relation.Item1;
                Vector2f dest1 = relation.Item2;
                Vector2f dest2 = relation.Item3;

                Vector2f diff1 = dest1 - orig;
                Vector2f diff2 = dest2 - orig;

                float slope1 = diff1.y / diff1.x;
                float slope2 = diff2.y / diff2.x + 0.000001f;

                float perp1 = -1f / slope1;
                float perp2 = -1f / slope2;

                Debug.Log("CORNER:" + kv.Key + " " + orig);
                Debug.Log(kv.Key  + "slopes: " + slope1 + " " + slope2 + " perps: " + perp1 + " " + perp2);
                Debug.Log("diffs=" + diff1 + " " + diff2);

                //Vector2 perpVec1 = new Vector2(1, perp1);
                //Vector2 perpVec2 = new Vector2(1, perp2);
                Vector2 perpVec1 = new Vector2(-diff1.y, diff1.x);
                Vector2 perpVec2 = new Vector2(-diff2.y, diff2.x);

                //Debug.Log(perpVec1 + " " + perpVec12);

                Debug.Log("old perpVec1: " + perpVec1 + " " + perpVec2);
                perpVec1 = perpVec1 * ((streetWidth / 2) / perpVec1.magnitude);
                perpVec2 = perpVec2 * ((streetWidth / 2) / perpVec2.magnitude);
                Debug.Log("new perpVec1: " + (perpVec1.x) + "," + (perpVec1.y) + " " + perpVec1.magnitude + " perpVec2=" + (perpVec2.x) + "," + (perpVec2.y));

                float yint1 = perpVec1.y + (Mathf.Pow(perpVec1.x, 2) / perpVec1.y);
                float yint2 = perpVec2.y + (Mathf.Pow(perpVec2.x, 2) / perpVec2.y);

                if (false) {
                    yint1 *= -1;
                    yint2 *= -1;
                }

                //Debug.Log("yint1and2=" + yint1 + " " + yint2 + " " + Mathf.Abs(perpVec1.y) + " " + (Mathf.Pow(perpVec1.x, 2) / Mathf.Abs(perpVec1.y)));


                float x = -1f * (yint1 - yint2) / (slope1 - slope2);
                float y = slope1 * x + yint1;

                //Debug.Log(perp1 + " " + perp2);
                //Debug.Log("calcing - orig=" + orig + " - " + x + " " + y + " d1:" + dest1 + " d2:" + dest2);

                Debug.Log("x:"+ x + " y:" + y);

                // if... swap direction
                /*if (diff1.x < 0 && diff2.x < 0) {
                    x *= -1;
                    y *= -1;
                }*/

                //cornerMap[orig] = new Vector2f(orig.x - x, orig.y - y);
                cornerPositionList.Add(
                    new Tuple<Vector2f, Vector2f>(
                        orig, 
                        new Vector2f(orig.x + x, orig.y + y)));
            }

            int c = 0;
            //BuildWall(new Vector2f(90,69), new Vector2f(94, 65), zoneX, zoneY);
            foreach (Edge edge in kv.Value.Edges) {
                if (edge.ClippedEnds == null) continue;

                Vector2f v1 = edge.ClippedEnds[LR.LEFT];
                Vector2f v2 = edge.ClippedEnds[LR.RIGHT];

                if (!EndsInBound(v1, v2, lowerBound, upperBound)) continue;

                Vector2f end1 = new Vector2f(0,0);
                Vector2f end2 = new Vector2f(0, 0);

                foreach (Tuple<Vector2f, Vector2f> tup in cornerPositionList) {
                    Vector2f diffLeft = tup.Item1 - v1;
                    Vector2f diffRight = tup.Item1 - v2;
                    if (diffLeft.magnitude < 0.05f)
                        end1 = tup.Item2;
                    if (diffRight.magnitude < 0.05f)
                        end2 = tup.Item2;
                }

                Debug.Log("v1=" + v1 + " v2=" + v2 + " -> " + end1 + " " + end2);
                BuildWall(end1, end2, zoneX, zoneY);
                //BuildWall(cornerMap[v1], cornerMap[v2], zoneX, zoneY);
                c++;
            }
            Debug.Log("c ==== " + c + " at " + kv.Key.x + "," + kv.Key.y);
        }

        foreach (Edge edge in edges) {
            // if the edge doesn't have clippedEnds, if was not within the bounds, dont draw it
            if (edge.ClippedEnds == null) continue;
            //if (!EdgeInBound(edge, lowerBound, upperBound)) continue;

            //Debug.Log(edge.ClippedEnds[LR.LEFT] + " " +  edge.ClippedEnds[LR.RIGHT]);

            ApplyStreet(edge.ClippedEnds[LR.LEFT], edge.ClippedEnds[LR.RIGHT], zoneX, zoneY);
            //InstantiateWall(edge.ClippedEnds[LR.LEFT], edge.ClippedEnds[LR.RIGHT], zoneX, zoneY);
        }


    }

    // Adds 3x3 zones around zoneX zoneY into zonesInstantiated hashset;
    private void RegisterInstantiatedZones(int zoneX, int zoneY) {
        for (int i = -1; i <= 1; i++) {
            for (int j = -1; j <= 1; j++) {
                Vector2 vec = new Vector2(zoneX + i, zoneY + j);
                if (!zonesInstantiated.Contains(vec))
                    zonesInstantiated.Add(vec);
            }
        }
    }

    /*private void InstantiateStreet(Vector2f p0, Vector2f p1, int zoneX, int zoneY) {
        float x0 = (float)p0.x;
        float y0 = (float)p0.y;
        float x1 = (float)p1.x;
        float y1 = (float)p1.y;

        float x = (float)(x0 + x1) / 2;
        float y = (float)(y0 + y1) / 2;

        Vector2 vec = new Vector2(x1 - x0, y1 - y0);

        float angle = Mathf.Atan2(x1-x0,y1-y0) * Mathf.Rad2Deg + 90;

        GameObject road = Instantiate(
                    cube,
                    new Vector3(x,0,y),
                    Quaternion.AngleAxis(angle, Vector3.up));
        road.transform.localScale = new Vector3(vec.magnitude, 1, streetWidth);

        Vector2 zone = DetermineZone(zoneX, zoneY, x, y);
        zoneStreetObjects[(int)zone.x + halfStorageDim, (int)zone.y + halfStorageDim].AddLast(road);
    }*/

    private void ApplyStreet(Vector2f p0, Vector2f p1, int zoneX, int zoneY) {
        float x0 = (float)p0.x;
        float y0 = (float)p0.y;
        float x1 = (float)p1.x;
        float y1 = (float)p1.y;

        float x = (float)(x0 + x1) / 2;
        float y = (float)(y0 + y1) / 2;

        Vector2 vec = new Vector2(x1 - x0, y1 - y0);

        float angle = Mathf.Atan2(x1 - x0, y1 - y0) * Mathf.Rad2Deg + 90;

        GameObject street = streetQueue.Dequeue();
        Transform trans = street.GetComponent<Transform>();

        trans.position = new Vector3(x, 0, y);
        trans.rotation = Quaternion.AngleAxis(angle, Vector3.up);
        trans.localScale = new Vector3(vec.magnitude, 1, streetWidth);

        Vector2 zone = DetermineZone(zoneX, zoneY, x, y);
        zoneStreetObjects[(int)zone.x + halfStorageDim, (int)zone.y + halfStorageDim].AddLast(street);
    }

    //Builds wall segment
    private void BuildWall(Vector2f p0, Vector2f p1, int zoneX, int zoneY) {
        //Debug.Log("p0=" + p0 + " p1=" + p1);
        float x0 = (float)p0.x;
        float y0 = (float)p0.y;
        float x1 = (float)p1.x;
        float y1 = (float)p1.y;

        float angle = Mathf.Atan2(x1 - x0, y1 - y0) * Mathf.Rad2Deg + 90;

        Vector2f diff = p1 - p0;
        float magnitude = diff.magnitude;

        int quant = (int)(magnitude / 2);
        float width = magnitude / quant;
        //Debug.Log("Width=" + width + " mag=" + magnitude);
        
        //base case for very short walls
        if (quant == 0) {
            quant = 1;
            width = magnitude;
        }

        for (int i = 0; i < quant; i++) {
            GameObject wallSeg = wallQueue.Dequeue();
            Transform trans = wallSeg.GetComponent<Transform>();

            trans.position = new Vector3(
                        p0.x + diff.x * ((i + 0.5f) * width / magnitude),
                        wallHeight / 2,
                        p0.y + diff.y * ((i + 0.5f) * width / magnitude));
            trans.rotation = Quaternion.AngleAxis(angle, Vector3.up);
            trans.localScale = new Vector3(width, wallHeight, 0.5f);
        }

        Debug.Log("segments made=" + quant);
    }

    private void InstantiateWall(Vector2f p0, Vector2f p1, int zoneX, int zoneY) {
        float x0 = (float)p0.x;
        float y0 = (float)p0.y;
        float x1 = (float)p1.x;
        float y1 = (float)p1.y;

        float angle = Mathf.Atan2(x1 - x0, y1 - y0) * Mathf.Rad2Deg + 90;

        Vector2f diff = p1 - p0;
        float magnitude = diff.magnitude;
        Vector3 perpAdj = new Vector3(-1*diff.y, 0, diff.x);
        perpAdj.Normalize();

        for (int i = 1; i < magnitude/2 -1; i++) {
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

    private void DestroyZone(int zoneX, int zoneY) {
        LinkedList<GameObject> streetList = zoneObjects[zoneX + halfStorageDim, zoneY + halfStorageDim];

        foreach (GameObject obj in streetList) {
            Destroy(obj);
        }

        streetList.Clear();
        isInstantiated[zoneX + halfStorageDim, zoneY + halfStorageDim] = false;
    }

    private void RecycleZone(int zoneX, int zoneY) {
        LinkedList<GameObject> streetList = zoneStreetObjects[zoneX + halfStorageDim, zoneY + halfStorageDim];

        foreach (GameObject obj in streetList) {
            //streets only:
            streetQueue.Enqueue(obj);
            //Destroy(obj);
        }

        streetList.Clear();
        isInstantiated[zoneX + halfStorageDim, zoneY + halfStorageDim] = false;
    }

    // HELPER FUNCTIONS ///////////////////////////////////////////////////////

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

    private bool SiteInBound(Site site, Vector2f lower, Vector2f upper) {
        Vector2f coord = site.Coord;

        return !(coord.x < lower.x || coord.x > upper.x ||
            coord.y < lower.y || coord.y > upper.y);
    }

    private bool EndsInBound(Vector2f v1, Vector2f v2, Vector2f lower, Vector2f upper) {
        float leftX = v1.x;
        float leftY = v1.y;
        float rightX = v2.x;
        float rightY = v2.y;

        return !(leftX < lower.x || leftX > upper.x ||
            leftY < lower.y || leftY > upper.y ||
            rightX < lower.x || rightX > upper.x ||
            rightY < lower.y || rightY > upper.y);
    }

    private Vector2 DetermineZone(int zoneX, int zoneY, float x, float y) {
        Vector2 res = new Vector2(zoneX, zoneY); //revisit this logic
        int diffX = (int)(x - (float)zoneX * zoneDim);
        int diffY = (int)(y - (float)zoneY * zoneDim);

        if (diffX < 0) {
            if (diffY < 0) res = new Vector2(zoneX - 1, zoneY - 1);
            else if (diffY < zoneDim) res = new Vector2(zoneX - 1, zoneY);
            else if (diffY < 2 * zoneDim) res = new Vector2(zoneX - 1, zoneY + 1);
        }
        else if (diffX < zoneDim) {
            if (diffY < 0) res = new Vector2(zoneX, zoneY - 1);
            else if (diffY < zoneDim) res = new Vector2(zoneX, zoneY);
            else if (diffY < 2 * zoneDim) res = new Vector2(zoneX, zoneY + 1);
        }
        else if (diffX < 2 * zoneDim) {
            if (diffY < 0) res = new Vector2(zoneX + 1, zoneY - 1);
            else if (diffY < zoneDim) res = new Vector2(zoneX + 1, zoneY);
            else if (diffY < 2 * zoneDim) res = new Vector2(zoneX + 1, zoneY + 1);
        }

        //if (res == null) res = new Vector2(zoneX, zoneY); //revisit this logic

        return res;
    }





}