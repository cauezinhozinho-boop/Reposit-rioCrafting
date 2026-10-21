using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 offset = new Vector3(10f, 10f, -10f);

    [Header("Follow")]
    public float smoothSpeed = 5f;

    void Start()
    {
        // Ângulo isométrico clássico
        transform.rotation = Quaternion.Euler(30f, -49.418f, 0f);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}