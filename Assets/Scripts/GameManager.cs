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
    public float displayedScore = 0f;
    public float bestScore = 0f;  // Nouvelle variable pour le meilleur score
    private float internalScore = 0f;
    public float InternalScore { get { return internalScore; } }  // Propriété publique pour accéder à internalScore
    private float currentComboCount = 0;
    
    // Indique si le joueur vient de cliquer sur une mauvaise carte
    public bool justClickedWrongCard { get; private set; } = false;

    [Header("Ads")]
    private AdMobAdsScript adMobAdsScript;
    private int gameCount = 0;  // Compteur de parties
    
    // Flag pour éviter les sélections multiples de wanted pendant une roulette
    private bool isSelectingNewWanted = false;

    private System.Random random;
    private bool isPaused = false;

    private float savedTimeRemaining;  // Pour sauvegarder le temps restant

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
        
        // Charger le meilleur score sauvegardé
        LoadBestScore();
    }

    private void LoadBestScore()
    {
        bestScore = PlayerPrefs.GetFloat("BestScore", 0f);
    }

    private void SaveBestScore()
    {
        PlayerPrefs.SetFloat("BestScore", bestScore);
        PlayerPrefs.Save();
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
        
        timeRemaining = roundDuration;
        isGameActive = true;
        onGameStart.Invoke();
        StartCoroutine(GameTimer());
        gameCount++;  // Incrémenter le compteur de parties
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
        savedTimeRemaining = timeRemaining;  // Sauvegarder le temps restant
        
        // Détruire la bannière publicitaire
        if (adMobAdsScript != null)
        {
            adMobAdsScript.DestroyBannerAd();
        }
        
        float finalScore = displayedScore + currentComboCount;
        onScoreChanged.Invoke(finalScore);

        AudioManager.Instance?.StopBackgroundMusic();

        // Vérifier et mettre à jour le meilleur score
        if (displayedScore > bestScore)
        {
            bestScore = displayedScore;
            SaveBestScore();
        }
        
        // Vérifier si c'est la 2ème partie
        if (gameCount % 2 == 0 && adMobAdsScript != null)
        {
            // Afficher l'interstitial avant l'écran de game over
            StartCoroutine(ShowInterstitialThenGameOver());
        }
        else
        {
            // On déclenche la séquence pour ne laisser que le Wanted visible
            StartCoroutine(RevealWantedAndGameOver());
        }
    }

    private IEnumerator ShowInterstitialThenGameOver()
    {
        // D'abord révéler le wanted
        var gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            foreach (var card in gridManager.cards)
            {
                if (card != null)
                {
                    if (card != wantedCharacter)
                    {
                        card.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
                    }
                    else
                    {
                        card.transform.DOScale(1.2f, 0.3f).SetLoops(2, LoopType.Yoyo)
                            .SetEase(Ease.OutBack);
                    }
                }
            }
        }

        yield return new WaitForSeconds(1f);

        // Montrer l'interstitial
        if (adMobAdsScript != null)
        {
            adMobAdsScript.LoadInterstitialAd();
            adMobAdsScript.ShowInterstitialAd();
        }

        // Attendre un peu pour s'assurer que l'interstitial a eu le temps de s'afficher
        yield return new WaitForSeconds(0.5f);

        // Afficher l'écran de game over
        onGameOver.Invoke();
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
        // Empêcher les appels multiples pendant une roulette
        if (isSelectingNewWanted)
        {
            Debug.LogWarning("SelectNewWantedCharacter déjà en cours - Ignoré");
            return;
        }
        
        // Vérifier que le personnage n'est pas déjà le wanted
        if (wantedCharacter == character)
        {
            Debug.LogWarning("GameManager: Tentative de sélectionner le même wanted character - Ignorée");
            return;
        }
        
        isSelectingNewWanted = true;
        
        AudioManager.Instance?.PlayWantedSelectionSound();
        wantedCharacter = character;
        
        // Notifier les abonnés du changement (déclenche la roulette UI)
        onNewWantedCharacter.Invoke(character);
        
        // Réinitialiser le flag après un court délai pour éviter les appels multiples
        StartCoroutine(ResetSelectingNewWantedFlag());
    }
    
    private IEnumerator ResetSelectingNewWantedFlag()
    {
        yield return new WaitForSeconds(0.5f);
        isSelectingNewWanted = false;
    }

    public void AddScore()
    {
        // Ajouter au combo
        currentComboCount++;
        
        // Incrémenter directement le score affiché
        displayedScore += scorePerCorrectClick;
        onScoreChanged.Invoke(displayedScore);
        
        // Le score réel continue d'augmenter de 1
        internalScore += scorePerCorrectClick;
        
        // Sauvegarder l'ancien temps restant
        float oldTimeRemaining = timeRemaining;
        // Calculer le nouveau temps restant (sans l'appliquer directement)
        float newTimeRemaining = Mathf.Min(timeRemaining + 5f, maxTime);
        
        // Démarrer l'animation d'incrémentation du timer
        StartCoroutine(AnimateTimerIncrease(oldTimeRemaining, newTimeRemaining));
        
        // Vérifier si une sélection de wanted est déjà en cours
        if (isSelectingNewWanted)
        {
            Debug.LogWarning("AddScore: Sélection de wanted déjà en cours - Création d'un nouveau wanted ignorée");
            return;
        }
        
        // Rechercher le GridManager une seule fois
        var gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            // Vérifier que le GridManager n'a pas déjà une roulette active
            if (!gridManager.IsRouletteActive)
            {
                PauseGame();
                gridManager.CreateNewWanted();
            }
            else
            {
                Debug.LogWarning("GameManager: GridManager a déjà une roulette active - Création d'un nouveau wanted ignorée");
            }
        }
    }
    
    private IEnumerator AnimateTimerIncrease(float startTime, float endTime)
    {
        // Si pas de temps ajouté, on ne fait rien
        if (Mathf.Approximately(startTime, endTime))
            yield break;
            
        // Pour chaque seconde ajoutée
        for (float currentTime = startTime + 1; currentTime <= endTime; currentTime += 1f)
        {
            // Mettre à jour le temps affiché
            timeRemaining = currentTime;
            
            // Jouer un son de tick pour chaque seconde ajoutée
            AudioManager.Instance?.PlayTimerIncreaseSound();
            
            // Attendre un court délai entre chaque incrémentation
            yield return new WaitForSeconds(0.15f);
        }
        
        // S'assurer que le temps final est exactement celui calculé
        timeRemaining = endTime;
    }

    // Nouvelle méthode pour réinitialiser le combo après l'animation
    public void ResetCombo()
    {
        currentComboCount = 0;
    }

    public void ApplyTimePenalty()
    {
        // Marquer qu'un clic incorrect vient d'être effectué
        justClickedWrongCard = true;
        
        // Réinitialiser le flag après un court délai
        StartCoroutine(ResetWrongClickFlag());
        
        timeRemaining = Mathf.Max(0f, timeRemaining - penaltyTime);
        if (timeRemaining <= 0)
        {
            GameOver();
        }
    }
    
    private IEnumerator ResetWrongClickFlag()
    {
        yield return new WaitForSeconds(0.5f);
        justClickedWrongCard = false;
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

    // Nouvelle méthode pour continuer la partie après la rewarded ad
    public void ContinueGame()
    {
        isGameActive = true;
        timeRemaining = savedTimeRemaining + 10f;  // Ajouter 10 secondes bonus
        AudioManager.Instance?.StartBackgroundMusic();
        
        // Recharger la bannière
        if (adMobAdsScript != null)
        {
            adMobAdsScript.LoadBannerAd();
        }

        // Masquer le menu game over
        UIManager.Instance?.OnGameStart();
        
        StartCoroutine(GameTimer());
    }
}
