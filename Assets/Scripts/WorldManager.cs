using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BorderState { Undefined, Closed, Opened };

public class WorldManager : MonoBehaviour {

    

    public GameObject player;
    private Transform playerTransform;
    public GameObject tile;

    private int curr = 0;
    //private GameObject[][] tiles = new GameObject[1000][1000]();
    
    public TileData[,] tiles;

    // Start is called before the first frame update
    void Start() {
        playerTransform = player.GetComponent<Transform>();

        //tiles = new TileData[20][20];
        tiles = GeneratePaths(20);
    }

    // Update is called once per frame
    void Update() {
        if (playerTransform.position.x > 10*curr) {
            Instantiate(tile, new Vector3(curr * 10 + 5, 0, 0), Quaternion.identity);
            curr++;
        }
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

            BorderState[] bs = DetermineTile(
                rand.Next(0, 100),
                myTiles[x, y-1].path[0],
                myTiles[x+1, y].path[0],
                myTiles[x, y+1].path[0],
                myTiles[x-1, y].path[0]);


            myTiles[x, y].SetPath(bs[0],bs[1],bs[2],bs[3]);
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

        for (int i = 0; i < 10; i++) {
            Debug.Log("rand #" + i + " " + rand.Next(0, 100));
        }

        return tiles;
    }

    private BorderState[] DetermineTile(int num, 
        BorderState n = BorderState.Undefined, 
        BorderState w = BorderState.Undefined,
        BorderState s = BorderState.Undefined,
        BorderState e = BorderState.Undefined) {

        BorderState[] res = new BorderState[4];

        //50% straight
        //20% building
        //10% cross
        //10% T junc
        //10% corner

        if (num < 20) { //20% building

        }
        else if (num < 70) { //50% straight

        }
        else if (num < 80) { //10% cross

        }
        else if (num < 90) { //10% T junc

        }
        else { //10% corner

        }


        return res;
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
}
