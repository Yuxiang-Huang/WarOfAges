using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControler : MonoBehaviour
{
    [SerializeField] float keyboardMovementSpeed = 10f;
    [SerializeField] float keyboardZoomSpeed = 2f;

    [SerializeField] float touchZoomSpeed = 0.5f;

    [SerializeField] Vector3 lastMousePosition;
    [SerializeField] bool isDragging;
    [SerializeField] float touchMovementSpeed = 2f;

    public static float maxZoom;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = keyboardMovementSpeed * Time.deltaTime * new Vector3(horizontal, vertical, 0);

        transform.position += movement;

        if (Input.GetKey(KeyCode.Minus))
        {
            Camera.main.orthographicSize += Time.deltaTime * keyboardZoomSpeed;
            // boundaries
            Camera.main.orthographicSize = Mathf.Max(Camera.main.orthographicSize, 3);
            Camera.main.orthographicSize = Mathf.Min(Camera.main.orthographicSize, maxZoom);
        }
        else if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKey(KeyCode.Equals))
        {
            Camera.main.orthographicSize -= Time.deltaTime * keyboardZoomSpeed;
            // boundaries
            Camera.main.orthographicSize = Mathf.Max(Camera.main.orthographicSize, 3);
            Camera.main.orthographicSize = Mathf.Min(Camera.main.orthographicSize, maxZoom);
        }

        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;
            Vector3 moveDelta = Time.deltaTime * touchMovementSpeed * -mouseDelta;
            transform.position += moveDelta;
        }

        // if there are two touches on the device
        if (Input.touchCount == 2)
        {
            // Store both touches.
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Find the position in the previous frame of each touch.
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Find the magnitude of the vector (the distance) between the touches in each frame.
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // Find the difference in the distances between each frame.
            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            // ... change the orthographic size based on the change in distance between the touches.
            Camera.main.orthographicSize += deltaMagnitudeDiff * touchZoomSpeed;
        }
    }
}
