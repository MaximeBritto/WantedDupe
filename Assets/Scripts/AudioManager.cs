using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Sound Effects")]
    [SerializeField] public AudioClip correctSound;
    [SerializeField] public AudioClip wrongSound;
    [SerializeField] public AudioClip shuffleSound;
    [SerializeField] public AudioClip playButtonSound;
    [SerializeField] public AudioClip wantedSelectionSound;  // Nouveau son pour la sélection du wanted
    [SerializeField] public AudioClip comboSound;  // Nouveau son pour les images de combo
    [SerializeField] public AudioClip comboDisappearSound;  // Nouveau son pour la disparition des images de combo
    [SerializeField] public AudioClip scoreIncreaseSound;  // Nouveau son pour l'augmentation du score
    [SerializeField] public AudioClip timerIncreaseSound;  // Nouveau son pour l'augmentation du timer
    [Range(0f, 1f)]
    public float sfxVolume = 1f;  // Volume des effets sonores
    
    [Header("Background Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;  // Volume de la musique
    [Range(1f, 2f)]
    public float maxPitch = 1.5f;  // Pitch maximum quand le temps est proche de 0
    
    [Header("Timer Sound")]
    public AudioClip timerTickSound;  // Son pour chaque seconde qui passe
    
    private AudioSource audioSource;        // Pour les effets sonores
    private AudioSource musicSource;        // Pour la musique de fond
    private AudioSource wantedSelectionSource; // Nouvel AudioSource dédié au son de sélection
    private float lastTickTime;       // Pour suivre la dernière fois qu'on a joué le son
    
    private void Awake()
    {
        Instance = this;
        // Configuration de l'audio source pour les effets
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = sfxVolume;
        
        // Configuration de l'audio source pour la musique
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.pitch = 1f;
        musicSource.volume = musicVolume;
        
        // Configuration de l'audio source pour la sélection du wanted
        wantedSelectionSource = gameObject.AddComponent<AudioSource>();
        wantedSelectionSource.volume = sfxVolume;
    }

    // Méthodes pour ajuster le volume en temps réel
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = sfxVolume;
        }
    }

    public void StartBackgroundMusic()
    {
        if (musicSource && backgroundMusic)
        {
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    public void StopBackgroundMusic()
    {
        if (musicSource)
        {
            musicSource.Stop();
        }
    }

    public void UpdateMusicSpeed(float timeRemaining)
    {
        if (musicSource)
        {
            // Gestion du son du timer
            float currentTime = Mathf.Floor(timeRemaining);
            if (currentTime != lastTickTime && timerTickSound != null)
            {
                PlaySound(timerTickSound);
                lastTickTime = currentTime;
            }

            // Logique existante pour le pitch de la musique
            if (timeRemaining > 20f)
            {
                musicSource.pitch = 1f;
            }
            else
            {
                float pitchFactor = 1f - (timeRemaining / 20f);
                musicSource.pitch = Mathf.Lerp(1f, maxPitch, pitchFactor);
            }
        }
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

    public void PlayButtonSound()
    {
        PlaySound(playButtonSound);
    }

    public void PlayWantedSelectionSound()
    {
        if (wantedSelectionSource && wantedSelectionSound)
        {
            wantedSelectionSource.clip = wantedSelectionSound;
            wantedSelectionSource.Play();
        }
    }

    public void PlayComboSound()
    {
        PlaySound(comboSound);
    }

    public void PlayComboDisappearSound()
    {
        PlaySound(comboDisappearSound);
    }

    public void PlayScoreIncreaseSound()
    {
        PlaySound(scoreIncreaseSound);
    }
    
    public void PlayTimerIncreaseSound()
    {
        PlaySound(timerIncreaseSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip, sfxVolume);
        }
    }
} 