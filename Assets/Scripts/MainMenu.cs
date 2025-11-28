using UnityEngine;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PlayerController player;
    [SerializeField] private AircraftPhysics physics;

    [Header("Текстовые поля")]
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI fuelText;
    [SerializeField] private TextMeshProUGUI rollText;
    [SerializeField] private TextMeshProUGUI pitchText;
    [SerializeField] private TextMeshProUGUI yawText;
    [SerializeField] private TextMeshProUGUI aoaText;
    [SerializeField] private TextMeshProUGUI airspeedText;
    [SerializeField] private TextMeshProUGUI altitudeText;

    private AircraftPhysics phys => physics ? physics : player?.GetComponent<AircraftPhysics>();
    private Transform plane => physics ? physics.transform : player?.transform;

    private void Update()
    {
        if (!phys || !plane) return;

        float kmh = phys.Airspeed * 3.6f;
        float fuelPct = phys.FuelLeft / 200f * 100f;

        if (speedText)     speedText.text     = $"Скорость: {(int)kmh} км/ч";
        if (fuelText)      fuelText.text      = $"Топливо: {(int)fuelPct}%";

        Vector3 e = plane.eulerAngles;
        float roll  = e.z > 180f ? e.z - 360f : e.z;
        float pitch = e.x > 180f ? e.x - 360f : e.x;
        float yaw   = e.y > 180f ? e.y - 360f : e.y;

        if (rollText)  rollText.text  = $"Крен: {roll:+000;-000}°";
        if (pitchText) pitchText.text = $"Тангаж: {pitch:+000;-000}°";
        if (yawText)   yawText.text   = $"Рыскание: {yaw:+000;-000}°";

        if (aoaText)      aoaText.text      = $"AoA: {phys.AngleOfAttack:+00.0;-00.0}°";
        if (airspeedText) airspeedText.text = $"Воздушная скорость: {(int)phys.Airspeed} м/с";
        if (altitudeText) altitudeText.text = $"Высота: {(int)phys.Altitude} м";
    }
}