using System.Collections;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Plays A_intro once, loops B_inside for the chosen duration, then plays C_outro once.
/// Place on the "Video Player" GameObject (same object as the VideoPlayer).
/// VideoPlayer: Render Mode = Render Texture (shown on the Skybox/Panoramic material, as today).
/// </summary>
public class VideoSequencer : MonoBehaviour
{
    [SerializeField] VideoPlayer player;
    [SerializeField] VideoClip intro, inside, outro;
    [Tooltip("Scanner sound, Loop = true, Play On Awake = false. Optional.")]
    [SerializeField] AudioSource scannerAudio;
    [SerializeField] GameManager gm;

    Coroutine running;

    void Reset() => player = GetComponent<VideoPlayer>();

    void Awake() => player.playOnAwake = false;

    public void Play(float insideSeconds)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Run(insideSeconds));
    }

    IEnumerator Run(float insideSeconds)
    {
        // A: into the scanner
        yield return PlayClip(intro, false);
        yield return WaitForEnd();

        // B: inside the bore, loops for the chosen time while motion is measured
        yield return PlayClip(inside, true);
        if (scannerAudio != null) scannerAudio.Play();
        gm.OnInsideStarted();
        yield return new WaitForSeconds(insideSeconds);
        if (scannerAudio != null) scannerAudio.Stop();
        gm.OnInsideEnded();

        // C: out of the scanner
        yield return PlayClip(outro, false);
        yield return WaitForEnd();

        player.Stop();
        running = null;
        gm.OnVideoFinished();
    }

    IEnumerator PlayClip(VideoClip clip, bool loop)
    {
        player.Stop();
        player.clip = clip;
        player.isLooping = loop;
        player.Prepare();
        while (!player.isPrepared) yield return null;
        player.Play();
    }

    IEnumerator WaitForEnd()
    {
        bool done = false;
        VideoPlayer.EventHandler handler = _ => done = true;
        player.loopPointReached += handler;
        while (!done) yield return null;
        player.loopPointReached -= handler;
    }
}
