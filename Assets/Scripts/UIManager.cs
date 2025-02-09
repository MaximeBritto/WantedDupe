using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas Orders")]
    public Canvas mainCanvas;        // Canvas principal
    public Canvas gridCanvas;        // Canvas pour la grille de cartes
    public Canvas uiCanvas;          // Canvas pour l'UI (score, timer)
    public Canvas overlayCanvas;     // Canvas pour les menus

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
    public float changeImageDelay = 0.1f;    // Délai entre chaque changement d'image

    [Header("Wanted Panel Sizes")]
    public Vector2 finalWantedPosition = new Vector2(0, -100);  // Position finale en haut
    public Vector2 finalWantedSize = new Vector2(300, 400);     // Taille finale
    public Vector2 rouletteWantedSize = new Vector2(500, 600);  // Taille pendant la roulette
    public Vector2 roulettePosition = new Vector2(0, 0);        // Position pendant la roulette
    public float rouletteScale = 1.2f;                          // Échelle pendant la roulette
    public float wantedImageScale = 0.6f;                       // Facteur de proportion de l'image
    public bool isRouletteRunning = false;

    [Header("Game Board")]
    public RectTransform gameBoard;
    public Image gameBoardImage;

    [Header("Mobile Settings")]
    public bool isMobileDevice;
    public float mobileScaleFactor = 0.7f;  // Facteur d'échelle pour mobile
    public Vector2 mobileWantedSize = new Vector2(200, 300);    // Taille du wanted en mode portrait
    public Vector2 mobileWantedPosition = new Vector2(0, 800);  // Position du wanted en mode portrait

    [Header("Safe Area")]
    public GameObject SafeArea;  // Changé pour GameObject et avec une majuscule
    public GameObject Board;  // Changé pour GameObject et avec une majuscule

    [Header("Difficulty Display")]
    public TextMeshProUGUI difficultyText;      // Pour afficher le niveau
    public TextMeshProUGUI currentStateText;     // Pour afficher l'état actuel

    public ComboSlider comboSlider;

    [Header("Background")]
    public BackgroundManager backgroundManager;

    private Vector2 timerInitialPosition;

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
    }

    private void Start()
    {
        // Configuration des ordres de Canvas
        gridCanvas.sortingOrder = 0;        
        uiCanvas.sortingOrder = 1;          
        overlayCanvas.sortingOrder = 2;     

        // Désactiver les raycasts sur les panels UI
        DisableRaycastOnPanel(wantedPanel);
        DisableRaycastOnPanel(gameInfoPanel);
        
        // Configuration des listeners
        GameManager.Instance.onGameStart.AddListener(OnGameStart);
        GameManager.Instance.onGameOver.AddListener(OnGameOver);
        GameManager.Instance.onNewWantedCharacter.AddListener(UpdateWantedCharacter);
        GameManager.Instance.onScoreChanged.AddListener(UpdateScoreText);
        
        menuPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        // Le WantedPanel reste visible par défaut
        
        startButton.onClick.AddListener(StartGame);
        restartButton.onClick.AddListener(StartGame);

        // Configurer le plateau de jeu
        if (gameBoardImage != null)
        {
            gameBoardImage.color = new Color(0, 0, 0, 0.2f);  // Fond semi-transparent
        }

        // Détecter si on est sur mobile
        isMobileDevice = Application.isMobilePlatform;
        
        if (isMobileDevice)
        {
            // Configurer pour le format portrait
            ConfigureForPortrait();
        }

        // Cacher le SafeArea et l'UI Canvas au démarrage
        if (SafeArea != null)
        {
            SafeArea.SetActive(false);
            Board.SetActive(false);
        }
        if (uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(false);
        }

        // S'assurer que le background est visible mais derrière les autres éléments
        if (backgroundManager != null)
        {
            backgroundManager.gameObject.SetActive(true);
        }

        // Sauvegarder la position initiale du timer
        if (timerText != null)
        {
            timerInitialPosition = timerText.rectTransform.anchoredPosition;
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
        // Ne mettre à jour l'UI que si le jeu est actif ET que GameManager existe
        if (GameManager.Instance != null && GameManager.Instance.isGameActive)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (GameManager.Instance == null) return;
        
        // Mettre à jour le score
        scoreText.text = $"{GameManager.Instance.displayedScore}";

        // Mettre à jour le timer avec changement de couleur
        float timeRemaining = GameManager.Instance.timeRemaining;
        timerText.text = $"{Mathf.CeilToInt(timeRemaining)}";
        
        // Changer la couleur en rouge et faire trembler si <= 10 secondes
        if (timeRemaining <= 10f)
        {
            timerText.color = Color.red;
            
            // Vérifier si une animation de tremblement est déjà en cours
            if (!DOTween.IsTweening(timerText.transform))
            {
                // Créer un effet de tremblement autour de la position initiale
                timerText.rectTransform.DOShakeAnchorPos(0.5f, 5f, 20, 90, false, true)
                    .SetLoops(-1, LoopType.Restart)
                    .SetId("TimerShake");
            }
        }
        else
        {
            timerText.color = Color.white;
            // Arrêter le tremblement si actif
            DOTween.Kill("TimerShake");
            // Réinitialiser la position
            timerText.rectTransform.anchoredPosition = timerInitialPosition;
        }
    }

    private void UpdateWantedCharacter(CharacterCard character)
    {
        if (isRouletteRunning) return;
        
        // Ne pas activer le panel ici, laissez WantedRouletteEffect le faire
        StartCoroutine(WantedRouletteEffect(character));
    }

    private IEnumerator WantedRouletteEffect(CharacterCard finalCharacter)
    {
        isRouletteRunning = true;
        
        // Désactiver temporairement la grille
        gridCanvas.gameObject.SetActive(false);
        
        // S'assurer d'avoir un sprite initial
        if (wantedCharacterImage.sprite == null)
        {
            wantedCharacterImage.sprite = GameManager.Instance.GetRandomSprite();
        }
        
        // Animation de descente initiale (sans changer la taille de l'image)
        Sequence startSequence = DOTween.Sequence();
        startSequence.Join(wantedPanel.DOAnchorPos(roulettePosition, 0.5f))
                    .Join(wantedPanel.DOSizeDelta(rouletteWantedSize, 0.5f))
                    .Join(wantedPanel.transform.DOScale(rouletteScale, 0.5f));

        yield return startSequence.WaitForCompletion();

        // Effet de roulette
        float elapsedTime = 0;
        while (elapsedTime < rouletteDuration)
        {
            Sprite randomSprite = GameManager.Instance.GetRandomSprite();
            wantedCharacterImage.sprite = randomSprite;
            
            // Ajuster la taille de l'image après avoir assigné le sprite
            float imageRatio = randomSprite.rect.width / randomSprite.rect.height;
            float rouletteHeight = rouletteWantedSize.y * wantedImageScale;
            float rouletteWidth = rouletteHeight * imageRatio;
            wantedImageRect.sizeDelta = new Vector2(rouletteWidth, rouletteHeight);
            
            yield return new WaitForSeconds(changeImageDelay);
            elapsedTime += changeImageDelay;
        }

        // Ralentir à la fin avec des délais plus longs
        float[] finalDelays = { 0.2f, 0.3f, 0.4f, 0.5f };
        foreach (float delay in finalDelays)
        {
            Sprite randomSprite = GameManager.Instance.GetRandomSprite();
            wantedCharacterImage.sprite = randomSprite;
            yield return new WaitForSeconds(delay);
        }

        // Afficher le sprite final
        wantedCharacterImage.sprite = finalCharacter.characterSprite;
        AudioManager.Instance.PlayCorrect();

        yield return new WaitForSeconds(0.5f);

        // Calculer la taille finale de l'image
        float finalImageRatio = finalCharacter.characterSprite.rect.width / finalCharacter.characterSprite.rect.height;
        float finalHeight = finalWantedSize.y * wantedImageScale;
        float finalWidth = finalHeight * finalImageRatio;
        Vector2 finalImageSize = new Vector2(finalWidth, finalHeight);

        // Animation de remontée et rétrécissement
        Sequence endSequence = DOTween.Sequence();
        endSequence.Join(wantedPanel.DOAnchorPos(finalWantedPosition, 0.5f))
                  .Join(wantedPanel.DOSizeDelta(finalWantedSize, 0.5f))
                  .Join(wantedPanel.transform.DOScale(1f, 0.5f))
                  .Join(wantedImageRect.DOSizeDelta(finalImageSize, 0.5f));

        yield return endSequence.WaitForCompletion();

        // Réactiver la grille et les cartes
        gridCanvas.gameObject.SetActive(true);
        
        var gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            gridManager.AnimateCardsEntry();
        }

        GameManager.Instance.ResumeGame();
        isRouletteRunning = false;
    }

    private void OnGameStart()
    {
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        // Afficher le SafeArea et l'UI Canvas quand la partie commence
        if (SafeArea != null)
        {
            SafeArea.SetActive(true);
            Board.SetActive(true);
        }
        if (uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(true);
        }

        // Réinitialiser le WantedPanel et l'image à leur taille normale
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

        // Cacher le SafeArea et l'UI Canvas à la fin de la partie
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
    }

    private void StartGame()
    {
        GameManager.Instance.StartGame();
    }

    public void UpdateDifficultyText(int threshold, GridManager.GridState state)
    {
        if (difficultyText != null)
        {
            difficultyText.text = $"Niveau {threshold/500 + 1}";
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
        // Ajuster le wanted
        finalWantedSize = mobileWantedSize;
        finalWantedPosition = mobileWantedPosition;
        
        // Ajuster la taille des textes
        if (scoreText != null) scoreText.fontSize = 40;
        if (timerText != null) timerText.fontSize = 40;
    }

    private void UpdateScoreText(float score)
    {
        // Pendant le jeu, n'afficher que le displayedScore
        if (GameManager.Instance.isGameActive)
        {
            scoreText.text = $"Score: {GameManager.Instance.displayedScore}";
        }
    }

    // Ajouter cette méthode pour nettoyer les tweens au besoin
    private void OnDestroy()
    {
        DOTween.Kill("TimerShake");
    }
} 