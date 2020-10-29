using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PropertiesPanel : MonoBehaviour {

    public ClientUI ui;

    public float[] cacheValues;
    public InputField[] enterValues;
    public Text[] printValues;

    public InputField hue;
    public InputField sat;
    public InputField val;

    public GameObject posInfo;
    public GameObject rotInfo;
    public GameObject scaleInfo;

    public RectTransform ColorSample;

    public bool CopyToTransform(Transform t) {
        float[] vals = new float[9];
        bool changed = false;
        for (int i = 0; i < 9; i++) {
            float newVal;
            if (float.TryParse(enterValues[i].text, out newVal)) {
                vals[i] = newVal;
            }
            else {
                vals[i] = cacheValues[i];
            }
            if (Mathf.Abs(vals[i] - cacheValues[i]) > 0.001f) {
                changed = true;
            }
        }
        t.position = new Vector3(vals[0], vals[1], vals[2]);
        t.eulerAngles = new Vector3(vals[3], vals[4], vals[5]);
        t.localScale = new Vector3(vals[6], vals[7], vals[8]);

        return changed;
    }

    public void ShowTransform(bool mode) {
        posInfo.SetActive(mode);
        rotInfo.SetActive(mode);
        scaleInfo.SetActive(mode);
    }

    public void GetColor() {
        hue.text = ui.cursor.selOp.objectProxyData.col[0].ToString();
        sat.text = ui.cursor.selOp.objectProxyData.col[1].ToString();
        val.text = ui.cursor.selOp.objectProxyData.col[2].ToString();
    }

    public IEnumerator SampleColor() {
        Debug.Log(ColorSample.rect.min.ToString());
        while (Input.GetButton("Fire1")) {
            yield return null;
        }
    }

    public void StartSampleColor() {
        Vector2 point = ColorSample.InverseTransformPoint(Input.mousePosition) + new Vector3(-3, 8, 0);
        ui.cursor.selOp.objectProxyData.col[0] = Mathf.Clamp(Mathf.RoundToInt(100*point.x / 210f), 0, 100);
        ui.cursor.selOp.objectProxyData.col[2] = Mathf.Clamp(Mathf.RoundToInt(100-100*point.y/-24f), 0, 100);
        float newFloat;
        if (float.TryParse(sat.text, out newFloat)) {
            ui.cursor.selOp.objectProxyData.col[1] = (byte)Mathf.Clamp(Mathf.RoundToInt(newFloat), 0, 100);
            ui.cursor.SendUpdateSelectedColor();
        }

        GetColor();

        //StartCoroutine(SampleColor());
    }

    public bool SetColor() {

        bool updated = false;
        float newFloat;
        if (float.TryParse(hue.text, out newFloat)) {
            ui.cursor.selOp.objectProxyData.col[0] = (byte)Mathf.Clamp(Mathf.RoundToInt(newFloat), 0, 100);
            updated = true;
        }
        if (float.TryParse(sat.text, out newFloat)) {
            ui.cursor.selOp.objectProxyData.col[1] = (byte)Mathf.Clamp(Mathf.RoundToInt(newFloat), 0, 100);
            updated = true;
        }
        if (float.TryParse(val.text, out newFloat)) {
            ui.cursor.selOp.objectProxyData.col[2] = (byte)Mathf.Clamp(Mathf.RoundToInt(newFloat), 0, 100);
            updated = true;
        }
        if (updated) {
            return true;
        }

        return false;
    }

    public void Apply() {
        if (ui.cursor.CanEditSelection()){
            if (CopyToTransform(ui.cursor.selected.transform)) {
                ui.cursor.SendUpdateSelectedTransform();
            }
            else if (SetColor()) {
                ui.cursor.SendUpdateSelectedColor();
            }
        }
    }

    public void CopyFromTransform(Transform t) {
        cacheValues[0] = t.position.x;
        cacheValues[1] = t.position.y;
        cacheValues[2] = t.position.z;

        cacheValues[3] = t.eulerAngles.x;
        cacheValues[4] = t.eulerAngles.y;
        cacheValues[5] = t.eulerAngles.z;

        cacheValues[6] = t.localScale.x;
        cacheValues[7] = t.localScale.y;
        cacheValues[8] = t.localScale.z;

        for (int i = 0; i < 9; i++) {
            enterValues[i].text = cacheValues[i].ToString();
            printValues[i].text = enterValues[i].text;
        }
    }
}
