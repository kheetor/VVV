using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectProxy : MonoBehaviour {
    public int cost;
    public int state = 0;
    public int id;
    public ObjectProxyData objectProxyData;
    //public ObjectProxyTransform objectProxyTransform;
    public Blueprint constrainToBp;

    public void UpdateState() {
        state = (state + 1) % 10000;
    }

    public void SetColliders(bool state) {
        Collider[] cols = transform.GetComponents<Collider>();
        for (int i = 0; i < cols.Length; i++) {
            cols[i].enabled = state;
        }
    }
}

