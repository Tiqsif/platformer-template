using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;
using System.Collections;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }
    [SerializeField] private AudioMixer audioMixer;
    private Dictionary<AudioClip, AudioSource> playingClips = new Dictionary<AudioClip, AudioSource>();
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void KillSFX(AudioClip audioClip)
    {
        if (playingClips.ContainsKey(audioClip))
        {
            DestroyAudioSource(playingClips[audioClip], 0f);
        }
    }
    public void PlaySFX(AudioClip clip, float volume=1f)
    {
        
        if (!playingClips.ContainsKey(clip))
        {
            GameObject sfx = new GameObject("SFXObject");
            AudioSource audioSource = sfx.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("SFX")[0];
            audioSource.Play();
            playingClips.Add(clip, audioSource);
            if (clip == null || audioSource == null) return;
            StartCoroutine(DestroyAudioSource(audioSource, clip.length));
        }
        
    }

    public void KillAndPlaySFX(AudioClip clip, float volume=1f)
    {
        if (playingClips.ContainsKey(clip))
        {
            AudioSource audioSource = playingClips[clip];
            playingClips.Remove(clip);
            Destroy(audioSource?.gameObject);
        }

        PlaySFX(clip, volume);
    }

    private IEnumerator DestroyAudioSource(AudioSource audioSource, float t) // invoke this with delay
    {
        yield return new WaitForSeconds(t);
        if (audioSource == null) yield break;
        if (playingClips.ContainsKey(audioSource.clip))
        {
            playingClips.Remove(audioSource?.clip);
        }
        yield return null;
        if (audioSource == null) yield break;
        Destroy(audioSource?.gameObject);
    }
}
