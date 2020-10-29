using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Conveyor : MonoBehaviour {

    public float speed = 1f;

    public List<ObjectProxy> obs = new List<ObjectProxy>();

    public Conveyor next;

    public float updateInterval = 2f;
    public float updateCounter = 0f;

    public LayerMask lm;

    public MeshRenderer mf;

    public ObjectView ov;

    void Start() {

    }

    public Vector3 GetEntryPos(Vector3 nearPos, Vector3 size) {
        Vector3 delta = transform.InverseTransformPoint(nearPos);
        return transform.TransformPoint(new Vector3(0f, 0.5f, delta.z)) + transform.up * size.y * 0.5f;
    }

    public Vector3 GetExitPos(Vector3 size) {
        return transform.TransformPoint(new Vector3(0f, 0.5f, 0.5f)) + transform.up * size.y * 0.5f;
    }

    public bool Attach(ObjectProxy go, bool check = true) {
        Vector3 delta = transform.InverseTransformPoint(go.transform.position);
        float size = 3f * (go.transform.localScale.x + go.transform.localScale.y);
        if (!check || Mathf.Abs(delta.x) < size &&
            Mathf.Abs(delta.y) < size &&
            Mathf.Abs(delta.z) < 0.5f+transform.localScale.z) {
            obs.Add(go);
            Collider[] cols = go.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++) {
                cols[i].enabled = false;
            }
            go.transform.position = transform.TransformPoint(new Vector3(0f, 0.5f, delta.z)) + transform.up * go.transform.localScale.y * 0.5f;
            go.transform.rotation = transform.rotation;
            return true;
        }
        return false;
    }

    public IEnumerator Detach(ObjectProxy go) {
        Debug.Log("detached " + go.id);

        obs.Remove(go);
        if (next) {
            next.Attach(go);
        }

        Trajectory tra = new Trajectory(GetExitPos(Vector3.one), transform.forward * 5f, 0.1f, lm);
        for(int i = 0; i < 30; i++) {
            tra.SimStep();
        }

        StartCoroutine(tra.Travel(go.gameObject, 5f));

        while (tra.travelProgress != 100) {
            yield return null;
        }

        if (tra.otherHit && tra.otherHit.gameObject != gameObject && ov) {
            ov.DepositObject(go, tra.otherHit);
        }
        else {
            Collider[] cols = go.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++) {
                cols[i].enabled = true;
            }
            go.objectProxyData.constrainedBy = -1;
        }

        go.objectProxyData.t = new SrTransform(go.transform);
        go.objectProxyData.constrainedBy = -1;
        go.UpdateState();
    }

    void Update() {
        bool update = false;
        if(updateCounter > updateInterval) {
            update = true;
            updateCounter = 0f;
        }
        updateCounter += Time.deltaTime;
        for (int i = 0; i < obs.Count; i++) {
            if (obs != null && obs[i] != null) {
                if (transform.InverseTransformPoint(obs[i].transform.position).z > 0.50f) {
                    StartCoroutine(Detach(obs[i]));
                    i--;
                    continue;
                }
            }
            obs[i].transform.position += speed * Time.deltaTime * transform.forward;
            if (update) {
                obs[i].UpdateState();
                obs[i].objectProxyData.t = new SrTransform(obs[i].transform);
            }
        }
        if (mf) {
            mf.material.mainTextureOffset = new Vector2(1f, speed-((Time.time * speed) % speed));
            mf.material.mainTextureScale = new Vector2(1f, transform.localScale.z);
        }
    }
}
