using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControler : MonoBehaviour
{
    [SerializeField] float keyboardMovementSpeed;
    [SerializeField] float keyboardZoomSpeed;

    [SerializeField] float touchZoomSpeed;

    [SerializeField] Vector3 lastMousePosition;
    [SerializeField] bool isDragging;
    [SerializeField] float touchMovementSpeed;

    public static float maxZoom;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // if there are two touches on the device
        if (Input.touchCount == 2)
        {
            // Store both touches.
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // find the position in the previous frame of each touch.
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // find the magnitude of the vector (the distance) between the touches in each frame.
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // find the difference in the distances between each frame.
            float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

            // change the orthographic size based on the change in distance between the touches.
            Camera.main.orthographicSize += deltaMagnitudeDiff * touchZoomSpeed;

            // boundaries
            Camera.main.orthographicSize = Mathf.Max(Camera.main.orthographicSize, 3);
            Camera.main.orthographicSize = Mathf.Min(Camera.main.orthographicSize, maxZoom);
        }

        // Keyboard zooming
        if (Input.GetKey(KeyCode.Minus))
        {
            Camera.main.orthographicSize -= Time.deltaTime * keyboardZoomSpeed;
            // boundaries
            Camera.main.orthographicSize = Mathf.Max(Camera.main.orthographicSize, 3);
            Camera.main.orthographicSize = Mathf.Min(Camera.main.orthographicSize, maxZoom);
        }
        else if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKey(KeyCode.Equals))
        {
            Camera.main.orthographicSize += Time.deltaTime * keyboardZoomSpeed;
            // boundaries
            Camera.main.orthographicSize = Mathf.Max(Camera.main.orthographicSize, 3);
            Camera.main.orthographicSize = Mathf.Min(Camera.main.orthographicSize, maxZoom);
        }

        // touch moving
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

        // Keyboard moving
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 movement = keyboardMovementSpeed * Camera.main.orthographicSize * Time.deltaTime * new Vector3(horizontal, vertical, 0);
        transform.position += movement;

        // boundary
        transform.position = new Vector3(Mathf.Min(transform.position.x, maxZoom/2),
                                         Mathf.Min(transform.position.y, maxZoom/2),
                                         transform.position.z);

        transform.position = new Vector3(Mathf.Max(transform.position.x, -maxZoom/2),
                                         Mathf.Max(transform.position.y, -maxZoom/2),
                                         transform.position.z);
    }
}
