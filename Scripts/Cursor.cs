using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cursor : MonoBehaviour {

    public GameObject selected;
    public ObjectProxy selOp;
    public Blueprint selBp;
    public ModularMesh selMm;

    public ClientUI ui;

    public GameObject[] cornerHandles = new GameObject[4];
    public GameObject moveHandle;
    public GameObject upRotHandle;
    public GameObject forwardRotHandle;

    public GameObject overlap;
    public GameObject editPlane;

    private float overlapAmount = 0.05f;
    private bool hide = true;

    public void RotateToEditPlane() {
        Transform t = selected.transform;

        //Find forward
        Vector3 delta = -Camera.main.transform.forward;
        Vector3 forward = t.forward;
        Vector3[] vecs = new Vector3[] {
            t.forward,
            t.right,
            t.up,
            -t.forward,
            -t.right,
            -t.up
        };
        int forwardAxis = -1;
        float minAngle = Mathf.Infinity;
        for (int i = 0; i < vecs.Length; i++) {
            float angle = Vector3.Angle(delta, vecs[i]);
            if (angle < minAngle) {
                forward = vecs[i];
                minAngle = angle;
                forwardAxis = i;
            }
        }

        // Find up
        delta = Camera.main.transform.up;
        Vector3 up = t.up;
        minAngle = Mathf.Infinity;
        for (int i = 0; i < vecs.Length; i++) {
            if (i == forwardAxis || i % 3 == forwardAxis % 3) {
                continue;
            }
            float angle = Vector3.Angle(delta, vecs[i]);
            if (angle < minAngle) {
                up = vecs[i];
                minAngle = angle;
            }
        }

        Debug.DrawLine(transform.position, transform.position + forward, Color.blue);
        Debug.DrawLine(transform.position, transform.position + up, Color.green);

        transform.LookAt(transform.position + forward, up);
    }

    public int HitHandle(RaycastHit hit) {
        return -1;
    }

    public IEnumerator MoveSelected() {
        Plane p = new Plane(transform.forward, transform.position);
        Vector3 startPointW;
        Vector3 startPos = selected.transform.position;
        if (CursorEditPlaneIntersect(p, out startPointW)) {
            while (Input.GetButton("Fire1")) {
                Vector3 editPoint;
                if (CursorEditPlaneIntersect(p, out editPoint)) {
                    Vector3 delta = editPoint - startPointW;
                    if (selOp.constrainToBp) {
                        selected.transform.position = Data.RoundVector(selOp.constrainToBp.ClampPos(startPos + delta), 0.1f);
                    }
                    else {
                        selected.transform.position = Data.RoundVector(startPos + delta, 0.1f);
                    }

                    ui.propertiesPanel.CopyFromTransform(selected.transform);
                }
                yield return null;
            }
        }
        SendUpdateSelectedTransform();
    }

    public void SendUpdateSelectedTransform() {
        if (!selected) {
            return;
        }
        ui.client.SendPlayerEditObjectTransform(selOp.id, new SrTransform(selected.transform));
    }

    public void SendUpdateSelectedColor() {
        if (!selected) {
            return;
        }
        ui.client.SendPlayerEditObjectColor(selOp.id, new SrTransform(selOp.objectProxyData.col));
    }

    public bool EditSelected(RaycastHit hit, int activeHandle) {

        if (!CanEditSelection()) {
            return false;
        }

        //Debug.Log(activeHandle);
        if (activeHandle != -1) {
            StartCoroutine(StretchSelected(activeHandle));
            return true;
        }
        if (hit.collider == null) {
            return false;
        }

        //Debug.Log(hit.collider.gameObject, hit.collider.gameObject);

        if (hit.collider.gameObject == moveHandle) {
            StartCoroutine(MoveSelected());
            return true;
        }
        for (int i = 0; i < cornerHandles.Length; i++) {
            if (hit.collider.gameObject == cornerHandles[i]) {
                StartCoroutine(MoveSelected());
                return true;
            }
        }

        if (hit.collider.gameObject == forwardRotHandle) {
            StartCoroutine(Rotate(transform.forward));
            return true;
        }
        if (hit.collider.gameObject == upRotHandle) {
            StartCoroutine(Rotate(transform.up));
            return true;
        }
        return false;
    }

    public IEnumerator Rotate(Vector3 axis) {
        Transform t = selected.transform;
        Plane p = new Plane(axis, transform.position+(overlap.transform.localScale.z-overlapAmount)*0.5f*overlap.transform.forward);
        Quaternion startRot = t.rotation;
        Vector3 startDir;
        if (CursorEditPlaneIntersect(p, out startDir)) {
            startDir = (startDir - transform.position).normalized;
            while (Input.GetButton("Fire1")) {
                Debug.DrawLine(transform.position + (overlap.transform.localScale.z - overlapAmount) * 0.5f * overlap.transform.forward, Vector3.zero, Color.cyan);
                Vector3 newDir;
                if (CursorEditPlaneIntersect(p, out newDir)) {
                    newDir = (newDir - transform.position).normalized;
                    t.rotation = startRot;
                    t.Rotate(axis, Vector3.SignedAngle(startDir, newDir, axis), Space.World);

                    ui.propertiesPanel.CopyFromTransform(t);
                }
                yield return null;
            }
            SendUpdateSelectedTransform();
        }
    }

    public IEnumerator StretchSelected(int handleIndex) {
        Transform t = selected.transform;
        Plane p = new Plane(transform.forward, transform.position + transform.forward * (0.5f * overlap.transform.localScale.z));

        Vector3 startDrag;
        if (CursorEditPlaneIntersect(p, out startDrag)) {
            Vector3 localDrag = transform.InverseTransformPoint(startDrag);
            Vector3 fixedPoint = new Vector3(
                -Mathf.Sign(localDrag.x) * 0.5f * (overlap.transform.localScale.x - overlapAmount),
                -Mathf.Sign(localDrag.y) * 0.5f * (overlap.transform.localScale.y - overlapAmount),
                -0.5f * (overlap.transform.localScale.z - overlapAmount)
                );
            fixedPoint = transform.TransformPoint(fixedPoint);

            Vector3 startPos = t.position;
            while (Input.GetButton("Fire1")) {
                Vector3 newPoint;
                if (CursorEditPlaneIntersect(p, out newPoint)) {
                    Vector3 newLocalPoint = transform.InverseTransformPoint(newPoint);
                    Vector3 fixedLocalPoint = transform.InverseTransformPoint(fixedPoint);
                    if (handleIndex % 2 == 1) {
                        newPoint = new Vector3(-fixedLocalPoint.x, newLocalPoint.y, -fixedLocalPoint.z);
                    }
                    else {
                        newPoint = new Vector3(newLocalPoint.x, -fixedLocalPoint.y, -fixedLocalPoint.z);
                    }

                    //Debug.DrawLine(transform.TransformPoint(fixedLocalPoint), transform.TransformPoint(newPoint), Color.red);
                    t.transform.localScale = Data.RoundVector(Data.ClampVector(Data.AbsVector(t.InverseTransformDirection(transform.TransformDirection(fixedLocalPoint - newPoint))), 0.2f, 100f),0.2f);
                    if (selOp.constrainToBp) {
                        t.position = selOp.constrainToBp.ClampPos(transform.TransformPoint(0.5f * (fixedLocalPoint + newPoint)));
                    }
                    else {
                        t.position = Data.RoundVector(transform.TransformPoint(0.5f * (fixedLocalPoint + newPoint)), 0.1f);
                    }

                    ui.propertiesPanel.CopyFromTransform(t);
                }
                yield return null;
            }
            SendUpdateSelectedTransform();
            Select(t);
        }

        yield return null;
    }

    public bool CursorEditPlaneIntersect(Plane p, out Vector3 point) {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        float enter = 0.0f;
        point = Vector3.zero;

        if (p.Raycast(ray, out enter)) {
            point = ray.GetPoint(enter);
            return true;
        }
        return false;
    }

    public void Deselect() {

        ui.propertiesPanel.gameObject.SetActive(false);
        ui.blueprintPanel.gameObject.SetActive(false);

        transform.localScale = Vector3.zero;

        if (selected) {
            Collider[] cols = selected.GetComponents<Collider>();
            for (int i = 0; i < cols.Length; i++) {
                cols[i].enabled = true;
            }
        }
        selected = null;
        selOp = null;
        selBp = null;
        selMm = null;
        //ui.Log(0, "deselected");
    }

    public void ScaleToEditPlane() {
        transform.position = selected.transform.position;
        Vector3 size = selected.transform.TransformPoint(1f, 1f, 1f);
        transform.localScale = Vector3.one;
        size = transform.InverseTransformPoint(size);
        overlap.transform.localScale = Data.AbsVector(size) + Vector3.one * 0.02f;
        editPlane.transform.localPosition = new Vector3(0f, 0f, 0.5f * overlap.transform.localScale.z);
        editPlane.transform.localScale = Vector3.one;
        float rs = Mathf.Min(0.1f * Mathf.Min(overlap.transform.localScale.x - overlapAmount, overlap.transform.localScale.y - overlapAmount), 1f);
        float hs = Mathf.Clamp((Camera.main.transform.position - editPlane.transform.position).magnitude * 0.01f,
            0.02f, rs);
        //hs = 0.1f;

        cornerHandles[0].transform.localPosition = new Vector3(0.5f * (overlap.transform.localScale.x - overlapAmount) - 0.5f * hs, 0f, 0f);
        cornerHandles[0].transform.localScale = new Vector3(hs, (1 - hs) * (overlap.transform.localScale.y - overlapAmount), hs);
        cornerHandles[1].transform.localPosition = new Vector3(0f, 0.5f * (overlap.transform.localScale.y - overlapAmount) - 0.5f * hs, 0f);
        cornerHandles[1].transform.localScale = new Vector3((1 - hs) * (overlap.transform.localScale.x - overlapAmount), hs, hs);
        cornerHandles[2].transform.localPosition = new Vector3(-0.5f * (overlap.transform.localScale.x - overlapAmount) + 0.5f * hs, 0f, 0f);
        cornerHandles[2].transform.localScale = new Vector3(hs, (1 - hs) * (overlap.transform.localScale.y - overlapAmount), hs);
        cornerHandles[3].transform.localPosition = new Vector3(0f, -0.5f * (overlap.transform.localScale.y - overlapAmount) + 0.5f * hs, 0f);
        cornerHandles[3].transform.localScale = new Vector3((1 - hs) * (overlap.transform.localScale.x - overlapAmount), hs, hs);

        float r = Mathf.Max(3f * Mathf.Min(overlap.transform.localScale.x, overlap.transform.localScale.y), 1f);
        forwardRotHandle.transform.localPosition = new Vector3(0f, 0f, 0.5f * overlap.transform.localScale.z + 0.1f);
        forwardRotHandle.transform.localScale = new Vector3(r, r, 0.1f);
    }

    public void Highlight() {
        transform.position = selected.transform.position;
        transform.rotation = selected.transform.rotation;
        transform.localScale = Vector3.one;
        overlap.transform.localScale = selected.transform.localScale + Vector3.one * overlapAmount;
    }

    public void Manipulate() {
        Collider[] cols = selected.GetComponents<Collider>();
        for (int i = 0; i < cols.Length; i++) {
            cols[i].enabled = false;
        }
    }

    private void CursorUIClick(RaycastHit hit) {
        if (hit.collider.gameObject == moveHandle) {
            StartCoroutine(MoveSelected());
        }
    }

    public void HideManipulator() {
        editPlane.transform.position = Vector3.zero;
        editPlane.transform.localScale = Vector3.zero;
        forwardRotHandle.transform.localScale = Vector3.zero;
    }
    public bool CanEditSelection() {
        //&& (selOp.constrainToBp || selOp.objectProxyData.pt == ObjectType.Conveyor)
        return selected && selOp  && !selBp && 
            (selOp.objectProxyData.owner < 0 || selOp.objectProxyData.owner == ui.client.myOwnerId);
    }

    public bool CanPickUp() {
        return selected && selOp && !selOp.constrainToBp && !selBp && (selOp.objectProxyData.pt != ObjectType.Conveyor) &&
            (selOp.objectProxyData.owner < 0 || selOp.objectProxyData.owner == ui.client.myOwnerId);
    }

    public void Select(Transform t) {

        if (selected) {
            Deselect();
        }

        selected = t.gameObject;
        selOp = t.GetComponent<ObjectProxy>();
        selBp = t.GetComponent<Blueprint>();
        if (!selOp && t.parent) {
            selOp = t.parent.GetComponent<ObjectProxy>();
            selBp = t.parent.GetComponent<Blueprint>();
        }

        selMm = t.GetComponentInChildren<ModularMesh>();
        if (selBp) {
            ui.propertiesPanel.gameObject.SetActive(true);
            ui.propertiesPanel.ShowTransform(false);
            ui.propertiesPanel.GetColor();
            ui.blueprintPanel.gameObject.SetActive(true);
            StartCoroutine(ui.blueprintPanel.UpdatePanel(selBp));
            HideManipulator();
        }
        else if (selOp) {
            ui.propertiesPanel.gameObject.SetActive(true);
            ui.propertiesPanel.ShowTransform(true);
            ui.blueprintPanel.gameObject.SetActive(false);
            ui.propertiesPanel.CopyFromTransform(t);
            ui.propertiesPanel.GetColor();
            if (CanEditSelection()) {
                Manipulate();
            }
            else {
                HideManipulator();
            }
        }
    }

    void Start() {

    }
    void Update() {
        if (selected) {
            if (CanEditSelection()) {
                RotateToEditPlane();
                ScaleToEditPlane();
            }
            else {
                Highlight();
            }
        }
    }
}
