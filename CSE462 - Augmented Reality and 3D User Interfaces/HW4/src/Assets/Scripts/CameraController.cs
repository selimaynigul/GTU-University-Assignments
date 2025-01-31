using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float speed = 50.0f;
    public float lookSpeed = 4.0f;

    void Update()
    {
        // Movement
        float moveX = Input.GetAxis("Horizontal") * speed * Time.deltaTime; // A/D
        float moveZ = Input.GetAxis("Vertical") * speed * Time.deltaTime;   // W/S
        float moveY = 0;

        if (Input.GetKey(KeyCode.E)) moveY = speed * Time.deltaTime;       // Up
        if (Input.GetKey(KeyCode.Q)) moveY = -speed * Time.deltaTime;      // Down

        transform.Translate(moveX, moveY, moveZ);

        // Looking around
        if (Input.GetMouseButton(1)) 
        {
            float rotX = Input.GetAxis("Mouse X") * lookSpeed;
            float rotY = -Input.GetAxis("Mouse Y") * lookSpeed;

            transform.Rotate(0, rotX, 0, Space.World);
            transform.Rotate(rotY, 0, 0, Space.Self);
        }
    }
}
