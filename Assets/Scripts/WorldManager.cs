using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BorderState { Undefined, Closed, Opened };

public class WorldManager : MonoBehaviour {

    public GameObject player;
    private Transform playerTransform;
    public GameObject[] tilePref;

    private int curr = 0;
    //private GameObject[][] tiles = new GameObject[1000][1000]();
    
    public TileData[,] tiles;
    public bool[,] instantiated;
    private int gridDim = 40;
    private ArrayList tileList;

    // Start is called before the first frame update
    void Start() {
        playerTransform = player.GetComponent<Transform>();

        tiles = GeneratePaths(gridDim);
        instantiated = new bool[gridDim, gridDim];
        for (int i = 0; i < gridDim; i++) {
            for (int j = 0; j < gridDim; j++) {
                instantiated[i, j] = false;
            }
        }

        tileList = new ArrayList();
    }

    // Update is called once per frame
    void Update() {
        //convert world coords to index
        int currX = ((int)playerTransform.position.x/10) + gridDim/2;
        int currZ = ((int)playerTransform.position.z/10) + gridDim/2;

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
        }

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

        //remove from list existing list
        foreach (Tuple<GameObject, int, int> tup in toDelete) {
            tileList.Remove(tup);
        }
    }

    private GameObject InstantiateTile(int x, int y, ArrayList list) {
        GameObject newTile = null;
        if (!instantiated[x,y]) {
            int prefIdx = DetermineTileType(tiles[x, y].path);
            //Debug.Log(x + " " + y + " " + prefIdx);
            newTile = Instantiate(
                tilePref[prefIdx],
                new Vector3((x-gridDim/2) * 10, 0, (y - gridDim / 2) * 10),
                Quaternion.identity);
            instantiated[x, y] = true;

            list.Add(new Tuple<GameObject, int, int>(newTile, x, y));
        }

        return newTile;
    }

    private int DetermineTileType(BorderState[] border) {

        int type = 0;
        if (border[0] == BorderState.Opened) {
            if (border[1] == BorderState.Opened) {
                if (border[2] == BorderState.Opened) {
                    if (border[3] == BorderState.Opened) {
                        type = 10;
                    }
                    else {
                        type = 6;
                    }
                }
                else {
                    if (border[3] == BorderState.Opened) {
                        type = 7;
                    }
                    else {
                        type = 2;
                    }
                }
            }
            else {
                if (border[2] == BorderState.Opened) {
                    if (border[3] == BorderState.Opened) {
                        type = 8;
                    }
                    else {
                        type = 1;
                    }
                }
                else {
                    if (border[3] == BorderState.Opened) {
                        type = 3;
                    }
                    else {
                        type = -1;
                    }
                }
            }
        }
        else { //0
            if (border[1] == BorderState.Opened) {
                if (border[2] == BorderState.Opened) {
                    if (border[3] == BorderState.Opened) {
                        type = 9;
                    }
                    else { //3
                        type = 5;
                    }
                }
                else { //2
                    if (border[3] == BorderState.Opened) {
                        type = 0;
                    }
                    else {
                        type = -1;
                    }
                }
            }
            else { // 1
                if (border[2] == BorderState.Opened) {
                    if (border[3] == BorderState.Opened) {
                        type = 4;
                    }
                    else { //3
                        type = -1;
                    }
                }
                else { //2
                    if (border[3] == BorderState.Opened) {
                        type = -1;
                    }
                    else {
                        type = -1;
                    }
                }
            }
        }

        if (type == -1) type = 10;
        return type;
    }

    private TileData[,] GeneratePaths(int dim) {
        TileData[,] myTiles = new TileData[dim,dim];
        System.Random rand = new System.Random(0);

        Queue<Tuple<int,int>> toExplore = new Queue<Tuple<int, int>>();
        toExplore.Enqueue(new Tuple<int,int>(dim/2, dim/2));

        for (int i = 0; i < dim; i++) {
            for (int j = 0; j < dim; j++) {
                myTiles[i, j] = new TileData();
            }
        }

        while (toExplore.Count > 0) {
            Tuple<int, int> curr = toExplore.Dequeue();
            int x = curr.Item1;
            int y = curr.Item2;

            Debug.Log("Coord: " + x + " " + y);
            //if (toExplore.Count > 0) Debug.Log("queue: " + toExplore.Peek());

            BorderState[] bs = DetermineTileBorders(
                rand.Next(0, 100),
                new BorderState[] {y > 0 ? myTiles[x, y-1].path[0] : BorderState.Closed,
                x < dim-1 ? myTiles[x+1, y].path[1] : BorderState.Closed,
                y < dim-1 ? myTiles[x, y+1].path[2] : BorderState.Closed,
                x > 0 ? myTiles[x-1, y].path[3] : BorderState.Closed }
                );

            myTiles[x, y].SetPath(bs);
            myTiles[x, y].established = true;

            //add to queue
            if (x < dim-1 && !myTiles[x+1,y].established) {
                toExplore.Enqueue(new Tuple<int, int>(x+1, y));
            }
            else if (x > 0 && !myTiles[x-1, y].established) {
                toExplore.Enqueue(new Tuple<int, int>(x-1, y));
            }
            else if (y < dim-1 && !myTiles[x, y+1].established) {
                toExplore.Enqueue(new Tuple<int, int>(x, y+1));
            }
            else if (y > 0 && !myTiles[x, y-1].established) {
                toExplore.Enqueue(new Tuple<int, int>(x, y-1));
            }

        }

        /*for (int i = 0; i < 10; i++) {
            Debug.Log("rand #" + i + " " + rand.Next(0, 100));
        }*/

        return myTiles;
    }

    private BorderState[] DetermineTileBorders(int num, 
        BorderState[] border) {

        //BorderState[] res = new BorderState[4];

        int undefCount = 0;
        for (int i = 0; i < 3; i++) {
            if (border[i] == BorderState.Undefined) undefCount++;
        }

        int[] order = new int[4] { 3,0,1,2 };
        for (int i = 0; i < 3; i++) {
            int curr = order[i];
            if (border[curr] == BorderState.Undefined) {
                switch (undefCount) {
                    case 1:
                        if (num < 10)
                            border[curr] = BorderState.Opened;
                        else
                            border[curr] = BorderState.Closed;
                        break;
                    case 2:
                        if (num < 20)
                            border[curr] = BorderState.Opened;
                        else
                            border[curr] = BorderState.Closed;
                        break;
                    case 3:
                        if (num < 35)
                            border[curr] = BorderState.Opened;
                        else
                            border[curr] = BorderState.Closed;
                        break;
                }
            }
        }

        //50% straight
        //20% building
        //10% cross
        //10% T junc
        //10% corner

        return border;
    }
}

public class TileData {
    public BorderState[] path;
    public bool established;

    public TileData(
        BorderState n = BorderState.Undefined, 
        BorderState w = BorderState.Undefined, 
        BorderState s = BorderState.Undefined, 
        BorderState e = BorderState.Undefined) {

        path = new BorderState[4] { n,w,s,e };
        established = false;
    }

    public void SetPath(
        BorderState n = BorderState.Undefined,
        BorderState w = BorderState.Undefined,
        BorderState s = BorderState.Undefined,
        BorderState e = BorderState.Undefined) {

        path = new BorderState[4] { n, w, s, e };
    }

    public void SetPath(BorderState[] border) {

        path = border;
    }
}
