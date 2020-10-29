using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlueprintPanel : MonoBehaviour {

    public ClientUI ui;

    public Text[] resourceValues;
    public RectTransform[] resourceBars;

    public Text bpName;

    public GameObject transactButton;
    public List<GameObject> transactButtons;

    void Start() {

    }

    public IEnumerator UpdatePanel(Blueprint bp) {
        bpName.text = "Base";
        int totalReq = 0;
        int totalComp = 0;
        for(int i = 0; i < resourceValues.Length; i++) {
            if (i == 5) {
                if (totalReq == 0) {
                    resourceValues[i].rectTransform.parent.gameObject.SetActive(false);
                }
                else {
                    resourceValues[i].rectTransform.parent.gameObject.SetActive(true);
                    resourceValues[i].text = ((100 * totalComp) / totalReq).ToString() + " %";
                    resourceBars[i].localScale = new Vector3((float)totalComp / (float)totalReq, 1f, 1f);
                }
            }
            else {

                if (bp.requirements[i] == 0) {
                    resourceValues[i].rectTransform.parent.gameObject.SetActive(false);
                }
                else {
                    resourceValues[i].rectTransform.parent.gameObject.SetActive(true);
                    resourceValues[i].text = bp.completed[i].ToString() + " / " + bp.requirements[i].ToString();
                    resourceBars[i].localScale = new Vector3((float)bp.completed[i] / (float)bp.requirements[i], 1f, 1f);
                }
                totalReq += bp.requirements[i];
                totalComp += bp.completed[i];
            }
        }
        for(int i = transactButtons.Count-1; i >= 0; i--) {
            Destroy(transactButtons[i]);
        }

        Debug.Log(ui.client.myResources[6].ToString() + ", " + ui.client.myResources[7].ToString());
        yield return null;
        transactButtons.Clear();
        if (bp.op.objectProxyData.owner == ui.client.myOwnerId) {
            for (int i = 0; i < bp.transacts.Count; i++) {
                if ((bp.transacts[i].price[6] == -1 || bp.transacts[i].price[6] == ui.client.myResources[6]) &&
                    (bp.transacts[i].price[7] == -1 || bp.transacts[i].price[7] == ui.client.myResources[7]) ){
                    GameObject newButton = Instantiate(transactButton, transform);
                    int h = i;
                    newButton.GetComponentInChildren<Button>().onClick.AddListener(delegate { OnTransaction(h); });
                    newButton.GetComponentInChildren<Text>().text = bp.transacts[i].name;
                    transactButtons.Add(newButton);
                }
                
            }
        }
    }

    public void OnTransaction(int index) {
        //Debug.Log(index);
        ui.client.SendPlayerTransaction(index);
    }

    void Update() {

    }
}
