using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DioramaOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 focusOffset = new Vector3(0, 0.15f, 0);

    [Header("Orbit")]
    public float yaw = 35f;
    public float pitch = 35f;
    float targetYaw;
    float targetPitch;
    public Vector2 pitchLimits = new Vector2(20f, 60f);
    public float orbitSpeed = 7f;
    public float rotateSensitivity = 140f;
    public float keyboardOrbitSpeed = 50f;

    [Header("Zoom")]
    public float distance = 2.5f;
    float targetDistance;
    public float minDistance = 1.6f;
    public float maxDistance = 4f;
    public float zoomSensitivity = 1f;
    public float zoomSmooth = 12f;

    [Header("Pan")]
    Vector3 focus;
    Vector3 targetFocus;
    public float panSensitivity = 1.2f;
    public float keyboardPanSpeed = 1.4f;
    public float panSmooth = 10f;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        targetYaw = yaw;
        targetPitch = pitch;
        targetDistance = distance;

        if (target != null)
        {
            focus = target.position + focusOffset;
            targetFocus = focus;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        HandleOrbitMouse();
        HandleOrbitKeyboard();
        HandleZoom();
        HandlePanMouse();
        HandlePanKeyboard();
        HandleQuickZoom();

        SmoothApply();
    }

    //-------------------------------------------------------------
    // Controls
    //-------------------------------------------------------------

    void HandleOrbitMouse()
    {
        if (Input.GetMouseButton(1)) // RMB orbit
        {
            targetYaw += Input.GetAxis("Mouse X") * rotateSensitivity * Time.deltaTime;
            targetPitch -= Input.GetAxis("Mouse Y") * rotateSensitivity * 0.5f * Time.deltaTime;
            targetPitch = Mathf.Clamp(targetPitch, pitchLimits.x, pitchLimits.y);
        }
    }

    void HandleOrbitKeyboard()
    {
        if (Input.GetKey(KeyCode.Q))
            targetYaw += keyboardOrbitSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.E))
            targetYaw -= keyboardOrbitSpeed * Time.deltaTime;
    }

    void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f && Input.GetMouseButton(1)) // RMB + Scroll = Zoom
        {
            targetDistance = Mathf.Clamp(
                targetDistance - scroll * zoomSensitivity,
                minDistance,
                maxDistance
            );
        }

        distance = Mathf.Lerp(distance, targetDistance, zoomSmooth * Time.deltaTime);
    }


    void HandlePanMouse()
    {
        if (Input.GetMouseButton(2)) // MMB = Pan
        {
            Vector2 delta = new(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            Vector3 right = transform.right;
            Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;

            targetFocus += (-right * delta.x + -forward * delta.y) * panSensitivity * Time.deltaTime;
        }
    }

    void HandlePanKeyboard()
    {
        Vector3 right = transform.right;
        Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;

        if (Input.GetKey(KeyCode.W))
            targetFocus += forward * keyboardPanSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S))
            targetFocus -= forward * keyboardPanSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A))
            targetFocus -= right * keyboardPanSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D))
            targetFocus += right * keyboardPanSpeed * Time.deltaTime;
    }

    // F = Focus zoom      R = Reset zoom + camera position
    void HandleQuickZoom()
    {
        if (Input.GetKeyDown(KeyCode.F))
            targetDistance = minDistance;

        if (Input.GetKeyDown(KeyCode.R))
        {
            targetDistance = maxDistance;
            targetYaw = yaw;
            targetPitch = pitch;
            targetFocus = target.position + focusOffset;
        }
    }

    //-------------------------------------------------------------
    // Apply smoothing & camera position
    //-------------------------------------------------------------
    void SmoothApply()
    {
        yaw = Mathf.Lerp(yaw, targetYaw, orbitSpeed * Time.deltaTime);
        pitch = Mathf.Lerp(pitch, targetPitch, orbitSpeed * Time.deltaTime);
        focus = Vector3.Lerp(focus, targetFocus, panSmooth * Time.deltaTime);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPos = focus + rotation * new Vector3(0, 0, -distance);

        transform.SetPositionAndRotation(desiredPos, rotation);
    }
}
