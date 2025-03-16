using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas Orders")]
    public Canvas mainCanvas;
    public Canvas gridCanvas;
    public Canvas uiCanvas;
    public Canvas overlayCanvas;

    [Header("Wanted Panel")]
    public Image wantedCharacterImage;
    public RectTransform wantedPanel;
    public RectTransform wantedImageRect;
    
    [Header("Game Info")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public RectTransform gameInfoPanel;
    
    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public Button startButton;
    public Button restartButton;

    [Header("Roulette Effect")]
    public float rouletteDuration = 2f;      // Durée totale de la roulette
    public float changeImageDelay = 0.1f;      // Délai entre chaque changement d'image

    [Header("Wanted Panel Sizes")]
    public Vector2 finalWantedPosition = new Vector2(0, -100);
    public Vector2 finalWantedSize = new Vector2(300, 400);
    public Vector2 rouletteWantedSize = new Vector2(500, 600);
    public Vector2 roulettePosition = new Vector2(0, 0);
    public float rouletteScale = 1.2f;
    public float wantedImageScale = 0.6f;
    public bool isRouletteRunning = false;
    private bool gridManagerRouletteActive = false;

    [Header("Game Board")]
    public RectTransform gameBoard;
    public Image gameBoardImage;

    [Header("Mobile Settings")]
    public bool isMobileDevice;
    public float mobileScaleFactor = 0.7f;
    public Vector2 mobileWantedSize = new Vector2(200, 300);
    public Vector2 mobileWantedPosition = new Vector2(0, 800);

    [Header("Safe Area")]
    public GameObject SafeArea;
    public GameObject Board;

    [Header("Difficulty Display")]
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI currentStateText;

    public ComboSlider comboSlider;

    [Header("Background")]
    public BackgroundManager backgroundManager;

    [Header("Continue Game")]
    public Button continueButton;  // Bouton pour regarder la pub et continuer

    private Vector2 timerInitialPosition;
    private AdMobAdsScript adMobAdsScript;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        gridCanvas.sortingOrder = 0;
        uiCanvas.sortingOrder = 1;
        overlayCanvas.sortingOrder = 2;

        DisableRaycastOnPanel(wantedPanel);
        DisableRaycastOnPanel(gameInfoPanel);
        
        GameManager.Instance.onGameStart.AddListener(OnGameStart);
        GameManager.Instance.onGameOver.AddListener(OnGameOver);
        GameManager.Instance.onNewWantedCharacter.AddListener(UpdateWantedCharacter);
        GameManager.Instance.onScoreChanged.AddListener(UpdateScoreText);
        
        menuPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        
        startButton.onClick.AddListener(StartGame);
        restartButton.onClick.AddListener(StartGame);

        if (gameBoardImage != null)
        {
            gameBoardImage.color = new Color(0, 0, 0, 0.2f);
        }

        isMobileDevice = Application.isMobilePlatform;
        if (isMobileDevice)
        {
            ConfigureForPortrait();
        }

        if (SafeArea != null)
        {
            SafeArea.SetActive(false);
            Board.SetActive(false);
        }
        if (uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(false);
        }

        if (backgroundManager != null)
        {
            backgroundManager.gameObject.SetActive(true);
        }

        if (timerText != null)
        {
            timerInitialPosition = timerText.rectTransform.anchoredPosition;
        }

        adMobAdsScript = FindObjectOfType<AdMobAdsScript>();
        
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(() => {
                if (adMobAdsScript != null)
                {
                    adMobAdsScript.LoadRewardedAd();
                    adMobAdsScript.ShowRewardedAd();
                }
            });
        }
    }

    private void DisableRaycastOnPanel(RectTransform panel)
    {
        if (panel != null)
        {
            Image[] images = panel.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                img.raycastTarget = false;
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameActive)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (GameManager.Instance == null) return;
        
        scoreText.text = $"{GameManager.Instance.displayedScore}";
        float timeRemaining = GameManager.Instance.timeRemaining;
        timerText.text = $"{Mathf.CeilToInt(timeRemaining)}";
        
        if (timeRemaining <= 10f)
        {
            timerText.color = Color.red;
            if (!DOTween.IsTweening(timerText.transform))
            {
                timerText.rectTransform.DOShakeAnchorPos(0.5f, 5f, 20, 90, false, true)
                    .SetLoops(-1, LoopType.Restart)
                    .SetId("TimerShake");
            }
        }
        else
        {
            timerText.color = Color.white;
            DOTween.Kill("TimerShake");
            timerText.rectTransform.anchoredPosition = timerInitialPosition;
        }
    }

    // Méthode appelée par GridManager quand sa roulette démarre
    public void OnGridManagerRouletteStarted()
    {
        gridManagerRouletteActive = true;
        Debug.Log("UIManager notifié: GridManager roulette démarrée");
    }
    
    // Méthode appelée par GridManager quand sa roulette se termine
    public void OnGridManagerRouletteEnded()
    {
        gridManagerRouletteActive = false;
        Debug.Log("UIManager notifié: GridManager roulette terminée");
    }

    private void UpdateWantedCharacter(CharacterCard character)
    {
        // Si une roulette est déjà en cours, ne pas en démarrer une nouvelle
        if (isRouletteRunning)
        {
            Debug.LogWarning("UIManager: une roulette est déjà en cours - Ignorée");
            return;
        }
        
        Debug.Log("UIManager: Démarrage de la roulette UI avec le sprite: " + character.characterSprite.name);
        StartCoroutine(WantedRouletteEffect(character));
    }

    private IEnumerator WantedRouletteEffect(CharacterCard finalCharacter)
    {
        // Vérifier à nouveau si une roulette est déjà active
        if (isRouletteRunning)
        {
            Debug.LogWarning("UIManager: WantedRouletteEffect annulé - une roulette est déjà active");
            yield break;
        }
            
        GameManager.Instance.PauseGame();
        isRouletteRunning = true;
        
        // Désactiver temporairement la grille pour éviter un pattern en arrière-plan
        gridCanvas.gameObject.SetActive(false);
        
        if (wantedCharacterImage.sprite == null)
        {
            wantedCharacterImage.sprite = GameManager.Instance.GetRandomSprite();
        }
        
        // Animation de démarrage de la roulette
        Sequence startSequence = DOTween.Sequence();
        startSequence.Join(wantedPanel.DOAnchorPos(roulettePosition, 0.5f))
                     .Join(wantedPanel.DOSizeDelta(rouletteWantedSize, 0.5f))
                     .Join(wantedPanel.transform.DOScale(rouletteScale, 0.5f));
        yield return startSequence.WaitForCompletion();

        float elapsedTime = 0;
        while (elapsedTime < rouletteDuration)
        {
            Sprite randomSprite = GameManager.Instance.GetRandomSprite();
            wantedCharacterImage.sprite = randomSprite;
            float imageRatio = randomSprite.rect.width / randomSprite.rect.height;
            float rouletteHeight = rouletteWantedSize.y * wantedImageScale;
            float rouletteWidth = rouletteHeight * imageRatio;
            wantedImageRect.sizeDelta = new Vector2(rouletteWidth, rouletteHeight);
            yield return new WaitForSeconds(changeImageDelay);
            elapsedTime += changeImageDelay;
        }

        // Ralentir la roulette en fin de cycle
        float[] finalDelays = { 0.2f, 0.3f, 0.4f, 0.5f };
        foreach (float delay in finalDelays)
        {
            Sprite randomSprite = GameManager.Instance.GetRandomSprite();
            wantedCharacterImage.sprite = randomSprite;
            yield return new WaitForSeconds(delay);
        }

        // Afficher le sprite final du wanted
        wantedCharacterImage.sprite = finalCharacter.characterSprite;
        AudioManager.Instance.PlayCorrect();

        yield return new WaitForSeconds(0.5f);

        // Calcul de la taille finale de l'image
        float finalImageRatio = finalCharacter.characterSprite.rect.width / finalCharacter.characterSprite.rect.height;
        float finalHeight = finalWantedSize.y * wantedImageScale;
        float finalWidth = finalHeight * finalImageRatio;
        Vector2 finalImageSize = new Vector2(finalWidth, finalHeight);

        Sequence endSequence = DOTween.Sequence();
        endSequence.Join(wantedPanel.DOAnchorPos(finalWantedPosition, 0.5f))
                   .Join(wantedPanel.DOSizeDelta(finalWantedSize, 0.5f))
                   .Join(wantedPanel.transform.DOScale(1f, 0.5f))
                   .Join(wantedImageRect.DOSizeDelta(finalImageSize, 0.5f));
        yield return endSequence.WaitForCompletion();

        // Réactiver la grille et s'assurer que toutes les cartes sont positionnées mais invisibles
        gridCanvas.gameObject.SetActive(true);
        
        // Attendre un court délai pour s'assurer que la grille est complètement réactivée
        yield return new WaitForSeconds(0.1f);
        
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            Debug.Log("UIManager: Préparation des cartes après roulette");
            
            // Forcer l'arrangement des cartes
            gridManager.ArrangeCardsBasedOnState();
            
            // Cacher toutes les cartes pendant qu'elles sont positionnées
            foreach (var card in gridManager.cards)
            {
                if (card != null)
                {
                    card.gameObject.SetActive(true);
                    card.transform.localScale = Vector3.zero;
                }
            }
            
            // Identifier le pattern actuel
            string patternType = gridManager.CurrentState.ToString();
            Debug.Log($"UIManager: Pattern détecté: {patternType}");
            
            // Traitement spécial pour certains patterns qui posent problème
            float delayBeforeAnimation = 0.3f;
            
            if (patternType.Contains("Column"))
            {
                Debug.Log("UIManager: Traitement spécial pour pattern Columns");
                delayBeforeAnimation = 0.5f;
                
                // Forcer un second positionnement pour les colonnes
                yield return new WaitForSeconds(0.2f);
                gridManager.ArrangeCardsBasedOnState();
                
                // Forcer un dernier positionnement
                yield return new WaitForSeconds(0.2f);
                gridManager.ArrangeCardsBasedOnState();
            }
            else if (patternType.Contains("Circular"))
            {
                Debug.Log("UIManager: Traitement spécial pour pattern CircularAligned");
                delayBeforeAnimation = 0.5f;
                
                // Forcer un second positionnement
                yield return new WaitForSeconds(0.2f);
                gridManager.ArrangeCardsBasedOnState();
                
                // Forcer un dernier positionnement
                yield return new WaitForSeconds(0.2f);
                gridManager.ArrangeCardsBasedOnState();
            }
            else if (patternType.Contains("Pulsing"))
            {
                Debug.Log("UIManager: Traitement spécial pour pattern Pulsing");
                delayBeforeAnimation = 0.7f;
                
                // Forcer une seconde fois l'arrangement pour les patterns complexes
                yield return new WaitForSeconds(0.2f);
                gridManager.ArrangeCardsBasedOnState();
                
                // Attendre encore pour s'assurer que tout est bien positionné
                yield return new WaitForSeconds(0.2f);
                gridManager.ArrangeCardsBasedOnState();
            }
            
            // Pour tous les patterns, un délai final avant de montrer les cartes
            yield return new WaitForSeconds(delayBeforeAnimation);
            
            // Animer l'entrée des cartes maintenant que tout est correctement positionné
            Debug.Log("UIManager: Animation des cartes après positionnement");
            gridManager.AnimateCardsEntry();
            
            // Ajouter une solution de secours pour s'assurer que les cartes sont visibles
            StartCoroutine(ForceShowCardsBackup(gridManager, 0.5f));
        }
        
        GameManager.Instance.ResumeGame();
        isRouletteRunning = false;
        Debug.Log("UIManager: Fin de la roulette UI");
    }
    
    // Méthode de secours qui force l'affichage des cartes après un délai
    private IEnumerator ForceShowCardsBackup(GridManager gridManager, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Si les cartes ne sont toujours pas visibles, les forcer à l'être
        if (gridManager != null)
        {
            bool anyInvisibleCards = false;
            bool anyInvalidScale = false;
            
            Debug.Log("SOLUTION DE SECOURS: Vérification des cartes...");
            
            foreach (var card in gridManager.cards)
            {
                if (card != null)
                {
                    // Vérifier si la carte est invisible
                    if (!card.gameObject.activeSelf)
                    {
                        anyInvisibleCards = true;
                        card.gameObject.SetActive(true);
                        Debug.Log($"Carte {card.name} forcée à être active");
                    }
                    
                    // Vérifier si la carte a une échelle incorrecte
                    if (Vector3.Distance(card.transform.localScale, Vector3.one) > 0.01f)
                    {
                        anyInvalidScale = true;
                        Debug.Log($"Carte {card.name} avait une échelle incorrecte: {card.transform.localScale}");
                        
                        // Arrêter toute animation en cours sur cette carte
                        DOTween.Kill(card.transform);
                        
                        // Forcer l'échelle à exactement 1
                        card.transform.localScale = Vector3.one;
                    }
                }
            }
            
            if (anyInvisibleCards || anyInvalidScale)
            {
                Debug.LogWarning("SOLUTION DE SECOURS APPLIQUÉE: Des problèmes de visibilité ou d'échelle des cartes ont été corrigés");
                
                // Tenter de réactiver la grille si elle était désactivée
                if (!gridCanvas.gameObject.activeSelf)
                {
                    gridCanvas.gameObject.SetActive(true);
                    Debug.LogWarning("SOLUTION DE SECOURS: Le canvas de la grille a été réactivé");
                }
                
                // S'assurer que les cartes sont correctement positionnées
                gridManager.ArrangeCardsBasedOnState();
            }
            else
            {
                Debug.Log("SOLUTION DE SECOURS: Toutes les cartes sont correctement visibles et à l'échelle 1");
            }
        }
    }

    public void OnGameStart()
    {
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        if (SafeArea != null)
        {
            SafeArea.SetActive(true);
            Board.SetActive(true);
        }
        if (uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(true);
        }

        if (wantedPanel != null)
        {
            wantedPanel.transform.localScale = Vector3.one;
            wantedPanel.sizeDelta = finalWantedSize;
            wantedPanel.anchoredPosition = finalWantedPosition;
            
            if (wantedImageRect != null && wantedCharacterImage.sprite != null)
            {
                float imageRatio = wantedCharacterImage.sprite.rect.width / wantedCharacterImage.sprite.rect.height;
                float height = finalWantedSize.y * wantedImageScale;
                float width = height * imageRatio;
                wantedImageRect.sizeDelta = new Vector2(width, height);
            }
        }
    }

    private void OnGameOver()
    {
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        if (SafeArea != null)
        {
            SafeArea.SetActive(false);
            Board.SetActive(false);
        }
        if (uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(false);
        }
        
        if (GameManager.Instance != null)
        {
            float finalScore = GameManager.Instance.displayedScore + GameManager.Instance.currentComboCount;
            finalScoreText.text = $"Score : {finalScore}";
        }

        // Activer le bouton continue
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
        }
    }

    private void StartGame()
    {
        GameManager.Instance.StartGame();
    }

    public void UpdateDifficultyText(int threshold, GridManager.GridState state)
    {
        if (difficultyText != null)
        {
            difficultyText.text = $"Niveau {threshold / 500 + 1}";
        }
        if (currentStateText != null)
        {
            string stateText = state switch
            {
                GridManager.GridState.Aligned => "Mode Aligné",
                GridManager.GridState.Columns => "Mode Colonnes",
                GridManager.GridState.Static => "Mode Statique",
                GridManager.GridState.SlowMoving => "Mode Mobile Lent",
                GridManager.GridState.FastMoving => "Mode Mobile Rapide",
                _ => state.ToString()
            };
            currentStateText.text = stateText;
        }
    }

    private void ConfigureForPortrait()
    {
        finalWantedSize = mobileWantedSize;
        finalWantedPosition = mobileWantedPosition;
        if (scoreText != null) scoreText.fontSize = 40;
        if (timerText != null) timerText.fontSize = 40;
    }

    private void UpdateScoreText(float score)
    {
        if (GameManager.Instance.isGameActive)
        {
            scoreText.text = $"Score: {GameManager.Instance.displayedScore}";
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill("TimerShake");
    }
}
