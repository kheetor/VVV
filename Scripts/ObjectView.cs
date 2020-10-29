using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UpdateMode {
    Instant,
    Lerp
}

public class ObjectView : MonoBehaviour {

    public bool isServer = false;
    public UpdateMode updateMode = UpdateMode.Instant;

    public int maxObjects = 10000;
    public int numObjects = 0;
    public int maxIndex = 0;

    public ObjectProxy[] obs;

    public GameObject objectTemplateOrganic;
    public GameObject objectTemplateMineral;
    public GameObject objectTemplateMetal;

    public GameObject objectTemplateCube;
    public GameObject objectTemplateSphere;
    public GameObject objectTemplateTri;
    public GameObject objectTemplateCylinder;
    public GameObject objectTemplateConv;

    public GameObject[] objectTemplatePlants;

    //dummy
    private MeshFilter mf;
    private MeshRenderer mr;
    private BoxCollider bc;
    private SphereCollider sc;

    private Transform player;

    void Start() {

        GameObject p = GameObject.FindWithTag("Player");

        if (p) {
            player = p.transform;
        }
        else {
            player = transform;
        }
    }

    public void InitObjects() {
        ObjectProxy[] initObs = transform.GetComponentsInChildren<ObjectProxy>();
        obs = new ObjectProxy[maxObjects];
        for (int i = 0; i < initObs.Length; i++) {
            initObs[i].objectProxyData = new ObjectProxyData(i, 0, -1, ObjectType.StaticBase, new SrTransform(initObs[i].transform), new int[] { 0, 100, 0 });
            initObs[i].id = i;
            obs[i] = initObs[i];
        }
        numObjects = initObs.Length;
        maxIndex = numObjects - 1;
    }

    public ObjectProxy CheckCreateObject(int id) {
        ObjectProxy obj = GetObject(id);
        if (obj) {
            return obj;
        }
        else {
            return CreateProxy(ObjectType.CacheCube1, Vector3.zero, Color.white, -1, id);
        }
    }

    public ObjectProxy GetObject(int id) {
        for (int i = 0; i < obs.Length - 1; i++) {
            if (obs[i] && obs[i].id == id) {
                return obs[i];
            }
        }
        return null;
    }

    private struct SortObjectData {
        public Transform t;
        public float dist;

        public SortObjectData(Transform t, float dist) {
            this.t = t;
            this.dist = dist;
        }
    }

    public void CheckClearFar() {
        if (isServer || transform.childCount < 0.9f * maxObjects) {
            return;
        }
        Debug.Log("Cleaning object pool");
        List<SortObjectData> sortObjects = new List<SortObjectData>();
        for (int i = 0; i < transform.childCount - 1; i++) {
            sortObjects.Add(new SortObjectData(transform.GetChild(i), (transform.GetChild(i).position - player.position).sqrMagnitude));
        }
        sortObjects.Sort((p1, p2) => p1.dist.CompareTo(p2.dist));

        for (int i = sortObjects.Count - 1; i > 0.9f * maxObjects; i--) {
            Destroy(sortObjects[i].t.gameObject);
            numObjects -= 1;
        }
    }

    public void UpdateObjects() {
        for (int i = 0; i < obs.Length; i++) {
            if (obs[i] == null) {
                obs[i] = null;
            }
        }
    }

    public int PlaceObject(ObjectProxy op) {
        for (int i = 0; i < obs.Length; i++) {
            if (obs[i] == null) {
                obs[i] = op;
                numObjects += 1;
                maxIndex = Mathf.Max(maxIndex, i);
                return i;
            }
        }
        Debug.LogError("Object pool overflow");
        return 0;
    }

