using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Sound Effects")]
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip shuffleSound;
    public AudioClip playButtonSound;
    public AudioClip wantedSelectionSound;  // Nouveau son pour la sélection du wanted
    public AudioClip comboSound;  // Nouveau son pour les images de combo
    public AudioClip comboDisappearSound;  // Nouveau son pour la disparition des images de combo
    public AudioClip scoreIncreaseSound;  // Nouveau son pour l'augmentation du score
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
        StopWantedSelectionSound(); // Arrêter le son de sélection avant de jouer le son correct
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

    public void PlayWantedSelection()
    {
        if (wantedSelectionSound != null)
        {
            wantedSelectionSource.clip = wantedSelectionSound;
            wantedSelectionSource.Play();
        }
    }

    public void StopWantedSelectionSound()
    {
        if (wantedSelectionSource != null && wantedSelectionSource.isPlaying)
        {
            wantedSelectionSource.Stop();
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

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume);
        }
    }
} 