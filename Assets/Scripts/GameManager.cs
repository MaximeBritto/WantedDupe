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
    public UnityEvent<float> onScoreChanged = new UnityEvent<float>();

    [Header("Timer Settings")]
    public float maxTime = 40f;       // Temps maximum possible
    public float penaltyTime = 5f;    // Temps retiré pour une mauvaise carte

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

    public void StartGame()
    {
        // Jouer le son du bouton
        AudioManager.Instance?.PlayButtonSound();
        // Démarrer la musique de fond
        AudioManager.Instance?.StartBackgroundMusic();
        
        currentScore = 0f;
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
                // Mettre à jour la vitesse de la musique
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
        // Arrêter la musique
        AudioManager.Instance?.StopBackgroundMusic();
        
        // Révéler la position du wanted en faisant disparaître les autres cartes
        StartCoroutine(RevealWantedPosition());
    }

    private IEnumerator RevealWantedPosition()
    {
        // Faire disparaître toutes les cartes sauf le wanted
        var gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            foreach (var card in gridManager.cards)
            {
                if (card != wantedCharacter)
                {
                    // Animation de disparition
                    card.transform.DOScale(0f, 0.3f)
                        .SetEase(Ease.InBack);
                }
                else
                {
                    // Mettre en évidence le wanted
                    card.transform.DOScale(1.2f, 0.5f)
                        .SetEase(Ease.OutElastic);
                    
                    // Optionnel : faire clignoter le wanted
                    var image = card.GetComponent<Image>();
                    if (image != null)
                    {
                        DOTween.Sequence()
                            .Append(image.DOColor(Color.yellow, 0.3f))
                            .Append(image.DOColor(Color.white, 0.3f))
                            .SetLoops(3);
                    }
                }
            }
        }

        // Attendre que les animations soient terminées
        yield return new WaitForSeconds(2f);

        // Afficher le game over
        onGameOver.Invoke();
    }

    public void SelectNewWantedCharacter(CharacterCard character)
    {
        // Jouer le son de sélection
        AudioManager.Instance?.PlayWantedSelection();
        
        wantedCharacter = character;
        onNewWantedCharacter.Invoke(character);
    }

    public void AddScore()
    {
        currentScore += scorePerCorrectClick;
        onScoreChanged.Invoke(currentScore);
        
        // Ajouter 5 secondes, mais permettre de dépasser le temps initial
        timeRemaining = Mathf.Min(timeRemaining + 5f, maxTime);
        
        // Créer un nouveau wanted
        var gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            PauseGame(); // Pause pendant la roulette
            gridManager.CreateNewWanted();
        }
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
        if (currentScore > 500)
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
} 