    public ObjectProxy CreateProxy(ObjectType pt, Vector3 pos, Color col, int bluePrint, int id = -1) {
        Debug.Log("creating " + id.ToString() + " : " + pt.ToString());
        CheckClearFar();
        GameObject template = objectTemplateCube;
        if ((int)pt == (int)ObjectType.CacheCylinder1) {
            template = objectTemplateCylinder;
        }
        else if ((int)pt == (int)ObjectType.CacheSphere1) {
            template = objectTemplateSphere;
        }
        else if ((int)pt == (int)ObjectType.CacheTri1) {
            template = objectTemplateTri;
        }
        else if ((int)pt == (int)ObjectType.NaturalA1) {
            template = objectTemplateOrganic;
        }
        else if ((int)pt == (int)ObjectType.NaturalA2) {
            template = objectTemplateMineral;
        }
        else if ((int)pt == (int)ObjectType.NaturalA3) {
            template = objectTemplateMetal;
        }
        else if (pt == ObjectType.KissaBase) {
            template = objectTemplatePlants[0];
        }
        else if (pt == ObjectType.PlayerBase) {
            template = objectTemplatePlants[1];
        }
        else if (pt == ObjectType.Conveyor) {
            template = objectTemplateConv;
        }

        GameObject go = Instantiate(template, pos, Quaternion.identity);
        go.transform.SetParent(transform);
        go.transform.position = pos;

        ObjectProxy op = go.GetComponent<ObjectProxy>();
        if (!op) {
            op = go.AddComponent<ObjectProxy>();
        }

        if (id < 0) {
            op.id = PlaceObject(op);
        }
        else {
            op.id = id;
            PlaceObject(op);
        }

        // This proxy is Blueprint object
        Blueprint bp = go.GetComponent<Blueprint>();
        if (bp) {
            Debug.Log("bp found");
            bp.op = op;
            bp.UpdateData();
        }

        // This proxy is Conveyor object
        Conveyor conv = go.GetComponent<Conveyor>();
        if (conv) {
            conv.ov = this;
            go.transform.localScale = new Vector3(1f, 2f, 10f);
        }

        // This proxy belongs to blueprint
        if (bluePrint >= 0) {
            op.constrainToBp = CheckCreateObject(bluePrint).gameObject.GetComponent<Blueprint>();
        }

        op.state = 0;

        return op;
    }

    public void UpdateObjColor(ObjectProxy obj) {
        Color col = Data.DesrColor(obj.objectProxyData.col);

        DynamicColorMat dc = obj.GetComponentInChildren<DynamicColorMat>();

        if (!dc) {
            MeshRenderer r = obj.GetComponent<MeshRenderer>();
            if (!r) {
                return;
            }
            r.material.SetColor("_Color", col);
        }
        else {
            dc.UpdateColor(col);
        }
    }

    private bool IsCarried(ObjectProxy obj) {
        return (obj.transform.parent && obj.transform.parent.parent && obj.transform.parent.parent.gameObject.tag == "Player");
    }

    public void ReceiveServerBpData(ServerBpData bpData) {
        
    }

    public void ReceiveObjectProxyData(ObjectProxyData objectProxyData) {
        //Debug.Log("received " + objectProxyData.id +  " : " + objectProxyData.pt.ToString());
        ObjectProxy obj = GetObject(objectProxyData.id);

        if (objectProxyData.pt == ObjectType.Destroyed) {
            if (obj) {
                Destroy(obj.gameObject);
            }
            return;
        }

        if (!obj || obj.objectProxyData == null || obj.objectProxyData.pt != objectProxyData.pt) {
            if (obj) {
                Destroy(obj.gameObject);
            }
            obj = CreateProxy(objectProxyData.pt, Vector3.zero, Data.DesrColor(objectProxyData.col), objectProxyData.constrainedBy, objectProxyData.id);
        }

        //Update pos
        objectProxyData.t.CopyToTransform(obj.transform);

        //Check for changes in constrain field
        if (objectProxyData.constrainedBy >= 0) {
            ObjectProxy constOp = GetObject(objectProxyData.constrainedBy);
            if (constOp) {
                Conveyor conv = constOp.GetComponent<Conveyor>();
                if (conv && !conv.obs.Contains(obj)) {
                    conv.Attach(obj);
                }
                obj.constrainToBp = constOp.gameObject.GetComponent<Blueprint>();
            }
            else {

            }
        }

        obj.objectProxyData = objectProxyData;
        UpdateObjColor(obj);
    }

