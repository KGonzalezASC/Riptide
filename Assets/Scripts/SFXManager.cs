using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager instance;

    [SerializeField] private AudioSource SFXObject;

    public AudioClip collectCoinSFX;
    public AudioClip hitHazardSFX;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void playSFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        // spawn audio gameObject
        AudioSource audioSource = Instantiate(SFXObject, spawnTransform.position, Quaternion.identity);

        // assign the audioclip
        audioSource.clip = audioClip;

        // assign volume
        audioSource.volume = volume;

        // play sound
        //Debug.Log(audioSource);
        audioSource.Play();

        // get length of SFX clip
        float clipLength = audioClip.length;

        // destroy after played
        Destroy(audioSource.gameObject, clipLength);
        
    }
}
