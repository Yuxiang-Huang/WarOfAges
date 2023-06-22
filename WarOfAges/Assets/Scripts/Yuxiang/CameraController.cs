using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] float keyboardMovementSpeed;
    [SerializeField] float keyboardZoomSpeed;

    [SerializeField] float touchZoomSpeed;
    [SerializeField] Vector2 prevTouch0Pos;
    [SerializeField] Vector2 prevTouch1Pos;

    [SerializeField] Vector3 prevMousePosition;
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
            // store both touches.
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // first occurrence
            if (!(prevTouch0Pos == Vector2.zero && prevTouch1Pos == Vector2.zero))
            {
                // find the magnitude of the the distance between the touches in each frame.
                float prevTouchDeltaMag = (prevTouch0Pos - prevTouch1Pos).magnitude;
                float touchDeltaMag = (touch0.position - touch1.position).magnitude;

                // find the difference in the distances between each frame.
                float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                // change the orthographic size based on the change in distance between the touches.
                Camera.main.orthographicSize += deltaMagnitudeDiff * touchZoomSpeed;

                // boundaries
                Camera.main.orthographicSize = Mathf.Max(Camera.main.orthographicSize, 3);
                Camera.main.orthographicSize = Mathf.Min(Camera.main.orthographicSize, maxZoom);
            }

            prevTouch0Pos = touch0.position;
            prevTouch1Pos = touch1.position;

            prevMousePosition = Input.mousePosition;
        }
        else
        {
            // reset
            prevTouch0Pos = Vector2.zero;
            prevTouch1Pos = Vector2.zero;
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


        // disable touch moving when zooming
        if (Input.touchCount < 2)
        {
            // touch moving
            if (Input.GetMouseButtonDown(0))
            {
                prevMousePosition = Input.mousePosition;
                isDragging = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
            if (isDragging)
            {
                Vector3 mouseDelta = Input.mousePosition - prevMousePosition;
                prevMousePosition = Input.mousePosition;
                Vector3 moveDelta = Time.deltaTime * touchMovementSpeed * -mouseDelta;
                transform.position += moveDelta;
            }
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
