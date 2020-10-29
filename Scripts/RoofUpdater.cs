using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoofUpdater : MonoBehaviour {

    public int cachedState = -1;

    public Blueprint source;
    public int trackSlot = 1;
    public GameObject[] sectors = new GameObject[0];

    void Start() {

    }

    void Update() {
        if (cachedState != source.op.state) {
            //Debug.Log("UPDATE");
            float progress = (float)source.completed[trackSlot] / (float)source.requirements[trackSlot];
            int sects = sectors.Length;
            int completePieces = (int)Mathf.Floor(progress * sects);
            float lastProgress = progress * sects - completePieces;
            for (int i = 0; i < sects; i++) {
                if (i <= completePieces) {
                    sectors[i].transform.localScale = Vector3.one;
                }
                else if (i == completePieces + 1) {
                    sectors[i].transform.localScale = Vector3.one * lastProgress;
                }
                else {
                    sectors[i].transform.localScale = Vector3.zero;
                }
            }

            cachedState = source.op.state;
        }
    }
}
