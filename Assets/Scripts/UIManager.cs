using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Wanted Panel")]
    public Image wantedCharacterImage;
    public TextMeshProUGUI wantedText;
    
    [Header("Game Info")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    
    [Header("Panels")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public Button startGameButton;

    private void Start()
    {
        GameManager.Instance.onGameStart.AddListener(OnGameStart);
        GameManager.Instance.onGameOver.AddListener(OnGameOver);
        GameManager.Instance.onNewWantedCharacter.AddListener(UpdateWantedCharacter);
        
        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);
            
        gameOverPanel.SetActive(false);
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
        wantedCharacterImage.sprite = character.characterSprite;
        wantedText.text = "WANTED!";
    }

    private void OnGameStart()
    {
        gameOverPanel.SetActive(false);
    }

    private void OnGameOver()
    {
        gameOverPanel.SetActive(true);
        finalScoreText.text = $"Score Final: {GameManager.Instance.currentScore}";
    }

    private void StartGame()
    {
        GameManager.Instance.StartGame();
    }
} 