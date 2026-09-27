using System;
using System.Globalization;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mock head-movement warning system for MRI preparation.
/// Calibrates a baseline head pose after a countdown, then warns when the
/// head rotates or translates beyond set thresholds. Logs data to CSV.
/// Independent of the background, so the 360 video can be added later.
/// </summary>
public class HeadMovementMonitor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform head;            // Main Camera (with Tracked Pose Driver)
    [SerializeField] Transform fixationTarget;  // small sphere = the dot to look at
    [SerializeField] Renderer targetRenderer;
    [SerializeField] TMP_Text statusText;       // world-space TextMeshPro text
    [SerializeField] AudioSource warningSound;  // optional

    [Header("Thresholds (placeholders - measure tracking noise first)")]
    [SerializeField] float maxRotationDeg = 3f;
    [SerializeField] float maxTranslationMm = 10f;

    [Header("Setup")]
    [SerializeField] float calibrationDelay = 3f;
    [SerializeField] float targetDistance = 2f;
    [SerializeField] Color okColor = Color.white;
    [SerializeField] Color warnColor = Color.red;
    [SerializeField] bool logToCsv = true;
    [SerializeField] string editorLogFolder = "HeadLogs"; // editor only: relative to project root, or a full path

    enum State { Countdown, Monitoring }
    State state;
    float countdown;
    Quaternion baseRot;
    Vector3 basePos;
    float timeStill, timeTotal;
    bool wasMoving;
    readonly StringBuilder log = new StringBuilder();
    bool logSaved;

    void Start() => Recalibrate();

    public void Recalibrate()
    {
        state = State.Countdown;
        countdown = calibrationDelay;
        timeStill = timeTotal = 0f;
        wasMoving = false;
        log.Clear();
        log.AppendLine("time_s,rotation_deg,translation_mm,moving");
        logSaved = false;
        SetTargetColor(okColor);
    }

    void Update()
    {
        // R in the editor restarts calibration
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Recalibrate();

        if (state == State.Countdown) UpdateCountdown();
        else UpdateMonitoring();
    }

    void UpdateCountdown()
    {
        countdown -= Time.deltaTime;
        SetText($"Get comfortable and look straight ahead\n{Mathf.CeilToInt(countdown)}");
        if (countdown > 0f) return;

        baseRot = head.rotation;
        basePos = head.position;

        // Place the target straight ahead of the calibrated head pose (world-locked)
        fixationTarget.position = basePos + head.forward * targetDistance;
        fixationTarget.LookAt(basePos);
        state = State.Monitoring;
    }

    void UpdateMonitoring()
    {
        float rot = Quaternion.Angle(baseRot, head.rotation);
        float trans = Vector3.Distance(basePos, head.position) * 1000f;
        bool moving = rot > maxRotationDeg || trans > maxTranslationMm;

        timeTotal += Time.deltaTime;
        if (!moving) timeStill += Time.deltaTime;
        float score = timeTotal > 0f ? 100f * timeStill / timeTotal : 100f;

        if (moving && !wasMoving && warningSound != null) warningSound.Play();
        wasMoving = moving;

        SetTargetColor(moving ? warnColor : okColor);
        SetText(moving
            ? "Please keep your head still"
            : $"Keep looking at the dot\nStillness score: {score:0}%");

        if (logToCsv)
            log.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0:F3},{1:F2},{2:F1},{3}", timeTotal, rot, trans, moving ? 1 : 0));
    }

    void SetTargetColor(Color c)
    {
        if (targetRenderer != null) targetRenderer.material.color = c;
    }

    void SetText(string s)
    {
        if (statusText != null) statusText.text = s;
    }

    // Quest apps are often paused rather than quit, so save on both
    void OnApplicationPause(bool paused) { if (paused) SaveLog(); }
    void OnApplicationQuit() => SaveLog();

    // Editor (incl. Quest Link): project folder or custom path. Quest build: persistentDataPath.
    string GetLogFolder()
    {
#if UNITY_EDITOR
        string folder = Path.IsPathRooted(editorLogFolder)
            ? editorLogFolder
            : Path.Combine(Directory.GetParent(Application.dataPath).FullName, editorLogFolder);
#else
        string folder = Application.persistentDataPath;
#endif
        Directory.CreateDirectory(folder);
        return folder;
    }

    void SaveLog()
    {
        if (!logToCsv || logSaved || timeTotal <= 0f) return;
        string path = Path.Combine(GetLogFolder(),
            $"headlog_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        File.WriteAllText(path, log.ToString());
        logSaved = true;
        Debug.Log($"Head movement log saved: {path}");
    }
}
