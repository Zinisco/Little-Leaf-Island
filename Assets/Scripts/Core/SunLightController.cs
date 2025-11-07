using UnityEngine;

public class SunLightController : MonoBehaviour
{
    [Header("Sun Settings")]
    public Light sunLight;
    public Gradient lightColorOverTime;        // Colors throughout 24h
    public AnimationCurve sunIntensityCurve;   // Intensity throughout 24h

    [Header("Rotation Settings")]
    public Transform sunPivot;                 // Assign the Directional Light transform
    public Vector3 rotationAxis = Vector3.right;
    public float rotationOffset = -150f;       // Shift so 8AM looks correct

    void Update()
    {
        if (sunLight == null || TimeManager.I == null || sunPivot == null)
            return;

        float time = TimeManager.I.CurrentTime; // 0..24
        float t = time / 24f;                  // normalized 0..1

        // Rotate around local axis
        float angle = t * 360f + rotationOffset;
        sunPivot.localRotation = Quaternion.AngleAxis(angle, rotationAxis);

        // Apply color + intensity curves
        if (lightColorOverTime != null)
            sunLight.color = lightColorOverTime.Evaluate(t);

        if (sunIntensityCurve != null)
            sunLight.intensity = sunIntensityCurve.Evaluate(t);
    }
}
