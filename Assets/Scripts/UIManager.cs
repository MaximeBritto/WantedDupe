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
    public TextMeshProUGUI wantedText;
    public RectTransform wantedPanel;
    
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
    public float fullscreenScale = 2f;       // Échelle maximale du panneau
    public Vector2 finalWantedPosition = new Vector2(0, 400f); // Position Y en haut de l'écran
    public Vector2 finalWantedSize = new Vector2(200, 300);    // Taille finale plus petite
    public bool isRouletteRunning = false;

    [Header("Game Board")]
    public RectTransform gameBoard;
    public Image gameBoardImage;

    [Header("Mobile Settings")]
    public bool isMobileDevice;
    public float mobileScaleFactor = 0.7f;  // Facteur d'échelle pour mobile
    public Vector2 portraitWantedPosition = new Vector2(0, 800f);  // Plus haut en mode portrait
    public Vector2 portraitWantedSize = new Vector2(300, 450f);    // Taille adaptée au portrait

    [Header("Safe Area")]
    public RectTransform safeAreaRect;

    [Header("Difficulty Display")]
    public TextMeshProUGUI difficultyText;      // Pour afficher le niveau
    public TextMeshProUGUI currentStateText;     // Pour afficher l'état actuel

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
        gridCanvas.sortingOrder = 0;        // La grille en dessous
        uiCanvas.sortingOrder = 1;          // L'UI au-dessus de la grille
        overlayCanvas.sortingOrder = 2;     // Les menus tout au-dessus

        // Désactiver les raycasts sur les panels UI
        DisableRaycastOnPanel(wantedPanel);
        DisableRaycastOnPanel(gameInfoPanel);
        
        // Configuration des listeners
        GameManager.Instance.onGameStart.AddListener(OnGameStart);
        GameManager.Instance.onGameOver.AddListener(OnGameOver);
        GameManager.Instance.onNewWantedCharacter.AddListener(UpdateWantedCharacter);
        
        menuPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        
        startButton.onClick.AddListener(StartGame);
        restartButton.onClick.AddListener(StartGame);

        // Configurer le plateau de jeu
        if (gameBoardImage != null)
        {
            gameBoardImage.color = new Color(0, 0, 0, 0.2f);  // Fond semi-transparent
        }

        // Détecter si on est sur mobile
        isMobileDevice = Application.isMobilePlatform;
        
        // Ajuster les tailles pour mobile
        if (isMobileDevice)
        {
            // Vérifier si on est en mode portrait
            bool isPortrait = Screen.height > Screen.width;
            if (isPortrait)
            {
                finalWantedSize = portraitWantedSize;
                finalWantedPosition = portraitWantedPosition;
                
                // Ajuster la zone de jeu pour le mode portrait
                var gridManager = FindObjectOfType<GridManager>();
                if (gridManager != null)
                {
                    gridManager.playAreaWidth = Screen.width * 0.9f;
                    gridManager.playAreaHeight = Screen.height * 0.5f;  // Moins haut pour laisser de la place au wanted
                }
            }
            else
            {
                // Ajuster la taille du wanted
                finalWantedSize = new Vector2(150, 225);  // Plus petit sur mobile
                finalWantedPosition = new Vector2(0, Screen.height * 0.4f);  // Position adaptée
                
                // Ajuster la taille des cartes
                var cardScale = Vector3.one * mobileScaleFactor;
                foreach (var card in FindObjectsOfType<CharacterCard>())
                {
                    card.transform.localScale = cardScale;
                }
                
                // Ajuster la zone de jeu
                var gridManager = FindObjectOfType<GridManager>();
                if (gridManager != null)
                {
                    gridManager.playAreaWidth = Screen.width * 0.9f;
                    gridManager.playAreaHeight = Screen.height * 0.7f;
                }

                // Appliquer la safe area
                Rect safeArea = Screen.safeArea;
                Vector2 anchorMin = safeArea.position;
                Vector2 anchorMax = anchorMin + safeArea.size;
                
                anchorMin.x /= Screen.width;
                anchorMin.y /= Screen.height;
                anchorMax.x /= Screen.width;
                anchorMax.y /= Screen.height;
                
                safeAreaRect.anchorMin = anchorMin;
                safeAreaRect.anchorMax = anchorMax;
                safeAreaRect.sizeDelta = Vector2.zero;
            }
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
        if (GameManager.Instance.isGameActive)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        scoreText.text = $"Score: {GameManager.Instance.currentScore}";
        timerText.text = $"Temps: {Mathf.CeilToInt(GameManager.Instance.timeRemaining)}";
    }

    private void UpdateWantedCharacter(CharacterCard character)
    {
        if (isRouletteRunning) return;
        StartCoroutine(WantedRouletteEffect(character));
    }

    private IEnumerator WantedRouletteEffect(CharacterCard finalCharacter)
    {
        isRouletteRunning = true;

        // Cacher uniquement la grille, garder le score/timer visible
        gridCanvas.gameObject.SetActive(false);
        
        // Mettre le jeu en pause
        GameManager.Instance.PauseGame();

        // Attendre que les cartes finissent leur animation actuelle
        yield return new WaitForSeconds(1f);

        // Cacher le panneau wanted
        wantedPanel.gameObject.SetActive(false);
        
        // Attendre un court instant
        yield return new WaitForSeconds(0.5f);

        // Activer et animer le panneau wanted en plein écran
        wantedPanel.gameObject.SetActive(true);
        wantedPanel.transform.DOScale(fullscreenScale, 0.5f);
        wantedPanel.DOAnchorPos(Vector2.zero, 0.5f);

        // Effet de roulette
        float elapsedTime = 0;
        while (elapsedTime < rouletteDuration)
        {
            // Choisir un sprite aléatoire
            Sprite randomSprite = GameManager.Instance.GetRandomSprite();
            wantedCharacterImage.sprite = randomSprite;

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
        wantedText.text = "WANTED!";
        AudioManager.Instance.PlayCorrect();  // Son final

        yield return new WaitForSeconds(0.5f);

        // Animation de transition vers la position finale en haut
        Sequence sequence = DOTween.Sequence();
        sequence.Join(wantedPanel.DOSizeDelta(finalWantedSize, 0.5f))
               .Join(wantedPanel.DOAnchorPos(finalWantedPosition, 0.5f))
               .Join(wantedPanel.transform.DOScale(1f, 0.5f));

        yield return sequence.WaitForCompletion();

        // Réactiver les éléments de jeu dans l'ordre
        gameInfoPanel.gameObject.SetActive(true);  // Réafficher le score/timer
        yield return new WaitForSeconds(0.3f);     // Petit délai

        // Animer l'apparition des cartes
        var gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            gridCanvas.gameObject.SetActive(true);
            gridManager.AnimateCardsEntry();
        }

        // À la fin, réactiver le jeu
        GameManager.Instance.ResumeGame();
        isRouletteRunning = false;
    }

    private void OnGameStart()
    {
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    private void OnGameOver()
    {
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        finalScoreText.text = $"Score Final: {GameManager.Instance.currentScore}";
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
} 