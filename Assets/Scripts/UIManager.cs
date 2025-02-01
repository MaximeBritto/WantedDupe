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

    [Header("Wanted Panel Sizes")]
    public Vector2 finalWantedPosition = new Vector2(0, -100);  // Position finale en haut
    public Vector2 finalWantedSize = new Vector2(300, 400);     // Taille finale
    public Vector2 rouletteWantedSize = new Vector2(500, 600);  // Taille pendant la roulette
    public Vector2 roulettePosition = new Vector2(0, 0);        // Position pendant la roulette
    public float rouletteScale = 1.2f;                          // Échelle pendant la roulette
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
        
        // Ne pas activer le panel ici, laissez WantedRouletteEffect le faire
        StartCoroutine(WantedRouletteEffect(character));
    }

    private IEnumerator WantedRouletteEffect(CharacterCard finalCharacter)
    {
        isRouletteRunning = true;
        
        // Désactiver temporairement la grille
        gridCanvas.gameObject.SetActive(false);
        
        // Animation de descente et agrandissement
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
        AudioManager.Instance.PlayCorrect();

        yield return new WaitForSeconds(0.5f);

        // Animation de remontée et rétrécissement
        Sequence endSequence = DOTween.Sequence();
        endSequence.Join(wantedPanel.DOAnchorPos(finalWantedPosition, 0.5f))
                  .Join(wantedPanel.DOSizeDelta(finalWantedSize, 0.5f))
                  .Join(wantedPanel.transform.DOScale(1f, 0.5f));

        yield return endSequence.WaitForCompletion();

        // Réactiver la grille et les cartes
        gridCanvas.gameObject.SetActive(true);
        
        // Réactiver et animer les cartes
        var gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            gridManager.AnimateCardsEntry();  // Cette méthode réactive et anime les cartes
        }

        GameManager.Instance.ResumeGame();
        isRouletteRunning = false;
    }

    private void OnGameStart()
    {
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        // Réinitialiser le WantedPanel à sa taille normale
        if (wantedPanel != null)
        {
            wantedPanel.transform.localScale = Vector3.one;
            wantedPanel.sizeDelta = finalWantedSize;
            wantedPanel.anchoredPosition = finalWantedPosition;
        }
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

    private void ConfigureForPortrait()
    {
        // Ajuster le wanted
        finalWantedSize = mobileWantedSize;
        finalWantedPosition = mobileWantedPosition;
        
        // Ajuster la taille des textes
        if (scoreText != null) scoreText.fontSize = 40;
        if (timerText != null) timerText.fontSize = 40;
        if (wantedText != null) wantedText.fontSize = 36;
    }
} 