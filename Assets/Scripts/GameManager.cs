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

    // Nouvelles variables pour sauvegarder l'état du combo
    private int savedComboCount = 0;
    private bool[] savedComboImageStates = new bool[5];
    private Vector3[] savedComboImageScales = new Vector3[5]; // Nouveau: sauvegarder les échelles
    private string savedIncreScore = "0";

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
        
        // Précharger les publicités récompensées
        if (adMobAdsScript != null)
        {
            adMobAdsScript.LoadRewardedAd();
        }
        
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
        
        // Sauvegarder l'état des étoiles de combo
        savedComboCount = (int)currentComboCount;
        
        // Sauvegarder l'état des images de combo
        if (UIManager.Instance != null && UIManager.Instance.comboImages != null)
        {
            Image[] comboImages = UIManager.Instance.comboImages;
            savedComboImageStates = new bool[comboImages.Length];
            savedComboImageScales = new Vector3[comboImages.Length]; // Initialiser le tableau d'échelles
            
            for (int i = 0; i < comboImages.Length && i < savedComboImageStates.Length; i++)
            {
                if (comboImages[i] != null)
                {
                    savedComboImageStates[i] = comboImages[i].gameObject.activeSelf;
                    
                    // Sauvegarder l'échelle exacte de chaque étoile
                    if (comboImages[i].transform != null)
                    {
                        savedComboImageScales[i] = comboImages[i].transform.localScale;
                    }
                    else
                    {
                        savedComboImageScales[i] = new Vector3(0.8f, 0.8f, 0.8f); // Une valeur par défaut plus petite
                    }
                }
            }
            
            // Sauvegarder le score incrémental
            if (UIManager.Instance.increScore != null)
            {
                savedIncreScore = UIManager.Instance.increScore.text;
            }
        }
        
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
        
        // Vérifier si c'est la bonne partie pour montrer l'interstitiel (tous les 2 jeux)
        if (gameCount % 2 == 0 && adMobAdsScript != null)
        {
            Debug.Log($"GameOver: Tentative d'affichage de l'interstitiel - gameCount={gameCount}");
            
            // S'assurer que la pub est chargée
            if (adMobAdsScript != null)
            {
                // Précharger la pub pour s'assurer qu'elle est prête
                adMobAdsScript.LoadInterstitialAd();
                // Attendre un peu puis afficher la pub et l'écran de game over
                StartCoroutine(ShowInterstitialThenGameOver());
            }
            else
            {
                Debug.LogError("GameOver: adMobAdsScript est null");
                StartCoroutine(RevealWantedAndGameOver());
            }
        }
        else
        {
            Debug.Log($"GameOver: Pas d'interstitiel cette fois - gameCount={gameCount}");
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
            // Charger l'interstitiel et attendre qu'il soit prêt
            adMobAdsScript.LoadInterstitialAd();
            
            // Attendre que l'interstitiel soit chargé (maximum 3 secondes)
            float waitTime = 0f;
            while (waitTime < 3f && !IsInterstitialAdReady())
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }
            
            // Afficher l'interstitiel s'il est prêt
            if (IsInterstitialAdReady())
            {
                adMobAdsScript.ShowInterstitialAd();
            }
        }

        // Attendre un peu pour s'assurer que l'interstitial a eu le temps de s'afficher
        yield return new WaitForSeconds(0.5f);

        // Afficher l'écran de game over
        onGameOver.Invoke();
    }
    
    // Méthode pour vérifier si l'interstitiel est prêt
    private bool IsInterstitialAdReady()
    {
        if (adMobAdsScript != null)
        {
            return adMobAdsScript.IsInterstitialReady();
        }
        return false;
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
        
        Debug.Log($"🔄 Sélection d'un nouveau wanted: {character.characterName}");
        
        // Réinitialiser toutes les cartes pour s'assurer qu'aucune autre n'est marquée comme wanted
        var gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            foreach (var card in gridManager.cards)
            {
                if (card != null && card != character)
                {
                    // S'assurer que cette carte n'est PAS marquée comme wanted
                    card.SetAsWanted(false);
                }
            }
        }
        
        // Marquer explicitement la nouvelle carte comme wanted
        character.SetAsWanted(true);
        
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
        Debug.Log("ContinueGame appelé - Relance du niveau");
        
        // Réactiver le statut du jeu
        isGameActive = true;
        
        // Restaurer le temps avec un bonus
        timeRemaining = savedTimeRemaining + 10f;  // Ajouter 10 secondes bonus
        
        // Redémarrer la musique
        AudioManager.Instance?.StartBackgroundMusic();
        
        // Recharger la bannière
        if (adMobAdsScript != null)
        {
            adMobAdsScript.LoadBannerAd();
        }

        // Masquer le menu game over et afficher l'interface de jeu
        UIManager.Instance?.OnGameStart();
        
        // Important: récupérer le GridManager et réinitialiser correctement le jeu
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            // CORRECTION: Ne pas appeler à la fois ResetGame et CreateNewWanted
            // car cela déclenche deux roulettes
            
            // Option 1: N'appeler que ResetGame qui va gérer toute la logique
            gridManager.ResetGame();
            
            // !! Ne pas appeler CreateNewWanted() ici pour éviter une double roulette !!
            // gridManager.CreateNewWanted(); -- SUPPRIMÉ
        }
        else
        {
            Debug.LogError("GridManager non trouvé dans ContinueGame");
        }
        
        // Restaurer l'état des étoiles de combo
        RestoreComboState();
        
        // Redémarrer le timer
        StartCoroutine(GameTimer());
    }
    
    // Nouvelle méthode pour restaurer l'état des étoiles de combo
    private void RestoreComboState()
    {
        Debug.Log($"Restauration de l'état du combo: {savedComboCount} étoiles");
        
        // Restaurer le compteur de combo
        currentComboCount = savedComboCount;
        
        // Restaurer l'état des images de combo
        if (UIManager.Instance != null && UIManager.Instance.comboImages != null)
        {
            Image[] comboImages = UIManager.Instance.comboImages;
            
            for (int i = 0; i < comboImages.Length && i < savedComboImageStates.Length; i++)
            {
                if (comboImages[i] != null)
                {
                    // Activer/désactiver l'image selon l'état sauvegardé
                    comboImages[i].gameObject.SetActive(savedComboImageStates[i]);
                    
                    // Restaurer l'échelle exacte de l'étoile
                    if (savedComboImageStates[i] && i < savedComboImageScales.Length)
                    {
                        // Utiliser l'échelle sauvegardée ou une valeur par défaut si l'échelle est nulle (0,0,0)
                        Vector3 scale = savedComboImageScales[i];
                        if (scale.magnitude < 0.1f) // Si l'échelle est presque nulle
                        {
                            scale = new Vector3(0.8f, 0.8f, 0.8f); // Utiliser une échelle légèrement réduite
                        }
                        
                        comboImages[i].transform.localScale = scale;
                        
                        // Arrêter toute animation DOTween en cours sur cette étoile
                        DOTween.Kill(comboImages[i].transform);
                    }
                }
            }
            
            // Restaurer le score incrémental
            if (UIManager.Instance.increScore != null && !string.IsNullOrEmpty(savedIncreScore))
            {
                UIManager.Instance.increScore.text = savedIncreScore;
            }
        }
    }
}
