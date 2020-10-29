using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModularMesh : MonoBehaviour {

    public ModularMesh2D top;
    public ModularMesh2D bottom;
    public ModularMesh2D[] walls;

    public Material mat;

    private bool instancedMat = false;
    public Vector3 size = Vector3.one;

    [ContextMenu("Build 3D")]
    [ExecuteInEditMode]
    public void Build() {
        BuildTop();
        BuildBottom();
        BuildWalls();
    }

    public void BuildTop() {
        top.transform.localPosition = new Vector3(0f, 0.5f * size.y, 0f);
        top.size = new Vector2(size.x, size.z);
        top.Build();
    }

    public void BuildBottom() {
        bottom.transform.localPosition = new Vector3(0f, -0.5f * size.y, 0f);
        bottom.size = new Vector2(size.x, size.z);
        bottom.Build();
    }

    public void BuildWalls() {
        for (int i = 0; i < 4; i++) {
            if (i % 2 == 0) {
                walls[i].transform.localPosition = new Vector3(0f, 0f, i == 0 ? 0.5f * size.z : -0.5f * size.z);
                walls[i].size = new Vector2(size.x, size.y);
            }
            else {
                walls[i].transform.localPosition = new Vector3(i == 1 ? 0.5f * size.x : -0.5f * size.x, 0f, 0f);
                walls[i].size = new Vector2(size.z, size.y);
            }
            walls[i].Build();
        }
    }

    void Start() {
        Build();
    }

    void FixedUpdate() {
        if (transform.parent && Mathf.Abs(transform.parent.localScale.x) > 0.1 &&
            Mathf.Abs(transform.parent.localScale.y) > 0.1 && Mathf.Abs(transform.parent.localScale.z) > 0.1) {
            if (Mathf.Abs(size.x - transform.parent.localScale.x) > 0.05f ||
                Mathf.Abs(size.y - transform.parent.localScale.y) > 0.05f ||
                Mathf.Abs(size.z - transform.parent.localScale.z) > 0.05f) {
                size = transform.parent.localScale;
                transform.localScale = new Vector3(
                    1f / transform.parent.localScale.x,
                    1f / transform.parent.localScale.y,
                    1f / transform.parent.localScale.z);
                Build();
            }

        }
    }
}
