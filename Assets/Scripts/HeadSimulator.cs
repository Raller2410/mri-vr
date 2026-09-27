using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

/// <summary>
/// Simulates head movement with mouse and keyboard when no VR headset is active.
/// Attach to the Main Camera. Disables itself automatically when a headset is connected.
///
/// Controls:
///   Right mouse button + drag : look around (rotation)
///   W/A/S/D/Q/E               : move head forward/left/back/right/down/up
///   Hold Left Shift           : fine movement (10x slower, for mm-scale tests)
///   Space                     : sudden head twitch that returns to rest
/// </summary>
public class HeadSimulator : MonoBehaviour
{
    [SerializeField] float lookSensitivity = 0.1f; // degrees per pixel of mouse movement
    [SerializeField] float moveSpeed = 0.05f;      // meters per second (50 mm/s)
    [SerializeField] float twitchDegrees = 6f;
    [SerializeField] float twitchRecovery = 4f;    // higher = faster return to rest

    float yaw, pitch, twitch;

    void Start()
    {
        if (XRSettings.isDeviceActive)
        {
            enabled = false; // real headset drives the camera
            return;
        }

        var poseDriver = GetComponent<TrackedPoseDriver>();
        if (poseDriver != null) poseDriver.enabled = false;

        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x > 180f ? e.x - 360f : e.x;
    }

    void Update()
    {
        var mouse = Mouse.current;
        var kb = Keyboard.current;

        if (mouse != null && mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue() * lookSensitivity;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
        }

        if (kb != null)
        {
            Vector3 dir = Vector3.zero;
            if (kb.wKey.isPressed) dir += Vector3.forward;
            if (kb.sKey.isPressed) dir += Vector3.back;
            if (kb.aKey.isPressed) dir += Vector3.left;
            if (kb.dKey.isPressed) dir += Vector3.right;
            if (kb.eKey.isPressed) dir += Vector3.up;
            if (kb.qKey.isPressed) dir += Vector3.down;

            float speed = kb.leftShiftKey.isPressed ? moveSpeed * 0.1f : moveSpeed;
            transform.position += Quaternion.Euler(0f, yaw, 0f) * dir * speed * Time.deltaTime;

            if (kb.spaceKey.wasPressedThisFrame)
                twitch += Random.value < 0.5f ? -twitchDegrees : twitchDegrees;
        }

        twitch = Mathf.Lerp(twitch, 0f, twitchRecovery * Time.deltaTime);
        transform.rotation = Quaternion.Euler(pitch, yaw + twitch, 0f);
    }
}
