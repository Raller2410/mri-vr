using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Measures head movement from the VR headset pose at a fixed sample rate.
/// Attach to any GameObject and assign the Main Camera (under XR Origin) as "head".
///
/// Per sample i (all in mm):
///   dTrans_i = |p_i - p_(i-1)|                         (Vector3.Distance)
///   dRot_i   = r * theta_i,  theta_i in radians         (Quaternion.Angle)
///   dM_i     = sqrt(dTrans_i^2 + dRot_i^2)
///   M_total  = sum(dM_i)
///
/// Also tracks displacement from a reference pose (taken at StartRecording),
/// which is what the dartboard shows and what "motion-free time" is based on.
/// Works with HeadSimulator.cs in the editor (no headset needed).
/// </summary>
public class HeadMotionTracker : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Main Camera under XR Origin. Local pose = tracking space, unaffected by moving the rig.")]
    [SerializeField] Transform head;

    [Header("Sampling")]
    [SerializeField, Range(5f, 90f)] float sampleRateHz = 30f;
    [SerializeField] bool startOnAwake = false;

    [Header("Metric")]
    [Tooltip("Radius used to convert rotation to arc length (mm). 57 mm per the notes.")]
    [SerializeField] float rotationRadiusMm = 57f;
    [Tooltip("Displacement from reference pose (mm) counted as 'still'.")]
    [SerializeField] float stillThresholdMm = 0.5f;

    [Header("Logging")]
    [SerializeField] bool writeCsv = true;

    // Results, readable by UI / dartboard
    public bool IsRecording { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public float TotalTranslationMm { get; private set; }
    public float TotalRotationDeg { get; private set; }
    public float TotalMotionMm { get; private set; }
    public float LastStepMm { get; private set; }            // dM_i of the latest sample
    public float CurrentDisplacementMm { get; private set; } // vs. reference pose
    public Vector2 CurrentOffsetMm { get; private set; }     // 2D offset for dartboard (x = right, y = up)
    public float MotionFreeFraction => sampleCount > 0 ? (float)stillCount / sampleCount : 1f;
    public string LastCsvPath { get; private set; }

    Vector3 prevPos, refPos;
    Quaternion prevRot, refRot;
    float timer;
    int sampleCount, stillCount;
    StringBuilder csv;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture; // avoid Danish decimal commas in CSV

    void Awake()
    {
        if (head == null && Camera.main != null) head = Camera.main.transform;
    }

    void Start()
    {
        if (startOnAwake) StartRecording();
    }

    /// <summary>Call when the "scan" starts. Current pose becomes the reference (dartboard centre).</summary>
    public void StartRecording()
    {
        refPos = prevPos = head.localPosition;
        refRot = prevRot = head.localRotation;
        timer = 0f;
        ElapsedSeconds = 0f;
        TotalTranslationMm = TotalRotationDeg = TotalMotionMm = 0f;
        LastStepMm = CurrentDisplacementMm = 0f;
        CurrentOffsetMm = Vector2.zero;
        sampleCount = stillCount = 0;

        if (writeCsv)
        {
            csv = new StringBuilder();
            csv.AppendLine("t_s,x_mm,y_mm,z_mm,qx,qy,qz,qw,dTrans_mm,dRot_deg,dMotion_mm,displacement_mm");
        }
        IsRecording = true;
    }

    /// <summary>Re-centre the dartboard without resetting the totals.</summary>
    public void Recenter()
    {
        refPos = head.localPosition;
        refRot = head.localRotation;
    }

    public void StopRecording()
    {
        if (!IsRecording) return;
        IsRecording = false;

        if (writeCsv && csv != null)
        {
            string file = $"headmotion_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
            LastCsvPath = Path.Combine(Application.persistentDataPath, file);
            File.WriteAllText(LastCsvPath, csv.ToString());
            Debug.Log($"[HeadMotion] Saved {sampleCount} samples to {LastCsvPath}");
        }

        Debug.Log(string.Format(Inv,
            "[HeadMotion] {0:F1} s | translation {1:F2} mm | rotation {2:F2}° | total {3:F2} mm | motion-free {4:P0}",
            ElapsedSeconds, TotalTranslationMm, TotalRotationDeg, TotalMotionMm, MotionFreeFraction));
    }

    void Update()
    {
        if (!IsRecording || head == null) return;

        ElapsedSeconds += Time.unscaledDeltaTime;
        timer += Time.unscaledDeltaTime;

        float interval = 1f / sampleRateHz;
        if (timer < interval) return;
        timer -= interval;
        if (timer > interval) timer = 0f; // after a frame drop: don't take several samples of the same pose

        Sample();
    }

    void Sample()
    {
        Vector3 pos = head.localPosition;
        Quaternion rot = head.localRotation;

        // --- Frame-to-frame movement (the formulas in the notes) ---
        float dTransMm = Vector3.Distance(prevPos, pos) * 1000f;  // Unity units are metres
        float dThetaDeg = Quaternion.Angle(prevRot, rot);
        float dRotMm = rotationRadiusMm * dThetaDeg * Mathf.Deg2Rad; // s = r * theta (radians)
        float dMotionMm = Mathf.Sqrt(dTransMm * dTransMm + dRotMm * dRotMm);

        TotalTranslationMm += dTransMm;
        TotalRotationDeg += dThetaDeg;
        TotalMotionMm += dMotionMm;
        LastStepMm = dMotionMm;

        // --- Displacement from reference pose (for feedback + motion-free time) ---
        float refTransMm = Vector3.Distance(refPos, pos) * 1000f;
        float refRotMm = rotationRadiusMm * Quaternion.Angle(refRot, rot) * Mathf.Deg2Rad;
        CurrentDisplacementMm = Mathf.Sqrt(refTransMm * refTransMm + refRotMm * refRotMm);

        sampleCount++;
        if (CurrentDisplacementMm <= stillThresholdMm) stillCount++;

        // 2D offset in the reference frame: sideways/up translation + yaw/pitch as arc length
        Vector3 localDelta = Quaternion.Inverse(refRot) * (pos - refPos) * 1000f;
        Vector3 e = (Quaternion.Inverse(refRot) * rot).eulerAngles;
        float yaw = Mathf.DeltaAngle(0f, e.y);
        float pitch = Mathf.DeltaAngle(0f, e.x); // positive = looking down in Unity
        CurrentOffsetMm = new Vector2(
            localDelta.x + rotationRadiusMm * yaw * Mathf.Deg2Rad,
            localDelta.y - rotationRadiusMm * pitch * Mathf.Deg2Rad);

        if (csv != null)
        {
            csv.AppendLine(string.Format(Inv,
                "{0:F4},{1:F3},{2:F3},{3:F3},{4:F6},{5:F6},{6:F6},{7:F6},{8:F4},{9:F4},{10:F4},{11:F4}",
                ElapsedSeconds, pos.x * 1000f, pos.y * 1000f, pos.z * 1000f,
                rot.x, rot.y, rot.z, rot.w, dTransMm, dThetaDeg, dMotionMm, CurrentDisplacementMm));
        }

        prevPos = pos;
        prevRot = rot;
    }

    void OnDisable() => StopRecording();
}
