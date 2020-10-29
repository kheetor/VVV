using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverviewCam : MonoBehaviour {

    public float speed = 24f;
    public float scrollSpeed = 10f;
    Camera cam;
    void Start() {
        cam = transform.GetComponent<Camera>();
    }

    void Update() {
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize+scrollSpeed*-Input.GetAxis("Mousewheel"),4f,200f);
        transform.position += speed * Time.deltaTime * (
            transform.right * Input.GetAxis("Horizontal") +
            new Vector3(transform.forward.x, 0f, transform.forward.z).normalized * Input.GetAxis("Vertical")
            ).normalized;
    }
}
