using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour
{
    public float speed = 5f; // Controls the movement speed of the camera

    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D keys or left/right arrow keys
        float verticalInput = Input.GetAxis("Vertical"); // W/S keys or up/down arrow keys

        // Move the camera in the YX plane based on the input
        Vector3 movement = new Vector3(horizontalInput, verticalInput, 0f) * speed * Time.deltaTime;
        transform.Translate(movement);
    }
}
