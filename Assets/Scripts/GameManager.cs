using UnityEngine;

/// <summary>
/// Controls the app flow: Intro -> Setup (duration) -> Video (A, B loop, C) -> Results.
/// Place on an empty GameObject "Managers" together with HeadMotionTracker.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject introPanel;
    [SerializeField] GameObject setupPanel;
    [SerializeField] GameObject resultsPanel;

    [Header("Components")]
    [SerializeField] VideoSequencer video;
    [SerializeField] HeadMotionTracker tracker;
    [SerializeField] MotionBar motionBar;
    [SerializeField] ResultsScreen results;

    public float ScanMinutes { get; private set; } = 5f;

    void Start()
    {
        motionBar.gameObject.SetActive(false);
        ShowOnly(introPanel);
    }

    // ---- Called from buttons (Button > On Click) ----

    public void OnIntroStart() => ShowOnly(setupPanel);

    public void OnDurationChosen(float minutes)
    {
        ScanMinutes = minutes;
        ShowOnly(null);
        video.Play(minutes * 60f);
    }

    public void OnRestart() => ShowOnly(introPanel);

    // ---- Called from VideoSequencer ----

    public void OnInsideStarted()
    {
        tracker.StartRecording();
        motionBar.Begin(ScanMinutes);
    }

    public void OnInsideEnded()
    {
        tracker.StopRecording();
        motionBar.gameObject.SetActive(false);
    }

    public void OnVideoFinished()
    {
        ShowOnly(resultsPanel);
        results.Show(tracker, motionBar.MaxMm);
    }

    void ShowOnly(GameObject panel)
    {
        introPanel.SetActive(panel == introPanel);
        setupPanel.SetActive(panel == setupPanel);
        resultsPanel.SetActive(panel == resultsPanel);
    }
}
