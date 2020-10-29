using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterView : MonoBehaviour {

    public float lerpPos = 0.5f;
    public float lerpRot = 0.5f;
    public float lerpScale = 0.5f;

    public List<CharacterProxy> chars = new List<CharacterProxy>();

    public GameObject charTemplate;

    public CharacterProxy GetCharacterProxy(int id) {
        for (int i = 0; i < chars.Count; i++) {
            if(chars[i].data.id == id) {
                return chars[i];
            }
        }
        return null;
    }

    CharacterProxy AddCharacter(ServerCharacterData cd) {
        GameObject go = Instantiate(charTemplate, transform);
        CharacterProxy cp = go.GetComponent<CharacterProxy>();
        cp.data = cd;
        chars.Add(cp);

        go.name = cd.name;
        cp.ProxyOb = cp.transform.GetChild(0);
        cp.ProxyOb.name = "Proxy " + cd.name;
        cp.ProxyOb.SetParent(transform);

        TextMesh tm = cp.ProxyOb.GetComponentInChildren<TextMesh>();
        tm.text = cd.name;

        return cp;
    }

    public void ReceiveCharacterData(ServerCharacterData cd) {
        CharacterProxy cp = GetCharacterProxy(cd.id);
        if (!cp) {
            cp = AddCharacter(cd);
            cd.t.CopyToTransform(cp.ProxyOb);
        }
        cd.t.CopyToTransform(cp.transform);
        cp.vect = Data.DesrVector(cd.vect, 10f);
    }

    void InterpLoop() {
        for (int i = 0; i < chars.Count; i++) {
            chars[i].ProxyOb.position = Vector3.Lerp(chars[i].ProxyOb.position, chars[i].transform.position, lerpPos);
            chars[i].ProxyOb.localEulerAngles = Vector3.Lerp(chars[i].ProxyOb.localEulerAngles, chars[i].transform.localEulerAngles, lerpRot);
            chars[i].ProxyOb.localScale = Vector3.Lerp(chars[i].ProxyOb.localScale, chars[i].transform.localScale, lerpScale);
            //chars[i].transform.position += chars[i].vect*Time.deltaTime;
        }
    }

    void Start() {

    }

    void Update() {
        InterpLoop();
    }
}
