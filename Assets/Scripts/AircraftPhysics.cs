using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AircraftPhysics : MonoBehaviour
{
    #region Wing & Mass
    [Header("Wing & Mass Settings")]
    [SerializeField] private float wingArea = 15f;
    [SerializeField] private float wingSpan = 10f;
    [SerializeField] private float chordLength = 2.5f;
    [SerializeField] private float emptyWeight = 800f;
    [SerializeField] private float fuelCapacity = 200f;
    [SerializeField] private float fuelDensity = 0.8f;
    #endregion

    #region Aerodynamics
    [Header("Aerodynamic Curves")]
    [SerializeField] private AnimationCurve liftCurve;
    [SerializeField] private AnimationCurve dragCurve;
    [SerializeField] private float stallAngle = 12f;
    #endregion

    #region Engine
    [Header("Engine Parameters")]
    [SerializeField] private float maxThrust = 3000f;
    [SerializeField] private float altitudeForHalfThrust = 5000f;
    #endregion

    #region Environment
    [Header("Wind & Turbulence")]
    [SerializeField] private float rhoSeaLevel = 1.225f;
    [SerializeField] private Vector3 windVelocity = Vector3.forward * 5f;
    [SerializeField] private float turbulencePower = 0.1f;
    [SerializeField] private float turbulenceSpeed = 0.5f;
    #endregion


    private Rigidbody rb;
    private PlayerController input;

    private float fuelRemaining;
    private float throttleLevel;
    private float currentAoA;
    private float currentAirspeed;
    private bool inStall;
    private float timeInStall;

    #region Unity callbacks
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<PlayerController>();
        fuelRemaining = fuelCapacity;

        if (liftCurve.keys.Length == 0)
            SetupDefaultCurves();

        UpdateMass();
    }

    private void FixedUpdate()
    {
        CalculateCurrentState();
        ApplyLiftAndDrag();
        ApplyEngineThrust();
        ApplyWindAndTurbulence();
        HandleStallBehaviour();
        ConsumeFuelAndUpdateMass();
    }
    #endregion

    #region Curves initialization
    private void SetupDefaultCurves()
    {
        liftCurve = new AnimationCurve(
            new Keyframe(-20f, -0.5f), new Keyframe(-5f, 0f),
            new Keyframe(5f, 0.5f),    new Keyframe(15f, 1.2f),
            new Keyframe(20f, 0.8f),   new Keyframe(25f, 0.5f)
        );

        dragCurve = new AnimationCurve(
            new Keyframe(-20f, 0.8f), new Keyframe(0f, 0.05f),
            new Keyframe(15f, 0.1f),  new Keyframe(20f, 0.3f),
            new Keyframe(25f, 0.5f)
        );
    }
    #endregion

    #region Flight state
    private void CalculateCurrentState()
    {
        Vector3 relativeAir = rb.linearVelocity - windVelocity;
        currentAirspeed = relativeAir.magnitude;

        if (currentAirspeed > 0.1f)
        {
            Vector3 localVel = transform.InverseTransformDirection(relativeAir);
            currentAoA = Mathf.Atan2(-localVel.y, localVel.z) * Mathf.Rad2Deg;
        }
        else
            currentAoA = 0f;
    }
    #endregion

    #region Aerodynamics
    private void ApplyLiftAndDrag()
    {
        if (currentAirspeed < 0.1f) return;

        float density = rhoSeaLevel * Mathf.Exp(-transform.position.y / 8000f);
        float dynamicPressure = 0.5f * density * currentAirspeed * currentAirspeed;
        Vector3 airflow = rb.linearVelocity - windVelocity;

        float Cl = liftCurve.Evaluate(currentAoA);
        float Cd = dragCurve.Evaluate(Mathf.Abs(currentAoA));

        if (inStall)
        {
            Cl *= 0.3f;
            Cd *= 2f;
        }

        Vector3 liftDirection = Vector3.Cross(airflow.normalized, transform.right).normalized;
        rb.AddForce(liftDirection * dynamicPressure * wingArea * Cl);
        rb.AddForce(-airflow.normalized * dynamicPressure * wingArea * Cd);
    }
    #endregion

    #region Engine
    private void ApplyEngineThrust()
    {
        throttleLevel = input.isBoosting ? 1f : 0.5f;

        if (throttleLevel > 0f && fuelRemaining > 0f)
        {
            float altitudeFactor = Mathf.Exp(-transform.position.y / altitudeForHalfThrust);
            float thrust = throttleLevel * maxThrust * altitudeFactor;
            rb.AddForce(transform.forward * thrust);
        }
    }
    #endregion

    #region Environment effects
    private void ApplyWindAndTurbulence()
    {
        rb.AddForce(windVelocity * rb.mass * 0.1f);

        if (turbulencePower > 0f)
        {
            Vector3 turb = turbulencePower * currentAirspeed * new Vector3(
                Mathf.PerlinNoise(Time.time * turbulenceSpeed, 0f) - 0.5f,
                Mathf.PerlinNoise(0f, Time.time * turbulenceSpeed) - 0.5f,
                Mathf.PerlinNoise(Time.time * turbulenceSpeed, Time.time * turbulenceSpeed) - 0.5f
            );

            rb.AddForce(turb);
        }
    }
    #endregion

    #region Stall logic
    private void HandleStallBehaviour()
    {
        if (!inStall && Mathf.Abs(currentAoA) > stallAngle)
        {
            inStall = true;
            timeInStall = 0f;
        }

        if (inStall)
        {
            timeInStall += Time.fixedDeltaTime;

            if (timeInStall < 3f)
            {
                rb.AddForce(turbulencePower * 2f * Random.insideUnitSphere * rb.mass);
                rb.AddTorque(Random.insideUnitSphere * 0.1f);
            }

            if (Mathf.Abs(currentAoA) < stallAngle * 0.7f && timeInStall > 2f)
                inStall = false;
        }
    }
    #endregion

    #region Fuel & mass
    private void ConsumeFuelAndUpdateMass()
    {
        if (throttleLevel > 0f && fuelRemaining > 0f)
        {
            fuelRemaining -= throttleLevel * 2f * Time.fixedDeltaTime;
            fuelRemaining = Mathf.Clamp(fuelRemaining, 0f, fuelCapacity);
            UpdateMass();
        }
    }

    private void UpdateMass()
    {
        rb.mass = emptyWeight + fuelRemaining * fuelDensity;
    }
    #endregion

    #region Public getters for HUD
    public float Airspeed => currentAirspeed;
    public float Altitude => transform.position.y;
    public float AngleOfAttack => currentAoA;
    public float FuelLeft => fuelRemaining;
    public bool IsStalled => inStall;
    #endregion
}