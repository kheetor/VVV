using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FitType {
    stretch,
    tile,
    array
}


public enum SymmetryType {
    Cycle,
    Mirror,
}

public class ModularMesh2D : MonoBehaviour {

    public Vector2 size = Vector2.one;

    public GameObject[] edges;
    public GameObject[] corners;
    public GameObject face;

    public float cornerSize = 0.1f;
    public float edgeWidth = 0.1f;
    public float dynScale = 0.1f;

    public bool snapLength = true;

    public GameObject[] edgesC = new GameObject[4];
    public GameObject[] cornersC = new GameObject[4];
    public GameObject faceC;

    public SymmetryType symmetry;

    private ModularMesh mm;

    [ContextMenu("Build")]
    [ExecuteInEditMode]
    public void Build() {
        if (!mm) {
            mm = transform.parent.GetComponent<ModularMesh>();
        }
        if (dynScale > 0) {
            float s = Mathf.Min(mm.size.x, mm.size.y, mm.size.z);
            s *= dynScale;
            cornerSize = s;
            edgeWidth = s;
        }

        if (edges.Length > 0) {
            BuildEdges();
        }
        if (corners.Length > 0) {
            BuildCorners();
        }
        BuildFace();
    }

    public void BuildFace() {
        if (!faceC) {
            faceC = Instantiate(face, transform);
        }
        faceC.transform.localPosition = new Vector3(0f, -0.5f * size.y + edgeWidth, 0f);
        faceC.transform.localEulerAngles = Vector3.zero;
        faceC.transform.localScale = new Vector3(size.x - 2f * edgeWidth, size.y - 2f * edgeWidth, edgeWidth);
    }

    public void BuildCorners() {
        Vector3[] cps = new Vector3[] {
            new Vector3(0.5f*size.x, 0.5f*size.y),
            new Vector3(-0.5f*size.x, 0.5f*size.y),
            new Vector3(-0.5f*size.x, -0.5f*size.y),
            new Vector3(0.5f*size.x, -0.5f*size.y)
        };
        for (int i = 0; i < 4; i++) {
            if (!corners[i % corners.Length]) {
                continue;
            }
            if (!cornersC[i]) {
                cornersC[i] = Instantiate(corners[i % corners.Length], transform);
            }
            cornersC[i].transform.localPosition = cps[i];
            cornersC[i].transform.localEulerAngles = new Vector3(0f, 0f, 90f * i);
            cornersC[i].transform.localScale = Vector3.one * cornerSize;
        }
    }

    public void BuildEdges() {
        Vector3[] cps = new Vector3[4];

        if (symmetry == SymmetryType.Cycle) {
            cps[0] = new Vector3(0.5f * size.x - cornerSize, 0.5f * size.y);
            cps[1] = new Vector3(-0.5f * size.x, 0.5f * size.y - cornerSize);
            cps[2] = new Vector3(-0.5f * size.x + cornerSize, -0.5f * size.y);
            cps[3] = new Vector3(0.5f * size.x, -0.5f * size.y + cornerSize);
        }
        else if (symmetry == SymmetryType.Mirror) {
            cps[0] = new Vector3(0.5f * size.x - cornerSize, 0.5f * size.y);
            cps[1] = new Vector3(-0.5f * size.x, 0.5f * size.y - cornerSize);
            cps[2] = new Vector3(0.5f * size.x - cornerSize, -0.5f * size.y);
            cps[3] = new Vector3(0.5f * size.x, 0.5f * size.y - cornerSize);
        }

        for (int i = 0; i < 4; i++) {
            if (!edges[i % edges.Length]) {
                continue;
            }
            if (!edgesC[i]) {
                edgesC[i] = Instantiate(edges[i % edges.Length], transform);
            }
            edgesC[i].transform.localPosition = cps[i];

            if (symmetry == SymmetryType.Cycle) {
                edgesC[i].transform.localEulerAngles = new Vector3(0f, 0f, 90f * i);
                if (i % 2 == 1) {
                    edgesC[i].transform.localScale = new Vector3(size.y - 2f * cornerSize, edgeWidth, edgeWidth);
                }
                else {
                    edgesC[i].transform.localScale = new Vector3(size.x - 2f * cornerSize, edgeWidth, edgeWidth);
                }
            }
            else if (symmetry == SymmetryType.Mirror) {
                edgesC[i].transform.localEulerAngles = new Vector3(0f, 0f, 90f * (i%2));

                if(i==0)
                    edgesC[i].transform.localScale = new Vector3(size.x - 2f * cornerSize, edgeWidth, edgeWidth);
                if (i == 1)
                    edgesC[i].transform.localScale = new Vector3(size.y - 2f * cornerSize, edgeWidth, edgeWidth);
                if (i == 2)
                    edgesC[i].transform.localScale = new Vector3(size.x - 2f * cornerSize, -edgeWidth, edgeWidth);
                if (i == 3) 
                    edgesC[i].transform.localScale = new Vector3(size.y - 2f * cornerSize, -edgeWidth, edgeWidth);
            }
        }
    }

    void Start() {
        mm = transform.parent.GetComponent<ModularMesh>();
    }

    void Update() {

    }
}
