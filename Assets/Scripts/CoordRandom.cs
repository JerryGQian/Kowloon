using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoordRandom {
    private int seed;
    private int[] prime = new int[] { 14173, 16249, 3761, 4157 };

    public CoordRandom(int seed) {
        this.seed = seed;
    }

    public int GetInt(int x, int y, int low, int high) {
        System.Random rand = new System.Random( (int)(prime[2] * x) + y + seed );

        return rand.Next(low, high + 1);
    }

    public int[] GetInts(int x, int y, int low, int high, int num=1) {
        System.Random rand = new System.Random((int)(prime[2] * x) + y + seed);
        int[] res = new int[num];

        for (int i = 0; i < num; i++) {
            res[i] = rand.Next(low, high + 1);
        }

        return res;
    }
}
