using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bar in the top-right corner that fills 0 -> full and goes green -> red
/// as total head motion (HeadMotionTracker.TotalMotionMm) uses up the motion budget.
/// Place on MotionBarCanvas (World Space canvas, child of Main Camera).
/// </summary>
public class MotionBar : MonoBehaviour
{
    [SerializeField] HeadMotionTracker tracker;
    [Tooltip("Image with Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left.")]
    [SerializeField] Image fill;
    [Tooltip("Green (0) -> yellow (0.5) -> red (1).")]
    [SerializeField] Gradient colors;
    [Tooltip("Motion budget in mm per minute. Placeholder: calibrate with tests.")]
    [SerializeField] float budgetMmPerMinute = 10f;

    public float MaxMm { get; private set; } = 1f;

    public void Begin(float minutes)
    {
        MaxMm = Mathf.Max(0.01f, budgetMmPerMinute * minutes);
        fill.fillAmount = 0f;
        fill.color = colors.Evaluate(0f);
        gameObject.SetActive(true);
    }

    void Update()
    {
        float t = Mathf.Clamp01(tracker.TotalMotionMm / MaxMm);
        fill.fillAmount = t;
        fill.color = colors.Evaluate(t);
    }
}
