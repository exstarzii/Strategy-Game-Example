using UnityEngine;

public class CameraController3rd : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float rotationSpeed = 100f;
    public float rotationX = 70f;
    public float scrollSpeed = 10f;
    public float minHeight = 5f;
    public float maxHeight = 50f;

    private float tmpHeight;
    private float height;

    void Start()
    {
        height = transform.position.y;
        tmpHeight = height;
        transform.rotation = Quaternion.Euler(rotationX, transform.rotation.eulerAngles.y, 0);
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 forward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 right = new Vector3(transform.right.x, 0f, transform.right.z).normalized;

        Vector3 direction = (right * moveX + forward * moveZ).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        float rotation = 0f;

        if (Input.GetKey(KeyCode.Q)) rotation -= 1f;
        if (Input.GetKey(KeyCode.E)) rotation += 1f;

        transform.Rotate(Vector3.up, rotation * rotationSpeed * Time.deltaTime, Space.World);

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        tmpHeight -= scroll * scrollSpeed;
        tmpHeight = Mathf.Clamp(tmpHeight, minHeight, maxHeight);
        height = Mathf.Lerp(height, tmpHeight, 3 * Time.deltaTime);

        Vector3 pos = transform.position;
        pos.y = height;
        transform.position = pos;
    }
}
