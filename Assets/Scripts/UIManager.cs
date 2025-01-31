using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
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
        Debug.Log($"Mise à jour du Wanted: {character.characterName}");
        wantedCharacterImage.sprite = character.characterSprite;
        wantedText.text = "WANTED!";
    }

    private void OnGameStart()
    {
        Debug.Log("OnGameStart appelé");
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
        Debug.Log("Démarrage du jeu");
        GameManager.Instance.StartGame();
        Debug.Log($"Jeu actif: {GameManager.Instance.isGameActive}");
    }
} 