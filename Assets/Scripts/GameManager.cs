using UnityEngine;
using System.Collections;
using UnityEngine.Events;

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
    public int startingGridSize = 16; // 4x4 grid
    public float scorePerCorrectClick = 100f;
    
    [Header("Game State")]
    public float currentScore = 0f;
    public float timeRemaining;
    public CharacterCard wantedCharacter;
    public bool isGameActive = false;
    
    public UnityEvent onGameStart = new UnityEvent();
    public UnityEvent onGameOver = new UnityEvent();
    public UnityEvent<CharacterCard> onNewWantedCharacter = new UnityEvent<CharacterCard>();

    private System.Random random;

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

    public void StartGame()
    {
        currentScore = 0f;
        timeRemaining = roundDuration;
        isGameActive = true;
        onGameStart.Invoke();
        StartCoroutine(GameTimer());
    }

    private IEnumerator GameTimer()
    {
        while (timeRemaining > 0 && isGameActive)
        {
            timeRemaining -= Time.deltaTime;
            yield return null;
        }
        
        GameOver();
    }

    public void GameOver()
    {
        isGameActive = false;
        onGameOver.Invoke();
    }

    public void SelectNewWantedCharacter(CharacterCard character)
    {
        wantedCharacter = character;
        onNewWantedCharacter.Invoke(character);
    }

    public void AddScore()
    {
        currentScore += scorePerCorrectClick;
        StartNewRound();
    }

    public Sprite GetRandomSprite()
    {
        int colorIndex = random.Next(allCharacterSprites.Length);
        int expressionIndex = random.Next(allCharacterSprites[colorIndex].expressions.Length);
        return allCharacterSprites[colorIndex].expressions[expressionIndex];
    }

    public void StartNewRound()
    {
        if (currentScore > 500)
        {
            roundDuration = Mathf.Max(10f, roundDuration - 2f);
        }
        
        timeRemaining = roundDuration;
        
        onGameStart.Invoke();
    }
} 