using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClientUI : MonoBehaviour {

    public GameObject loginPanel;
    public Text userText;
    public Text passText;

    public GameObject infoPanel;
    public Text output;
    public Text statusText;
    private List<string> outputHistory = new List<string>();

    public GameObject resourcePanel;
    public Text[] resources;

    public PropertiesPanel propertiesPanel;

    public BlueprintPanel blueprintPanel;

    public Cursor cursor;

    public Client client;

    public int logLevel = 0;

    public bool ghost = false;

    private int focusField = 0;

    GraphicRaycaster m_Raycaster;
    PointerEventData m_PointerEventData;
    EventSystem m_EventSystem;

    void Start() {
        m_Raycaster = GetComponent<GraphicRaycaster>();
        m_EventSystem = GetComponent<EventSystem>();
    }

    void Update() {
        if (client.status == ClientStatus.login) {
            loginPanel.active = true;
            if (Input.GetButtonDown("Submit")) {
                Login();
            }
            else if (Input.GetKeyDown(KeyCode.Tab)) {
                if (focusField == 0) {
                    passText.transform.parent.GetComponent<InputField>().Select();
                    passText.transform.parent.GetComponent<InputField>().ActivateInputField();
                }
                else {
                    userText.transform.parent.GetComponent<InputField>().Select();
                    userText.transform.parent.GetComponent<InputField>().ActivateInputField();
                }
                focusField = (focusField + 1) % 2;
            }
            propertiesPanel.gameObject.active = false;
            blueprintPanel.gameObject.active = false;
        }
        else if (client.status == ClientStatus.connected) {
            loginPanel.active = false;
            //propertiesPanel.gameObject.active = (cursor.selected && !cursor.selBp);
            //blueprintPanel.gameObject.active = (cursor.selected && cursor.selBp);
        }
        else {
            loginPanel.active = false;
        }

        if (Input.GetKeyDown(KeyCode.PageUp)) {
            logLevel += 10;
            logLevel = Mathf.Clamp(logLevel, 0, 100);
            Log(200, "log level " + logLevel.ToString());
        }
        else if (Input.GetKeyDown(KeyCode.PageDown)) {
            logLevel -= 10;
            logLevel = Mathf.Clamp(logLevel, 0, 100);
            Log(200, "log level " + logLevel.ToString());
        }
    }

    public void UpdateResources(int[] newResources, bool bling = true) {
        for (int i = 0; i < resources.Length; i++) {
            resources[i].text = newResources[i].ToString();
        }
    }

    public bool RayCastUI() {
        m_PointerEventData = new PointerEventData(m_EventSystem);
        m_PointerEventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        m_Raycaster.Raycast(m_PointerEventData, results);

        return results.Count > 0;
    }

    public void ToggleGhost() {
        if (Physics.GetIgnoreLayerCollision(9,10)) {
            Physics.IgnoreLayerCollision(9, 10, false);
        }
        else {
            Physics.IgnoreLayerCollision(9, 10, true);
        }
    }

    public void Login() {
        client.userName = userText.text;
        client.passwordHash = Hash128.Compute(passText.text).ToString();
        client.SendPlayerAuth();
    }

    public void Register() {
        client.userName = userText.text;
        client.passwordHash = Hash128.Compute(passText.text).ToString();
        client.SendPlayerAuth(true);
    }

    public void Log(int lvl, params string[] args) {
        if (lvl < logLevel) {
            return;
        }
        int lines = 10;
        string s = "";
        for (int i = 0; i < args.Length; i++) {
            s += args[i] + " ";
        }
        Debug.Log(s);
        outputHistory.Insert(0, s);
        if (outputHistory.Count > 300) {
            outputHistory.RemoveRange(200, outputHistory.Count - 200 - 1);
        }
        s = "";
        int startIndex = Mathf.Min(lines, outputHistory.Count) - 1;
        for (int i = startIndex; i >= Mathf.Max(0, startIndex - lines); i--) {
            s += outputHistory[i] + "\n";
        }
        output.text = s;
    }

    public void CreateCube() {
        client.SendPlayerCreateObject(ObjectType.CacheCube1);
    }
    public void CreateCylinder() {
        client.SendPlayerCreateObject(ObjectType.CacheCylinder1);
    }
    public void CreateSphere() {
        client.SendPlayerCreateObject(ObjectType.CacheSphere1);
    }
    public void CreateConveyor() {
        client.SendPlayerCreateObject(ObjectType.Conveyor);
    }

    public void DeleteObject() {
        client.SendPlayerEditObjectDestroyed(cursor.selOp.id, ObjectType.Destroyed, new SrTransform(cursor.selOp.transform));
    }

    public void CreateTri() {
        client.SendPlayerCreateObject(ObjectType.CacheTri1);
    }
}
