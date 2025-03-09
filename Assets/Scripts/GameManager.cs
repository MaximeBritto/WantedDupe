using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [System.Serializable]
    public class CharacterSprites
    {
        public string characterColor;
        public Sprite[] expressions;
    }

    [Header("Character Sprites")]
    public CharacterSprites[] allCharacterSprites;
    
    [Header("Game Settings")]
    public float roundDuration = 30f;
    public int startingGridSize = 16; // Exemple de grille 4x4
    public float scorePerCorrectClick = 1f;
    
    [Header("Game State")]
    public float timeRemaining;
    public CharacterCard wantedCharacter;
    public bool isGameActive = false;
    
    public UnityEvent onGameStart = new UnityEvent();
    public UnityEvent onGameOver = new UnityEvent();
    public UnityEvent<CharacterCard> onNewWantedCharacter = new UnityEvent<CharacterCard>();
    public UnityEvent<float> onScoreChanged = new UnityEvent<float>();

    [Header("Timer Settings")]
    public float maxTime = 40f;
    public float penaltyTime = 5f;

    [Header("Score Settings")]
    public int maxComboMultiplier = 5;
    public int currentComboCount { get; private set; } = 0;
    public float displayedScore { get; private set; } = 0f;
    public float internalScore { get; private set; } = 0f;
    
    public UnityEvent<float> onComboChanged = new UnityEvent<float>();

    [Header("Ads")]
    private AdMobAdsScript adMobAdsScript;

    private System.Random random;
    private bool isPaused = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        random = new System.Random();
    }

    private void Start()
    {
        // Récupérer la référence à AdMobAdsScript
        adMobAdsScript = FindObjectOfType<AdMobAdsScript>();
    }

    public void StartGame()
    {
        AudioManager.Instance?.PlayButtonSound();
        AudioManager.Instance?.StartBackgroundMusic();
        
        // Charger et afficher la bannière publicitaire
        if (adMobAdsScript != null)
        {
            adMobAdsScript.LoadBannerAd();
        }
        
        internalScore = 0f;
        displayedScore = 0f;
        currentComboCount = 0;
        onComboChanged.Invoke(0f);
        
        timeRemaining = roundDuration;
        isGameActive = true;
        onGameStart.Invoke();
        StartCoroutine(GameTimer());
    }

    private IEnumerator GameTimer()
    {
        Debug.Log("Timer démarré");
        while (isGameActive)
        {
            yield return new WaitForSeconds(0.1f);
            if (!isPaused)
            {
                timeRemaining -= 0.1f;
                AudioManager.Instance?.UpdateMusicSpeed(timeRemaining);
                if (timeRemaining <= 0)
                {
                    GameOver();
                    break;
                }
            }
        }
    }

    public void GameOver()
    {
        isGameActive = false;
        
        // Détruire la bannière publicitaire
        if (adMobAdsScript != null)
        {
            adMobAdsScript.DestroyBannerAd();
        }
        
        float finalScore = displayedScore + currentComboCount;
        onScoreChanged.Invoke(finalScore);

        AudioManager.Instance?.StopBackgroundMusic();

        // On déclenche la séquence pour ne laisser que le Wanted visible
        StartCoroutine(RevealWantedAndGameOver());
    }

    /// <summary>
    /// Coroutine qui masque toutes les cartes sauf le Wanted,
    /// puis affiche l'écran de Game Over.
    /// </summary>
    private IEnumerator RevealWantedAndGameOver()
    {
        var gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            foreach (var card in gridManager.cards)
            {
                if (card != null)
                {
                    if (card != wantedCharacter)
                    {
                        // On fait disparaître les cartes non-wanted
                        card.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
                    }
                    else
                    {
                        // On peut agrandir légèrement le Wanted, ou le mettre en surbrillance
                        card.transform.DOScale(1.2f, 0.3f).SetLoops(2, LoopType.Yoyo)
                            .SetEase(Ease.OutBack);
                    }
                }
            }
        }

        // Attendre un peu pour finir l'animation de disparition
        yield return new WaitForSeconds(1f);

        // On peut ensuite déclencher l'écran Game Over
        onGameOver.Invoke();
    }

    public void SelectNewWantedCharacter(CharacterCard character)
    {
        AudioManager.Instance?.PlayWantedSelection();
        wantedCharacter = character;
        onNewWantedCharacter.Invoke(character);
    }

    public void AddScore()
    {
        // Ajouter au combo
        currentComboCount++;
        
        // Si le combo atteint le maximum
        if (currentComboCount >= maxComboMultiplier)
        {
            // Ne pas réinitialiser le combo ici, le ComboSlider s'en chargera
            // currentComboCount = 0;
            // Le score sera incrémenté progressivement par le ComboSlider
        }
        
        // Mettre à jour le slider
        onComboChanged.Invoke((float)currentComboCount / maxComboMultiplier);
        
        // Le score réel continue d'augmenter de 1
        internalScore += scorePerCorrectClick;
        
        timeRemaining = Mathf.Min(timeRemaining + 5f, maxTime);
        var gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            PauseGame();
            gridManager.CreateNewWanted();
        }
    }

    // Nouvelle méthode pour réinitialiser le combo après l'animation
    public void ResetCombo()
    {
        currentComboCount = 0;
        onComboChanged.Invoke(0f);
    }

    public void ApplyTimePenalty()
    {
        timeRemaining = Mathf.Max(0f, timeRemaining - penaltyTime);
        if (timeRemaining <= 0)
        {
            GameOver();
        }
    }

    public Sprite GetRandomSprite()
    {
        int colorIndex = random.Next(allCharacterSprites.Length);
        int expressionIndex = random.Next(allCharacterSprites[colorIndex].expressions.Length);
        return allCharacterSprites[colorIndex].expressions[expressionIndex];
    }

    public void StartNewRound()
    {
        if (internalScore > 500)
        {
            roundDuration = Mathf.Max(10f, roundDuration - 2f);
        }
        timeRemaining = roundDuration;
        onGameStart.Invoke();
    }

    public void PauseGame()
    {
        isPaused = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
    }

    public void IncrementDisplayedScore()
    {
        displayedScore += 1;
        onScoreChanged.Invoke(displayedScore);
    }
}
