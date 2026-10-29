using TMPro;
using UnityEngine;

/// <summary>
/// Shows total motion (mm), a short verdict and motion-free time after the scan.
/// Uses the same budget as MotionBar, so bar and verdict always agree.
/// Place on ResultsCanvas.
/// </summary>
public class ResultsScreen : MonoBehaviour
{
    [SerializeField] TMP_Text scoreText;
    [SerializeField] TMP_Text verdictText;
    [SerializeField] TMP_Text detailText;

    public void Show(HeadMotionTracker t, float budgetMm)
    {
        float frac = budgetMm > 0f ? t.TotalMotionMm / budgetMm : 0f;

        scoreText.text = $"{t.TotalMotionMm:F1} mm";
        verdictText.text =
            frac < 0.33f ? "Meget stille – flot!" :
            frac < 0.66f ? "Godt, med lidt bevægelse" :
            frac < 1f    ? "Meget bevægelse" :
                           "For meget bevægelse – billedet kunne blive uskarpt";
        detailText.text = $"Du lå stille {t.MotionFreeFraction:P0} af tiden";
    }
}
