using System.Collections;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager instance;

    [SerializeField] private AudioSource SFXObject;

    public AudioClip collectCoinSFX;
    public AudioClip hitHazardSFX;
    public AudioClip bottleBreakerSFX;
    public AudioClip bufferJumpSFX;
    public AudioClip introSong;
    public AudioClip bridgeSong;
    public AudioClip melodySong;

    private bool melodyStarted = false;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void playSFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        AudioSource audioSource = Instantiate(SFXObject, spawnTransform.position, Quaternion.identity);
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.Play();
        Destroy(audioSource.gameObject, audioClip.length);
    }

    // Loop the intro song
    public void playIntroSong()
    {
        SFXObject.clip = introSong;
        SFXObject.loop = true;
        SFXObject.Play();
    }

    // Stop the intro song
    public void stopIntroSong()
    {
        SFXObject.Stop();
    }

    // Play level music
    public void PlayLevelMusic()
    {
        SFXObject.clip = bridgeSong;
        SFXObject.loop = false;
        SFXObject.Play();
    }

    private void Update()
    {
        if (!SFXObject.isPlaying && !melodyStarted)
        {
            // Switch to the melody song
            SFXObject.clip = melodySong;
            SFXObject.loop = true;
            SFXObject.volume = 0.5f;
            SFXObject.Play();
            melodyStarted = true;

            // Start fading coroutine
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeMelodyLoop());
        }
    }

    // Stop level music
    public void StopLevelMusic()
    {
        SFXObject.Stop();

        // Stop fade coroutine if running
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
    }

    private IEnumerator FadeMelodyLoop()
    {
        while (true)
        {
            // Wait until the melody is near the end of its loop
            float fadeDuration = 1f; // Duration of the fade
            float targetVolume = 0.25f; // Minimum volume during fade-out

            while (SFXObject.time < melodySong.length - fadeDuration)
            {
                yield return null;
            }

            // Fade out
            yield return StartCoroutine(FadeVolume(SFXObject, targetVolume, fadeDuration));

            // Wait for the loop to restart
            while (SFXObject.time > 0.1f)
            {
                yield return null;
            }

            // Fade in
            yield return StartCoroutine(FadeVolume(SFXObject, 0.5f, fadeDuration));
        }
    }

    private IEnumerator FadeVolume(AudioSource audioSource, float targetVolume, float duration)
    {
        float startVolume = audioSource.volume;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}
