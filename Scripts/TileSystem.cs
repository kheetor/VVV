using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileSystem : MonoBehaviour {

    public float visionRange = 1024f;
    public Transform player;
    public float updateInterval = 5f;
    public float lastUpdate = 0f;

    /*
    public int tileRange = 2;
    public GameObject[] tiles = new GameObject[64];
    

    float tileSize = 256;
    int numTiles = 16;
    Vector3 offsetCenter = new Vector3(1024f, 0f, 1024f);

    public int GetTile(Vector3 pos) {
        return Mathf.RoundToInt((pos.x+offsetCenter.x) / tileSize) + numTiles*Mathf.RoundToInt((pos.z - offsetCenter.z) / tileSize);
    }

    
    public int[] GetTiles(Vector3 pos, int range) {

    }
    */

    void Start() {
        UpdateTileVisibility();
    }

    void SetTile(Transform t, bool active) {
        t.gameObject.SetActive(active);
    }

    public void UpdateTileVisibility() {
        Vector3 projPos = new Vector3(player.transform.position.x, 0f, player.transform.position.z);
        for (int i = 0; i < transform.childCount; i++) {
            SetTile(transform.GetChild(i), (projPos - transform.GetChild(i).position).sqrMagnitude < visionRange * visionRange);
        }

        lastUpdate = 0f;
    }

    void FixedUpdate() {
        lastUpdate += Time.deltaTime;
        if (lastUpdate > updateInterval) {
            UpdateTileVisibility();
        }
    }
}
