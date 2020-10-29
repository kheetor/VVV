using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Spawner {
    public ObjectType objectType;
    public int maxNum = 64;
    public int spawnInterval = 20;
    public int nextSpawn = 20;
    public List<ObjectProxy> resources = new List<ObjectProxy>();

    public void Tick(ResourceSpawn rs) {
        if(nextSpawn == 0) {
            Spawn(rs);
            nextSpawn = spawnInterval;
        }
        else {
            nextSpawn--;
        }
    }

    public void Spawn(ResourceSpawn rs) {
        if (resources.Count < maxNum) {
            Vector3 pos = rs.GetSpawnLocation();
            ObjectProxy obj = rs.ov.CreateProxy(objectType, pos, Color.white, -1);
            obj.objectProxyData = new ObjectProxyData(obj.id, -1, -1, objectType, new SrTransform(Data.SrVector(pos, 1000f)), new int[] { 0, 100, 0 });
            resources.Add(obj);
        }
    }
}
public class ResourceSpawn : MonoBehaviour {

    public float inRange = 100f;
    public float outRange = 1000f;
    public float initialFill = 0.5f;

    public ObjectView ov;

    public Dictionary<ObjectType, float> dist = new Dictionary<ObjectType, float>();

    public List<Spawner> spawners = new List<Spawner>();

    int nextSpawn = 0;
    float frac = 1f;
    float sinceLastTick = 0f;

    bool initDone = false;

    void Start() {
        if (!initDone) {
            SpawnInitial();
            frac = 1f / spawners.Count;
        }
    }

    void SpawnInitial() {
        for(int i = 0; i < spawners.Count; i++) {
            // add objects loaded from server save
            for (int e = 0; e < ov.maxIndex; e++) {
                if (ov.obs[e] && ov.obs[e].objectProxyData.pt == spawners[i].objectType) {
                    spawners[i].resources.Add(ov.obs[e]);
                }
            }
            // add rest
            for (int j = spawners[i].resources.Count; j < spawners[i].maxNum*Mathf.Min(initialFill,1f); j++) {
                spawners[i].Spawn(this);
            }
        }
    }

    void SpawnTick() {
        spawners[nextSpawn].Tick(this);
        nextSpawn = (nextSpawn +1) % spawners.Count;
    }

    public Vector3 GetSpawnLocation() {
        RaycastHit hit;
        int iter = 0;
        //
        while (iter < 100) {
            float x = (Random.Range(0, 2) == 1 ? 1 : -1) * Random.Range(0, outRange);
            float y = (Random.Range(0, 2) == 1 ? 1 : -1) * Random.Range((Mathf.Abs(x) < inRange ? inRange : 0), outRange);
            if (Physics.Raycast(new Vector3(x, 10f, y), Vector3.down, out hit, 10f)) {
                return hit.point;
            }
            iter++;
        }
        return Vector3.zero;
    }

    void FixedUpdate() {
        if(sinceLastTick > frac) {
            sinceLastTick = 0;
            SpawnTick();
        }
        else {
            sinceLastTick += Time.deltaTime;
        }
    }
}
