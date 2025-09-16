using UnityEngine;

public class RotateY : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 60f; // degrees per second

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.Self);
    }
}
