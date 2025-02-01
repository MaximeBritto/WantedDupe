using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public float playAreaWidth = 1200f;    // Zone de jeu plus large
    public float playAreaHeight = 800f;    // Zone de jeu plus haute
    
    [Header("Prefabs")]
    public GameObject characterCardPrefab;
    
    [Header("References")]
    public RectTransform gridContainer;
    
    [Header("Roulette Settings")]
    public float rouletteDuration = 2f;    // Durée de l'effet roulette
    public float highlightDelay = 0.1f;    // Délai entre chaque highlight
    public float delayAfterSuccess = 1f;   // Délai après avoir trouvé le bon wanted

    [Header("Mobile Settings")]
    public float mobileCardScale = 0.8f;  // Échelle des cartes sur mobile
    public float mobileSpacing = 80f;     // Espacement pour mobile

    [Header("Card Settings")]
    public float minCardDistance = 100f;  // Distance minimale entre les cartes

    [Header("Board Settings")]
    public RectTransform gameBoardRect;  // Référence au RectTransform du GameBoard
    private float boardWidth;
    private float boardHeight;

    public List<CharacterCard> cards = new List<CharacterCard>();
    private CharacterCard wantedCard;

    public enum GridState
    {
        Aligned,            // Cartes alignées en ligne
        Columns,            // Cartes en colonnes
        Static,             // Cartes placées aléatoirement mais immobiles
        SlowMoving,         // Cartes en mouvement lent
        FastMoving,         // Cartes en mouvement rapide
        AlignedMoving,      // Cartes alignées qui se déplacent
        ColumnsMoving       // Colonnes qui se déplacent
    }

    [System.Serializable]
    public class DifficultyLevel
    {
        public int scoreThreshold;     // Score minimum pour ce niveau
        public int minCards;           // Nombre minimum de cartes
        public int maxCards;           // Nombre maximum de cartes
        public float moveSpeed;        // Vitesse de déplacement des cartes
        public GridState[] possibleStates;  // États possibles pour ce niveau
    }

    [Header("Difficulty Settings")]
    public DifficultyLevel[] difficultyLevels;  // Tu pourras configurer ça dans l'éditeur
    [SerializeField] private DifficultyLevel currentLevel;  // Sera visible dans l'Inspector
    [SerializeField] private GridState currentState;        // Sera visible dans l'Inspector

    [Header("Layout Settings")]
    public float cardSpacing = 100f;   // Espacement entre les cartes
    [Header("Column Settings")]
    public float columnSpacing = 150f;        // Espacement entre les colonnes
    public int maxColumns = 6;                // Nombre maximum de colonnes autorisé
    [Range(0.1f, 1f)]
    public float boardWidthUsage = 0.9f;      // Pourcentage de la largeur du board à utiliser (90% par défaut)
    [Range(0.1f, 1f)]
    public float boardHeightUsage = 0.9f;     // Pourcentage de la hauteur du board à utiliser

    private List<Sequence> activeSequences = new List<Sequence>();  // Pour garder trace des séquences actives

    private void Start()
    {
        GameManager.Instance.onGameStart.AddListener(InitializeGrid);
        GameManager.Instance.onScoreChanged.AddListener(OnScoreChanged);
        
        if (difficultyLevels == null || difficultyLevels.Length == 0)
        {
            Debug.LogError("Aucun niveau de difficulté configuré!");
        }
        
        // Vérifier la configuration du GridContainer
        if (gridContainer != null)
        {
            Debug.Log($"GridContainer size: {gridContainer.rect.width}x{gridContainer.rect.height}");
            Debug.Log($"GridContainer position: {gridContainer.position}");
            Debug.Log($"GridContainer anchoredPosition: {gridContainer.anchoredPosition}");
        }
        else
        {
            Debug.LogError("GridContainer non assigné!");
        }

        // Récupérer les dimensions du GameBoard
        if (gameBoardRect != null)
        {
            boardWidth = gameBoardRect.rect.width;
            boardHeight = gameBoardRect.rect.height;
        }
    }

    private void AdjustForMobileIfNeeded()
    {
        if (Application.isMobilePlatform)
        {
            // Ajuster la zone de jeu pour le format portrait
            playAreaWidth = Screen.width * 0.9f;
            playAreaHeight = Screen.height * 0.6f;
            
            // Ajuster l'espacement
            cardSpacing = mobileSpacing;
            columnSpacing = mobileSpacing * 1.2f;
            
            // Ajuster la taille des cartes
            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.transform.localScale = Vector3.one * mobileCardScale;
                }
            }
        }
    }

    private Vector2 GetValidCardPosition()
    {
        int maxAttempts = 50;
        int attempts = 0;
        
        while (attempts < maxAttempts)
        {
            Vector2 position = new Vector2(
                Random.Range(-playAreaWidth/2, playAreaWidth/2),
                Random.Range(-playAreaHeight/2, playAreaHeight/2)
            );

            // Vérifier la distance avec toutes les cartes existantes
            bool isValidPosition = true;
            foreach (var card in cards)
            {
                if (card == null) continue;
                
                RectTransform rectTransform = card.GetComponent<RectTransform>();
                float distance = Vector2.Distance(position, rectTransform.anchoredPosition);
                if (distance < minCardDistance)
                {
                    isValidPosition = false;
                    break;
                }
            }

            if (isValidPosition)
                return position;

            attempts++;
        }

        // Si on ne trouve pas de position valide, retourner une position par défaut
        return Vector2.zero;
    }

    public void InitializeGrid()
    {
        AdjustForMobileIfNeeded();
        
        // Mettre à jour le niveau de difficulté au début
        UpdateDifficultyLevel();
        
        // Nettoyer la grille existante
        foreach (var card in cards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        cards.Clear();

        // Utiliser le nombre de cartes du niveau actuel
        int numberOfCards = Random.Range(currentLevel.minCards, currentLevel.maxCards + 1);
        
        // Créer le wanted card avec un sprite aléatoire
        Sprite wantedSprite = GameManager.Instance.GetRandomSprite();
        List<Sprite> usedSprites = new List<Sprite>();
        usedSprites.Add(wantedSprite);

        // Créer toutes les cartes
        for (int i = 0; i < numberOfCards; i++)
        {
            GameObject cardObj = Instantiate(characterCardPrefab, gridContainer);
            CharacterCard card = cardObj.GetComponent<CharacterCard>();
            
            // Utiliser GetValidCardPosition pour éviter les superpositions
            Vector2 position = GetValidCardPosition();
            RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            
            Debug.Log($"Carte {i} créée à la position : {position}");

            Sprite cardSprite;
            if (i == 0)
            {
                cardSprite = wantedSprite;
                wantedCard = card;
            }
            else
            {
                do
                {
                    cardSprite = GameManager.Instance.GetRandomSprite();
                } while (cardSprite == wantedSprite);
                usedSprites.Add(cardSprite);
            }

            card.Initialize(i == 0 ? "Wanted" : $"Card_{i}", cardSprite);
            cards.Add(card);
        }

        Debug.Log($"Nombre total de cartes créées : {cards.Count}");

        // Mélanger l'ordre des cartes dans la hiérarchie
        ShuffleCardsOrder();

        // Important : définir la carte wanted dans le GameManager
        if (wantedCard == null)
        {
            Debug.LogError("Wanted Card is null!");
        }
        else
        {
            GameManager.Instance.SelectNewWantedCharacter(wantedCard);
        }

        // Ne pas afficher les cartes immédiatement
        foreach (var card in cards)
        {
            card.gameObject.SetActive(false);
        }
        
        // Les cartes seront activées par AnimateCardsEntry plus tard

        // Arranger les cartes selon l'état actuel
        ArrangeCardsBasedOnState();
    }

    private void ShuffleCardsOrder()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.SetSiblingIndex(Random.Range(0, cards.Count));
        }
    }

    public void CreateNewWanted()
    {
        UpdateDifficultyOnScoreChange();  // Mettre à jour la difficulté
        StartCoroutine(RouletteEffect());
    }

    private IEnumerator RouletteEffect()
    {
        // Attendre un peu après avoir trouvé le bon wanted
        yield return new WaitForSeconds(delayAfterSuccess);

        // Choisir le nouveau sprite wanted
        Sprite newWantedSprite = GameManager.Instance.GetRandomSprite();
        
        // Mettre à jour les cartes et le wanted
        int newWantedIndex = Random.Range(0, cards.Count);
        wantedCard = cards[newWantedIndex];
        wantedCard.Initialize("Wanted", newWantedSprite);

        // Mettre à jour les autres cartes
        for (int i = 0; i < cards.Count; i++)
        {
            if (i != newWantedIndex)
            {
                Sprite randomSprite;
                do
                {
                    randomSprite = GameManager.Instance.GetRandomSprite();
                } while (randomSprite == newWantedSprite);
                
                cards[i].Initialize($"Card_{i}", randomSprite);
            }
        }

        // Mettre à jour le GameManager (ceci déclenchera la roulette)
        GameManager.Instance.SelectNewWantedCharacter(wantedCard);
    }

    private void StartContinuousCardMovement(CharacterCard card)
    {
        RectTransform rectTransform = card.GetComponent<RectTransform>();
        
        // Créer une séquence d'animation infinie
        Sequence sequence = DOTween.Sequence();
        
        // Fonction pour obtenir une nouvelle position aléatoire
        Vector2 GetRandomPosition()
        {
            return new Vector2(
                Random.Range(-playAreaWidth/2, playAreaWidth/2),
                Random.Range(-playAreaHeight/2, playAreaHeight/2)
            );
        }

        // Configurer l'animation continue
        sequence
            .Append(rectTransform.DOAnchorPos(GetRandomPosition(), Random.Range(2f, 4f))
                .SetEase(Ease.InOutQuad))
            .AppendCallback(() => {
                // Chaque fois qu'une animation se termine, en démarrer une nouvelle
                rectTransform.DOAnchorPos(GetRandomPosition(), Random.Range(2f, 4f))
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() => StartContinuousCardMovement(card));
            });
    }

    public void AnimateCardsEntry()
    {
        Debug.Log($"Début AnimateCardsEntry avec {cards.Count} cartes");
        
        foreach (var card in cards)
        {
            if (card == null) continue;
            
            // Activer la carte
            card.gameObject.SetActive(true);
            card.transform.localScale = Vector3.zero;
            
            // Animation d'apparition de base
            card.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => {
                    // Appliquer l'arrangement selon l'état actuel
                    ArrangeCardsBasedOnState();
                });
        }
    }

    private void InitializeDifficultyLevels()
    {
        difficultyLevels = new DifficultyLevel[]
        {
            new DifficultyLevel {
                scoreThreshold = 0,
                minCards = 15,
                maxCards = 20,
                moveSpeed = 2f,
                possibleStates = new GridState[] { GridState.Aligned, GridState.Static }
            },
            new DifficultyLevel {
                scoreThreshold = 500,
                minCards = 18,
                maxCards = 24,
                moveSpeed = 3f,
                possibleStates = new GridState[] { GridState.Aligned, GridState.Columns, GridState.Static }
            },
            new DifficultyLevel {
                scoreThreshold = 1000,
                minCards = 20,
                maxCards = 28,
                moveSpeed = 4f,
                possibleStates = new GridState[] { GridState.Static, GridState.SlowMoving }
            },
            new DifficultyLevel {
                scoreThreshold = 2000,
                minCards = 24,
                maxCards = 32,
                moveSpeed = 5f,
                possibleStates = new GridState[] { GridState.SlowMoving, GridState.FastMoving }
            }
        };
    }

    private void UpdateDifficultyLevel()
    {
        float currentScore = GameManager.Instance.currentScore;
        
        // Trouver le niveau approprié
        currentLevel = difficultyLevels[0];
        foreach (var level in difficultyLevels)
        {
            if (currentScore >= level.scoreThreshold)
                currentLevel = level;
        }

        // Choisir un état aléatoire parmi ceux possibles
        currentState = currentLevel.possibleStates[Random.Range(0, currentLevel.possibleStates.Length)];
    }

    private void ArrangeCardsBasedOnState()
    {
        StopAllCardMovements();  // Arrêter TOUS les mouvements avant de changer d'état
        
        // Attendre une frame pour s'assurer que tous les mouvements sont arrêtés
        DOVirtual.DelayedCall(0.1f, () => {
            switch (currentState)
            {
                case GridState.Aligned:
                    ArrangeCardsInLine();
                    break;
                case GridState.Columns:
                    ArrangeCardsInColumns();
                    break;
                case GridState.Static:
                    ArrangeCardsRandomly(false);
                    break;
                case GridState.SlowMoving:
                    ArrangeCardsRandomly(true, currentLevel.moveSpeed);
                    break;
                case GridState.FastMoving:
                    ArrangeCardsRandomly(true, currentLevel.moveSpeed * 1.5f);
                    break;
                case GridState.AlignedMoving:
                    StartAlignedMovement();
                    break;
                case GridState.ColumnsMoving:
                    StartColumnsMovement();
                    break;
            }
        });
    }

    private void ArrangeCardsInLine()
    {
        int totalCards = cards.Count;
        
        // Calculer combien de cartes peuvent tenir sur une ligne
        float maxCardsPerRow = Mathf.Floor(boardWidth * 0.9f / cardSpacing);
        int rows = Mathf.CeilToInt(totalCards / maxCardsPerRow);
        int cardsPerRow = Mathf.CeilToInt(totalCards / rows);
        
        float startX = -(cardsPerRow * cardSpacing) / 2;
        float startY = (boardHeight / 2) - (rows * cardSpacing / 2);
        
        for (int i = 0; i < totalCards; i++)
        {
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            
            Vector2 targetPos = new Vector2(
                startX + (col * cardSpacing),
                startY - (row * cardSpacing)
            );
            
            RectTransform rectTransform = cards[i].GetComponent<RectTransform>();
            rectTransform.DOAnchorPos(targetPos, 0.5f)
                .SetEase(Ease.OutBack);
        }
    }

    private void ArrangeCardsInColumns()
    {
        int totalCards = cards.Count;
        
        // Utiliser directement maxColumns ou moins si l'espace ne le permet pas
        float maxPossibleColumns = Mathf.Floor(boardWidth * boardWidthUsage / columnSpacing);
        int columns = Mathf.Min(maxColumns, (int)maxPossibleColumns);
        
        // Forcer l'utilisation du nombre de colonnes défini
        int cardsPerColumn = Mathf.CeilToInt((float)totalCards / columns);
        
        // Calculer l'espacement vertical
        float verticalSpacing = Mathf.Min(cardSpacing, (boardHeight * boardHeightUsage) / cardsPerColumn);
        
        float startX = -(columns * columnSpacing) / 2;
        float startY = (boardHeight / 2) - (verticalSpacing / 2);

        // Distribuer les cartes de haut en bas, colonne par colonne
        int currentCard = 0;
        
        // Pour chaque colonne
        for (int col = 0; col < columns && currentCard < totalCards; col++)
        {
            // Remplir la colonne de haut en bas
            for (int row = 0; row < cardsPerColumn && currentCard < totalCards; row++)
            {
                Vector2 targetPos = new Vector2(
                    startX + (col * columnSpacing),
                    startY - (row * verticalSpacing)
                );
                
                RectTransform rectTransform = cards[currentCard].GetComponent<RectTransform>();
                rectTransform.anchoredPosition = targetPos;
                currentCard++;
            }
        }
    }

    private void ArrangeCardsRandomly(bool moving, float speed = 2f)
    {
        StopAllCardMovements(); // Arrêter les mouvements précédents
        
        foreach (var card in cards)
        {
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            Vector2 randomPos = GetValidCardPosition();
            
            if (moving)
            {
                StartContinuousCardMovement(card, speed);
            }
            else
            {
                rectTransform.DOAnchorPos(randomPos, 0.5f)
                    .SetEase(Ease.OutBack);
            }
        }
    }

    // Version modifiée de StartContinuousCardMovement avec vitesse paramétrable
    private void StartContinuousCardMovement(CharacterCard card, float speed)
    {
        RectTransform rectTransform = card.GetComponent<RectTransform>();
        
        // Arrêter les animations précédentes
        rectTransform.DOKill();
        
        // Calculer la durée en fonction de la vitesse
        float duration = 4f / speed;
        
        void StartNewMovement()
        {
            Vector2 targetPos = GetValidCardPosition();
            float distance = Vector2.Distance(rectTransform.anchoredPosition, targetPos);
            float adjustedDuration = distance / (speed * 100f); // Ajuster la durée selon la distance
            
            rectTransform.DOAnchorPos(targetPos, adjustedDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(StartNewMovement);
        }
        
        StartNewMovement();
    }

    // Méthode pour mettre à jour la difficulté quand le score change
    public void UpdateDifficultyOnScoreChange()
    {
        UpdateDifficultyLevel();
        ArrangeCardsBasedOnState();
        
        // Mettre à jour le texte de difficulté dans l'UI
        UIManager.Instance.UpdateDifficultyText(currentLevel.scoreThreshold, currentState);
    }

    // Méthode pour arrêter tous les mouvements
    public void StopAllCardMovements()
    {
        // Arrêter toutes les séquences actives
        foreach (var sequence in activeSequences)
        {
            if (sequence != null)
                sequence.Kill();
        }
        activeSequences.Clear();

        // Arrêter aussi les tweens individuels sur les cartes
        foreach (var card in cards)
        {
            if (card != null)
            {
                RectTransform rectTransform = card.GetComponent<RectTransform>();
                rectTransform.DOKill();
            }
        }
    }

    private void OnScoreChanged(float newScore)
    {
        UpdateDifficultyOnScoreChange();
    }

    private void StartAlignedMovement()
    {
        StopAllCardMovements();  // S'assurer que tout mouvement précédent est arrêté
        
        int totalCards = cards.Count;
        float availableWidth = playAreaWidth * 0.9f;
        int cardsPerRow = Mathf.FloorToInt(availableWidth / cardSpacing);
        int rows = Mathf.CeilToInt((float)totalCards / cardsPerRow);
        
        float startX = -playAreaWidth/2;
        float endX = playAreaWidth/2;
        float startY = (rows * cardSpacing) / 2;
        
        for (int i = 0; i < totalCards; i++)
        {
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            bool moveRight = row % 2 == 0;
            
            RectTransform rectTransform = cards[i].GetComponent<RectTransform>();
            float yPos = startY - (row * cardSpacing);
            float xPos = moveRight ? startX + (col * cardSpacing) : endX - (col * cardSpacing);
            rectTransform.anchoredPosition = new Vector2(xPos, yPos);
            
            // Utiliser directement la vitesse du niveau
            float speed = 100f * currentLevel.moveSpeed; // Base speed à 100 au lieu de 200
            
            Sequence sequence = DOTween.Sequence()
                .SetLoops(-1)
                .SetUpdate(true)
                .OnUpdate(() => {
                    float x = rectTransform.anchoredPosition.x;
                    x += moveRight ? speed * Time.deltaTime : -speed * Time.deltaTime;
                    
                    if (moveRight && x > endX)
                        x = startX;
                    else if (!moveRight && x < startX)
                        x = endX;
                    
                    rectTransform.anchoredPosition = new Vector2(x, yPos);
                });
            
            activeSequences.Add(sequence);  // Garder trace de la séquence
        }
    }

    private void StartColumnsMovement()
    {
        StopAllCardMovements();  // S'assurer que tout mouvement précédent est arrêté
        
        int totalCards = cards.Count;
        int columns = Mathf.CeilToInt(Mathf.Sqrt(totalCards));
        int cardsPerColumn = Mathf.CeilToInt((float)totalCards / columns);
        
        float startX = -(columns * columnSpacing) / 2;
        float startY = playAreaHeight/2;
        float endY = -playAreaHeight/2;
        
        for (int col = 0; col < columns; col++)
        {
            bool moveUp = col % 2 == 0;
            float xPos = startX + (col * columnSpacing);
            
            for (int i = 0; i < cardsPerColumn && (col * cardsPerColumn + i) < cards.Count; i++)
            {
                int cardIndex = col * cardsPerColumn + i;
                if (cardIndex >= cards.Count) break;
                
                RectTransform rectTransform = cards[cardIndex].GetComponent<RectTransform>();
                float yPos = moveUp ? 
                    startY - (i * cardSpacing) :
                    endY + (i * cardSpacing);
                
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                
                float speed = 100f * currentLevel.moveSpeed;
                Sequence sequence = DOTween.Sequence()
                    .SetLoops(-1)
                    .SetUpdate(true)
                    .OnUpdate(() => {
                        float y = rectTransform.anchoredPosition.y;
                        y += moveUp ? -speed * Time.deltaTime : speed * Time.deltaTime;
                        
                        if (moveUp && y < endY)
                            y = startY;
                        else if (!moveUp && y > startY)
                            y = endY;
                        
                        rectTransform.anchoredPosition = new Vector2(xPos, y);
                    });
                
                activeSequences.Add(sequence);
            }
        }
    }
} 