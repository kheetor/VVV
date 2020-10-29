using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Transaction {
    int id;
    public string name;
    public int[] price;
    public int[] reward;

    public Transaction(int[] price, string name, int[] reward, int id) {
        this.id = id;
        this.name = name;
        this.price = price;
        this.reward = reward;
    }

    public bool CanTransact(int[] myResources) {
        bool enough = true;
        for(int i = 0; i < Mathf.Min(myResources.Length, price.Length); i++) {
            enough = (enough && (myResources[i] >= price[i]));
        }
        Debug.Log("Can transact " + enough.ToString());
        return enough;
    }

}

public class Blueprint : MonoBehaviour {

    public ObjectProxy op;

    public Event OnCompleteEvent;

    public int[] requirements = new int[6];
    public int[] completed = new int[6];

    public int progress = 0;

    public List<Transaction> transacts = new List<Transaction>();

    public Bounds site = new Bounds(Vector3.up*5f, Vector3.one * 10f);

    public ServerBpData bpData;

    public Vector3 ClampPos(Vector3 pos) {
        Vector3 localPoint = transform.InverseTransformPoint(pos);
        return transform.TransformPoint(new Vector3(
            Mathf.Clamp(localPoint.x, site.center.x - site.extents.x, site.center.x + site.extents.x),
            Mathf.Clamp(localPoint.y, site.center.y - site.extents.y, site.center.y + site.extents.y),
            Mathf.Clamp(localPoint.z, site.center.z - site.extents.z, site.center.z + site.extents.z)
            ));
    }

    public void UpdateData() {
        if(bpData == null) {
            bpData = new ServerBpData(op.id, completed);
        }
        else {
            bpData.id = op.id;
            bpData.resources = completed;
        }
    }

    void Start() {
        UpdateData();
    }

    void Update() {

    }
}
