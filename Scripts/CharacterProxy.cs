using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterProxy : MonoBehaviour {

    public ServerCharacterData data;
    public Vector3 vect;
    public Transform ProxyOb;

    public Transform nameplate;

    public void Update() {
        nameplate.LookAt(Camera.main.transform, Vector3.up);
    }
}

