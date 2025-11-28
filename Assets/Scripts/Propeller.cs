using UnityEngine;

public class Propeller : MonoBehaviour
{
    [SerializeField] private float spinSpeed = 720f; // градусов в секунду

    void FixedUpdate()
    {
        transform.Rotate(0f, 0f, spinSpeed * Time.fixedDeltaTime);
    }
}