    public void ReceiveObjectEdit(PlayerEditObject playerEditObject, ClientProxy cp) {
        ObjectProxy obj = GetObject(playerEditObject.controlId);
        if (playerEditObject.editType == EditType.Transform) {
            TransformObject(obj, playerEditObject.t);
        }
        else if (playerEditObject.editType == EditType.Deposit) {
            TransformObject(obj, playerEditObject.t);
            DepositObject(obj, playerEditObject.otherId);
        }
        else if (playerEditObject.editType == EditType.Color) {
            obj.objectProxyData.col = playerEditObject.t.pos;
        }
        else if (playerEditObject.editType == EditType.Destroy) {
            Destroy(obj.gameObject);
        }
        else if (playerEditObject.editType == EditType.Carry) {
            obj.objectProxyData.owner = cp.info.id;
            obj.objectProxyData.t.scale = new int[] { 0, 0, 0 };
            obj.UpdateState();
        }
    }

    public void DepositObject(ObjectProxy obj, int otherId) {
        //Debug.Log(obj.id.ToString() + " tries to attach to " + otherId.ToString());
        ObjectProxy otherObj = GetObject(otherId);
        if (!otherObj) {
            Debug.LogError("No other object found " + otherId);
            return;
        }
        DepositObject(obj, otherObj);
    }

    public void DepositObject(ObjectProxy obj, Blueprint bp) {
        Debug.Log("Depositing " + obj.objectProxyData.pt);
        if (obj.objectProxyData.pt >= ObjectType.NaturalA1 && obj.objectProxyData.pt <= ObjectType.NaturalC5) {
            int res = Data.GetObjectResourceType(obj.objectProxyData.pt);
            bp.completed[res]++;
            //Debug.Log("Bp res " + res + " is now " + bp.completed[res]);
            bp.UpdateData();
            MarkObjectDestroyed(obj);
        }
        else {
            obj.constrainToBp = bp;
            obj.objectProxyData.constrainedBy = bp.op.id;
            obj.UpdateState();

            Collider[] cols = obj.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++) {
                cols[i].enabled = true;
            }
        }

        //Debug.Log("Deposit! Constrained " + obj.id + " to BP: " + obj.objectProxyData.constrainedBy);
        return;
    }

    public void DepositObject(ObjectProxy obj, Conveyor conv) {
        if (conv.Attach(obj)) {
            obj.objectProxyData.constrainedBy = conv.GetComponent<ObjectProxy>().id;
            //Debug.Log("Deposit! Attached to conv " + obj.objectProxyData.constrainedBy);
        }
    }

    public void DepositObject(ObjectProxy obj, ObjectProxy otherObj) {

        Blueprint bp = otherObj.GetComponent<Blueprint>();
        if (bp) {
            DepositObject(obj, bp);
        }
        Conveyor conv = otherObj.GetComponent<Conveyor>();
        if (conv) {
            DepositObject(obj, conv);
        }
    }

    public void MarkObjectDestroyed(ObjectProxy obj) {
        obj.objectProxyData.pt = ObjectType.Destroyed;
        Collider[] cols = obj.GetComponents<Collider>();
        for (int i = 0; i < cols.Length; i++) {
            cols[i].enabled = false;
        }
        obj.UpdateState();
    }

    public void DestroyObjectProxy(ObjectProxy obj) {
        Debug.Log("destroying " + obj.ToString());
        Destroy(obj.gameObject);
    }

    public void TransformObject(ObjectProxy obj, SrTransform t) {
        t.CopyToTransform(obj.transform);
        obj.objectProxyData.t = t;
        obj.UpdateState();
    }

    void Update() {

    }
}
