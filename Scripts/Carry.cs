using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trajectory {
    public Vector3[] points = new Vector3[100];
    public float bounce = 0.5f;
    public float timeStep = 0.1f;
    public LayerMask lm;

    public int progress = 0;
    public Vector3 pos;
    public Vector3 velocity;

    public ObjectProxy otherHit;

    public int travelProgress = 0;

    public Trajectory(Vector3 startPos, Vector3 startVelocity, float bounce, LayerMask lm) {
        this.pos = startPos;
        this.velocity = startVelocity;
        this.bounce = bounce;
        this.lm = lm;
    }

    public void Freeze() {
        if (otherHit) {
            Conveyor conv = otherHit.GetComponent<Conveyor>();
            if (conv) {
                pos = conv.GetEntryPos(pos, Vector3.one);
            }
            Blueprint bp = otherHit.GetComponent<Blueprint>();
            if (bp) {
                pos = bp.ClampPos(pos);
            }
        }
        while (progress < 100) {
            points[progress] = pos;
            progress++;
        }
    }

    public void CheckDropToGround() {
        RaycastHit hit;
        Vector3 point = pos;
        if (!Physics.Raycast(pos, Vector3.down, 0.55f) && Physics.Raycast(pos, Vector3.down, out hit, 100f, lm)) {
            point = hit.point + 0.5f * hit.normal;
        }
        while (progress < 100) {
            points[progress] = point;
            progress++;
        }
    }

    public void SimStep() {
        if (progress == 99 || Physics.Raycast(pos, Vector3.down, 0.45f)) {
            CheckDropToGround();
            return;
        }
        if (otherHit) {
            Freeze();
            return;
        }
        RaycastHit hit;
        if (Physics.SphereCast(pos, 0.5f, velocity, out hit, velocity.magnitude * timeStep, lm)) {
            //Debug.Log("hit " + hit.collider.gameObject.name);
            pos = hit.point + hit.normal * 0.5f;
            velocity = Vector3.Reflect(velocity, hit.normal) * bounce;
            ObjectProxy op = hit.collider.gameObject.GetComponentInParent<ObjectProxy>();
            if (op) {
                Blueprint bp = hit.collider.gameObject.GetComponentInParent<Blueprint>();
                if (bp) {
                    otherHit = op;
                }
                Conveyor conv = hit.collider.gameObject.GetComponent<Conveyor>();
                if (conv) {
                    otherHit = op;
                }
            }
        }
        else {
            pos += velocity * timeStep;
        }
        points[progress] = pos;
        progress++;
        velocity += timeStep * 9.81f * Vector3.down;
    }

    public IEnumerator Travel(GameObject o, float speed) {
        o.transform.localScale = Vector3.one;

        for (int i = 0; i < Mathf.Min(progress-1, points.Length-1); i++) {
            float segDist = (points[i] - points[i + 1]).magnitude;
            float dist = 0;
            while (dist < segDist) {
                o.transform.position = Vector3.Lerp(points[i], points[i + 1], dist / segDist);
                dist += Time.deltaTime * speed;
                yield return null;
            }
        }

        o.transform.position = points[Mathf.Min(progress,points.Length) - 1];
        travelProgress = 100;
    }
}

public class Carry : MonoBehaviour {

    public Client client;

    

    void Start() {
    }

    void Update() {
        
    }
}
