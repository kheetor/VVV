using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour {

    public Cursor cursor;
    public ClientUI ui;
    public Client client;
    public Transform yaw;
    public Transform pitch;

    public float moveSpeed = 5f;
    public float flySpeed = 5f;
    public float gravityForce = 2f;
    public CharacterController drone1Char;
    public WheelCollider[] wheels;
    public float motorForce = 50f;
    public float brakeForce = 50f;
    public float maxSpeed = 25f;
    public float maxAngle = 30f;

    public Vector2 mouseLookSensitivity = Vector2.one * 10f;
    public float camDist = 10f;
    public float zoomSens = 10f;

    private bool mouseDrive = false;
    private float currentPitch;
    private float mouseMove = 0f;

    public LayerMask blockCamLayers = new LayerMask();
    public LayerMask selectLayers;

    private int activeHandle = -1;

    public int activeDrone = 0;
    public Transform[] drones;

    public Transform pickupPanel;
    public ObjectProxy hlObj;
    public float hlThreshold = 0.5f;
    public float hlSqrDist = 100f;

    public List<GameObject> slotItems = new List<GameObject>();

    public List<Vector3> slotPosition = new List<Vector3>();
    public Vector3 slotSize = Vector3.one * 0.3f;

    public float traPower = 20f;
    public float throwSpeed = 10f;
    public float traBounce = 0.5f;

    public LineRenderer l;

    public GameObject targetCursor;
    public LayerMask lm;

    public TileSystem ts;

    void Start() {
        ui.gameObject.SetActive(true);
        targetCursor.transform.localScale = Vector3.zero;
        LoadDroneProfile();
    }

    void Update() {
        if (client.status == ClientStatus.connected) {
            if (mouseMove > 0.1f && !Input.GetButton("Fire1")) {
                MouseHoverUpdate();
                mouseMove = 0f;
            }
            mouseMove += Mathf.Abs(Input.GetAxis("Mouse X")) + Mathf.Abs(Input.GetAxis("Mouse Y"));

            if (Input.GetButtonDown("Fire1") || Input.GetButtonDown("Fire2")) {
                if (!ui.RayCastUI()) {
                    StartCoroutine(MouseClick(Input.GetButtonDown("Fire2")));
                }
            }
            GetInput();
            UpdateSlotPositions();
        }
    }

    public void MouseHoverUpdate() {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!ui.RayCastUI() && Physics.Raycast(ray, out hit, 50f, selectLayers)) {
            activeHandle = -1;
            for (int i = 0; i < cursor.cornerHandles.Length; i++) {
                if (hit.collider.gameObject == cursor.cornerHandles[i]) {
                    cursor.cornerHandles[i].GetComponent<MeshRenderer>().enabled = true;
                    activeHandle = i;
                }
                else {
                    cursor.cornerHandles[i].GetComponent<MeshRenderer>().enabled = false;
                }
            }
        }
        //Debug.Log("hover over : " + activeHandle.ToString());
    }

    public void StartCreateBase() {
        if (client.myResources[7] > 0) {
            ui.Log(100, "You already have a base");
            return;
        }
        StartCoroutine(PickPosCreate(ObjectType.PlayerBase, 16f));
    }

    public void StartCreateCube() {
        StartCoroutine(PickPosCreate(ObjectType.CacheCube1, 1f));
    }

    public IEnumerator PickPosCreate(ObjectType pt, float size) {
        while (!Input.GetButtonDown("Fire1") && !Input.GetButtonDown("Fire2")) {
            RaycastHit hit;
            targetCursor.transform.localScale = Vector3.one * size;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit)) {
                if (pt == ObjectType.PlayerBase) {
                    Vector3 outPos;
                    if (Data.GetBasePos(hit.point, out outPos)) {
                        targetCursor.transform.position = outPos;
                    }
                }
                else {
                    targetCursor.transform.position = hit.point;
                }
            }

            yield return null;
        }

        if (Input.GetButtonDown("Fire1")) {
            SrTransform t = new SrTransform(Data.SrVector(targetCursor.transform.position, 1000f), new int[3], new int[] { 1, 1, 1 });

            //Debug.Log(targetCursor.transform.position);
            client.SendPlayerCreateObject(pt, t);
        }

        targetCursor.transform.localScale = Vector3.zero;
    }

    public IEnumerator MouseClick(bool rightClick = false) {
        float cursorMove = 0f;
        while ((Input.GetButton("Fire1") || Input.GetButton("Fire2")) && cursorMove < 1f) {
            cursorMove += Mathf.Abs(Input.GetAxis("Mouse X")) + Mathf.Abs(Input.GetAxis("Mouse Y"));
            yield return null;
        }

        // Drag
        float startTime = Time.time;
        RaycastHit hit;
        if (!rightClick && cursor.selected && (activeHandle != -1 | Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 50f, selectLayers)) &&
            cursor.EditSelected(hit, activeHandle)) {
        }
        else {
            mouseDrive = rightClick;
            if (mouseDrive && activeDrone == 0) {
                drones[activeDrone].LookAt(drones[activeDrone].position + yaw.forward, Vector3.up);
                //yaw.localEulerAngles = Vector3.zero;
            }
            while (Input.GetButton("Fire1") || Input.GetButton("Fire2")) {
                //drone1Char.transform.LookAt(drones[0].transform.position + yaw.forward, Vector3.up);
                /*
                if (mouseDrive && activeDrone == 0) {
                    drones[activeDrone].Rotate(Vector3.up, Input.GetAxis("Mouse X") * mouseLookSensitivity.x);
                }
                else {
                    yaw.Rotate(Vector3.up, Input.GetAxis("Mouse X") * mouseLookSensitivity.x);
                }
                */
                yaw.Rotate(Vector3.up, Input.GetAxis("Mouse X") * mouseLookSensitivity.x);
                if (mouseDrive && activeDrone == 0) {
                    drones[activeDrone].LookAt(drones[activeDrone].position + yaw.forward, Vector3.up);
                }

                currentPitch = Mathf.Clamp(currentPitch + Input.GetAxis("Mouse Y") * mouseLookSensitivity.y * -1f, -90f, 90f);
                pitch.localEulerAngles = new Vector3(currentPitch, 0f, 0f);
                yield return null;
            }
        }

        if (!Input.GetButton("Fire1") && Time.time - startTime < 0.2f) {
            RayCastSelect();
        }
        mouseDrive = false;
    }

    public void RayCastSelect(bool rightClick = false) {
        //new Vector3(Camera.main.pixelWidth * 0.5f, Camera.main.pixelHeight * 0.5f, 0f)
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, selectLayers) &&
            (hit.transform.GetComponentInParent<ObjectProxy>())) {
            //ui.Log(0, hit.collider.gameObject.name);
            cursor.Select(hit.transform);
        }
        else {
            cursor.Deselect();
        }
    }

    public void Drone1Input() {
        yaw.position = drones[0].position;
        float keyboardTurnSpeed = 120f;
        Vector3 movement = Time.deltaTime * (Vector3.down * gravityForce
            + drones[0].transform.up * ((Input.GetButton("Jump") ? 1f : 0f) - (Input.GetButton("Crouch") ? 1f : 0f)));

        if (mouseDrive) {
            movement += (
                drones[0].transform.right * Input.GetAxis("Horizontal") +
                drones[0].transform.forward * Input.GetAxis("Vertical")).normalized * moveSpeed * Time.deltaTime;
            drones[0].transform.rotation = yaw.rotation;
        }
        else {
            movement += (drones[0].transform.forward * Input.GetAxis("Vertical")).normalized * moveSpeed * Time.deltaTime;
            drones[0].transform.Rotate(Vector3.up, Input.GetAxis("Horizontal") * Time.deltaTime * keyboardTurnSpeed);
            yaw.Rotate(Vector3.up, Input.GetAxis("Horizontal") * Time.deltaTime * keyboardTurnSpeed);
        }

        drone1Char.Move(movement);

        drones[0].transform.position = Data.ClampVector(drones[0].transform.position, -999f, 999f);
    }

    public void FlipDrone() {
        if (activeDrone != 1) {
            return;
        }
        drones[1].LookAt(yaw.position + new Vector3(drones[1].forward.x, 0f, drones[1].forward.z).normalized, Vector3.up);
        drones[1].position += Vector3.up * 0.5f;
        drones[1].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }

    public void ResetPosition() {
        if (activeDrone == 0) {
            drones[0].gameObject.SetActive(false);
        }
        drones[activeDrone].position = new Vector3(100f, 7f, -100f);
        yaw.position = drones[activeDrone].position;

        ts.UpdateTileVisibility();

        if (activeDrone == 0) {
            drones[0].gameObject.SetActive(true);
        }
        FlipDrone();

    }

    public void Drone2Input() {
        yaw.position = drones[1].position;
        Rigidbody rb = drones[1].GetComponent<Rigidbody>();
        float vert = Input.GetAxis("Vertical");
        float hor = Input.GetAxis("Horizontal");
        Vector3 v = drones[1].InverseTransformDirection(rb.velocity);
        if (Input.GetKeyDown(KeyCode.F)) {
            drones[1].LookAt(yaw.position + drones[1].forward, Vector3.up);
            drones[1].position += Vector3.up * 0.5f;
            drones[1].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }

        float motor = Mathf.Clamp(vert * motorForce, -motorForce, motorForce);
        if (v.z > maxSpeed || v.z < -0.2f * maxSpeed) {
            motor = 0f;
        }

        float brake = 0f; // = (Input.GetKey(KeyCode.Space) ? 1f : 0f) * brakeForce;
        if (Input.GetKey(KeyCode.Space) || Mathf.Clamp01(vert * -v.z) > 0.1f) {
            brake = brakeForce;
        }
        //Debug.Log("motor " + motor.ToString() + ", brake " + brake.ToString() + " vel " + v.z.ToString());

        Vector3 pos;
        Quaternion quat;
        for (int i = 0; i < wheels.Length; i++) {
            //wheels[i].forceAppPointDistance = 0.5f;

            wheels[i].brakeTorque = brake;

            wheels[i].motorTorque = motor;
            if (i < 2) {
                wheels[i].steerAngle = Input.GetAxis("Horizontal") * maxAngle;
            }

            wheels[i].GetWorldPose(out pos, out quat);
            wheels[i].transform.GetChild(0).position = pos;
            wheels[i].transform.GetChild(0).rotation = quat;
        }

        //drones[1].GetComponent<Rigidbody>().angularVelocity = new Vector3(av.x, av.y, 0f);
    }

    private void CheckResetPos() {
        Vector3 pos = drones[activeDrone].transform.position;
        if (Mathf.Abs(pos.x) > 1000f || Mathf.Abs(pos.z) > 1000f || pos.y > 300f || pos.y < -5) {
            ResetPosition();
        }
    }

    public void SwitchDrone1() {
        SwitchDrone(0);
    }

    public void SwitchDrone2() {
        SwitchDrone(1);
    }

    public void SwitchDrone(int newActiveDrone) {
        if(client.myResources[6]<newActiveDrone) {
            ui.Log(100, "Drone not yet unlocked");
            return;
        }
        if (slotItems.Count > 0) {
            ui.Log(100, "Can't switch drones while carrying cargo");
            return;
        }

        Vector3 pos = drones[activeDrone].position;
        Vector3 forward = drones[activeDrone].forward;
        activeDrone = newActiveDrone;
        drones[activeDrone].position = pos;
        //yaw.position = pos;

        if (activeDrone == 0) {
            drones[0].gameObject.SetActive(true);
            drones[1].gameObject.SetActive(false);
            drones[1].GetComponent<Rigidbody>().isKinematic = true;
        }
        else {
            drones[0].gameObject.SetActive(false);
            drones[1].gameObject.SetActive(true);
            drones[1].GetComponent<Rigidbody>().isKinematic = false;
        }

        //yaw.SetParent(drones[activeDrone]);
        //yaw.localEulerAngles = Vector3.zero;
        drones[activeDrone].LookAt(drones[activeDrone].position + new Vector3(forward.x, 0f, forward.z), Vector3.up);
        LoadDroneProfile();
    }

    private void LoadDroneProfile() {
        if (activeDrone == 0) {
            traPower = 10f;
            traBounce = 0.7f;
            throwSpeed = 10f;
            slotPosition.Clear();
            slotPosition.Add(new Vector3(0f, 0.5f, 0f));
        }
        if (activeDrone == 1) {
            traPower = 30f;
            traBounce = 0.7f;
            throwSpeed = 30f;
            slotPosition.Clear();
            slotPosition.Add(new Vector3(0f, 1f, 1f));
            slotPosition.Add(new Vector3(0f, 1f, 0f));
            slotPosition.Add(new Vector3(0f, 1f, -1f));
        }
    }

    public void GetInput() {

        CheckResetPos();

        if (Input.GetKeyDown(KeyCode.V)) {
            SwitchDrone((activeDrone + 1) % drones.Length);
        }

        if (Input.GetKeyDown(KeyCode.Q)) {
            if (slotItems.Count < slotPosition.Count && cursor.CanPickUp()) {
                GameObject go = cursor.selected;
                cursor.Deselect();
                PickUp(go);
            }
            else if (!cursor.selected && hlObj) {
                PickUp(hlObj.gameObject);
            }
        }

        if (Input.GetKeyDown(KeyCode.E) && slotItems.Count > 0) {
            StartCoroutine(Throw(slotItems[0], lm));
        }

        CameraPosition();

        if (activeDrone == 0) {
            Drone1Input();
        }
        else if (activeDrone == 1) {
            Drone2Input();
        }

        client.SendPlayerPosition();
    }

    public void HighlightPickup() {

        if (slotItems.Count < slotPosition.Count) {
            Vector3 pos;
            for (int i = 0; i < client.ov.maxIndex; i++) {
                if (!client.ov.obs[i]) {
                    continue;
                }
                if (CanPickup(client.ov.obs[i])) {
                    pos = Camera.main.transform.InverseTransformPoint(client.ov.obs[i].transform.position);
                    if (pos.z > 0 && Mathf.Abs(pos.x) + 0.5f * Mathf.Abs(pos.y) < hlThreshold &&
                        (client.ov.obs[i].transform.position - drones[activeDrone].position).sqrMagnitude < 400f &&
                        client.ov.obs[i].GetComponent<Collider>().enabled) {
                        pickupPanel.position = client.ov.obs[i].transform.position + 1.5f * Vector3.up;
                        pickupPanel.localScale = Vector3.one * 0.15f * Mathf.Clamp((client.ov.obs[i].transform.position - drones[activeDrone].position).magnitude / 20f, 0.5f, 1f);
                        pickupPanel.LookAt(Camera.main.transform, Vector3.up);
                        hlObj = client.ov.obs[i];
                        return;
                    }
                }
            }
        }
        pickupPanel.localScale = Vector3.zero;
        hlObj = null;
    }

    private bool CanPickup(ObjectProxy obj) {
        return !obj.constrainToBp && ((int)obj.objectProxyData.pt <= (int)ObjectType.NaturalC5) &&
            !(obj.transform.parent && obj.transform.parent.parent && obj.transform.parent.parent.gameObject.tag == "Player") &&
        (obj.objectProxyData.owner < 0 || obj.objectProxyData.owner == ui.client.myOwnerId);
    }

    public void PickUp(GameObject o) {
        slotItems.Add(o);
        Collider[] cols = o.GetComponents<Collider>();
        for (int i = 0; i < cols.Length; i++) {
            cols[i].enabled = false;
        }
        //o.transform.position = transform.position + Vector3.up * 2f;
        o.transform.SetParent(drones[activeDrone]);
        UpdateSlotPositions();
        client.SendPlayerEditObject(o.GetComponent<ObjectProxy>().id, EditType.Carry, new SrTransform(o.transform));
    }

    IEnumerator Throw(GameObject o, LayerMask lm) {
        slotItems.Remove(o);
        UpdateSlotPositions();

        o.transform.localScale = Vector3.zero;
        o.transform.SetParent(client.ov.transform);

        int simsPerCycle = 10;
        Trajectory tra = new Trajectory(drones[activeDrone].TransformPoint(slotPosition[0]), (Camera.main.transform.forward + Vector3.up * 0.5f) * traPower, 0.9f, lm);
        float mouseMove = 0f;
        while (Input.GetKey(KeyCode.E)) {
            mouseMove += Mathf.Abs(Input.GetAxis("Mouse X")) + Mathf.Abs(Input.GetAxis("Mouse Y"));
            if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.05f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.05f || (Input.GetButton("Fire2") && mouseMove > 0.1f)) {
                tra.progress = 0;
                tra.pos = drones[activeDrone].TransformPoint(slotPosition[0]);
                tra.velocity = (Camera.main.transform.forward + Vector3.up * 0.5f) * traPower;
                tra.otherHit = null;
                mouseMove = 0f;
            }
            for (int i = 0; i < simsPerCycle; i++) {
                if (tra.progress < 100) {
                    tra.SimStep();
                }
            }

            if (targetCursor) {
                if (tra.otherHit) {
                    targetCursor.transform.position = tra.points[tra.progress - 1];
                    targetCursor.transform.localScale = Vector3.one;
                }
                else {
                    targetCursor.transform.localScale = Vector3.zero;
                }
            }

            l.positionCount = tra.progress;
            l.SetPositions(tra.points);
            yield return null;
        }
        targetCursor.transform.localScale = Vector3.zero;
        while (tra.progress < 100) {
            tra.SimStep();
            l.positionCount = tra.progress;
            l.SetPositions(tra.points);
            yield return null;
        }
        l.positionCount = 0;

        UpdateSlotPositions();
        StartCoroutine(tra.Travel(o, throwSpeed));

        while (tra.travelProgress != 100) {
            yield return null;
        }

        ObjectProxy op = o.GetComponent<ObjectProxy>();
        if (tra.otherHit) {
            client.SendPlayerEditObjectDeposit(op.objectProxyData.id, new SrTransform(o.transform), tra.otherHit.id);
            if (tra.otherHit.GetComponent<Blueprint>()) {
                op.SetColliders(true);
            }
        }
        else {
            client.SendPlayerEditObjectTransform(op.objectProxyData.id, new SrTransform(o.transform));
            op.SetColliders(true);
        }
    }

    private void UpdateSlotPositions() {
        for (int i = 0; i < slotItems.Count; i++) {
            slotItems[i].transform.localEulerAngles = Vector3.zero;
            slotItems[i].transform.localScale = slotSize;
            slotItems[i].transform.localPosition = slotPosition[i];
        }
    }

    void CameraPosition() {
        camDist = Mathf.Clamp(camDist - zoomSens * Input.GetAxis("Mousewheel"), 0f, 30f);
        RaycastHit hit;
        if (Physics.SphereCast(yaw.position + Vector3.up * 0.5f - pitch.transform.forward, 0.2f, -pitch.transform.forward, out hit, camDist, blockCamLayers)) {
            Camera.main.transform.position = hit.point + 0.5f * hit.normal;
            //Debug.DrawLine(yaw.position + Vector3.up * 0.5f, hit.point, Color.red);
        }
        else {
            Camera.main.transform.localPosition = new Vector3(0f, 0f, -camDist);
            //Debug.DrawLine(yaw.position + Vector3.up * 0.5f, -pitch.transform.forward*camDist+yaw.position+Vector3.up*0.05f, Color.blue);
        }

        HighlightPickup();

    }

}
