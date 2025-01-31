using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Sound Effects")]
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip shuffleSound;
    
    private AudioSource audioSource;

    private void Awake()
    {
        Instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayCorrect()
    {
        PlaySound(correctSound);
    }

    public void PlayWrong()
    {
        PlaySound(wrongSound);
    }

    public void PlayShuffle()
    {
        PlaySound(shuffleSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
} 