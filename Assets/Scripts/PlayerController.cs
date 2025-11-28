using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    #region References
    [Header("Ссылки")]
    [SerializeField] private AircraftPhysics flightPhysics;
    #endregion

    #region Torque Settings
    [Header("Управление")]
    [SerializeField] private float rollPower = 8f;
    [SerializeField] private float pitchPower = 5f;
    [SerializeField] private float yawPower = 5f;
    [SerializeField] private float mouseSens = 4f;
    #endregion

    #region Speed display
    [Header("Скорость (для HUD)")]
    [SerializeField] private float baseSpeed = 15f;
    [SerializeField] private float boostedSpeed = 25f;
    [SerializeField] private float speedChangeRate = 2f;
    #endregion

    private Rigidbody rb;
    private float mouseHorizontal;
    private float currentDisplayedSpeed;

    public bool isBoosting { get; private set; }

    public float currentSpeed => currentDisplayedSpeed;
    public float boostSpeed => boostedSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        currentDisplayedSpeed = baseSpeed;
    }

    private void Update()
    {
        mouseHorizontal = Input.GetAxis("Mouse X") * mouseSens;
        isBoosting = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void FixedUpdate()
    {
        if (flightPhysics == null || flightPhysics.Airspeed < 2f)
            rb.linearVelocity = transform.forward * currentDisplayedSpeed;

        float rollInput  = -mouseHorizontal;
        float pitchInput = Input.GetAxis("Vertical");
        float yawInput   = Input.GetAxis("Horizontal");

        if (Input.GetKey(KeyCode.Q)) rollInput += 1f;
        if (Input.GetKey(KeyCode.E)) rollInput -= 1f;

        ApplyRotationTorque(rollInput, pitchInput, yawInput);
        SmoothDisplayedSpeed();
    }

    private void ApplyRotationTorque(float roll, float pitch, float yaw)
    {
        float multiplier = isBoosting ? 1.5f : 1f;

        if (Mathf.Abs(yaw)   > 0.01f) rb.AddTorque(transform.up      * yaw   * yawPower   * multiplier, ForceMode.Acceleration);
        if (Mathf.Abs(pitch) > 0.01f) rb.AddTorque(transform.right   * -pitch * pitchPower * multiplier, ForceMode.Acceleration);
        if (Mathf.Abs(roll)  > 0.01f) rb.AddTorque(transform.forward * roll  * rollPower  * multiplier, ForceMode.Acceleration);

        if (Mathf.Abs(yaw) > 0.1f || Mathf.Abs(pitch) > 0.1f || Mathf.Abs(roll) > 0.1f)
            rb.angularVelocity *= 0.99f;
        else if (!isBoosting)
            rb.angularVelocity *= 0.98f;
    }

    private void SmoothDisplayedSpeed()
    {
        float target = isBoosting ? boostedSpeed : baseSpeed;
        currentDisplayedSpeed = Mathf.Lerp(currentDisplayedSpeed, target, speedChangeRate * Time.deltaTime);
    }
    public Vector3 GetCurrentRotation()
    {
        Vector3 euler = transform.eulerAngles;
        return new Vector3(
            euler.x > 180f ? euler.x - 360f : euler.x,
            euler.y > 180f ? euler.y - 360f : euler.y,
            euler.z > 180f ? euler.z - 360f : euler.z
        );
    }
}