using UnityEngine;

public class CamControl : MonoBehaviour
{
    public float speedPerSecond = 1.0f;
    public float rotateSpeed = 10.0f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private Vector2 lastMousePosition;
    
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * (speedPerSecond * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.forward * (speedPerSecond * Time.deltaTime);
        }
        
        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * (speedPerSecond * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * (speedPerSecond * Time.deltaTime);
        }

        Vector2 delta = Input.mousePositionDelta;

        transform.RotateAround(Vector3.up, delta.x * rotateSpeed);
        transform.RotateAround(transform.right, -delta.y * rotateSpeed);
    }
}
