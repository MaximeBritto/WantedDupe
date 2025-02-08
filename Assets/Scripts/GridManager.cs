using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public bool useGameBoardSize = false;
    public float playAreaWidth = 1200f;
    public float playAreaHeight = 800f;

    [Header("Prefabs")]
    public GameObject characterCardPrefab;

    [Header("References")]
    public RectTransform gridContainer;

    [Header("Roulette Settings")]
    public float rouletteDuration = 2f;         // Durée de l'effet roulette avant d'afficher les cartes
    public float highlightDelay = 0.1f;
    public float delayAfterSuccess = 1f;          // Délai après un succès

    [Header("Mobile Settings")]
    public float mobileCardScale = 0.8f;
    public float mobileSpacing = 80f;

    [Header("Card Settings")]
    public float minCardDistance = 100f;

    [Header("Board Settings")]
    public RectTransform gameBoardRect;
    private float boardWidth;
    private float boardHeight;

    public List<CharacterCard> cards = new List<CharacterCard>();
    private CharacterCard wantedCard;

    /// <summary>
    /// Différents états possibles pour l'agencement et le mouvement des cartes.
    /// </summary>
    public enum GridState
    {
        Aligned,
        Columns,
        Static,
        SlowMoving,
        FastMoving,
        AlignedMoving,
        ColumnsMoving,
        CircularAligned,
        CircularAlignedMoving,
        PulsingMoving
    }

    [System.Serializable]
    public class DifficultyLevel
    {
        public int scoreThreshold;
        public int minCards;
        public int maxCards;
        public float moveSpeed;
        public GridState[] possibleStates;

        [Header("Column Specific Settings (Only used if state is Columns or ColumnsMoving)")]
        public int fixedColumns = 2;
        public float fixedColumnSpacing = 150f;

        [Header("Only One Color")]
        public bool onlyOneColor;
    }

    [Header("Difficulty Settings")]
    public DifficultyLevel[] difficultyLevels;
    [SerializeField] private DifficultyLevel currentLevel;
    [SerializeField] private GridState currentState;

    [Header("Layout Settings")]
    public float cardSpacing = 100f;

    [Header("Board Usage Settings")]
    [Range(0.1f, 1f)]
    public float boardWidthUsage = 0.9f;
    [Range(0.1f, 1f)]
    public float boardHeightUsage = 0.9f;

    [Header("Card Spacing")]
    public float horizontalSpacing = 150f;
    public float verticalSpacing = 200f;

    [Header("Circular Settings")]
    public int maxCardsPerCircle = 12;

    /// <summary>
    /// Liste de tous les Tweens actifs (Tweener ou Sequence).
    /// </summary>
    private List<Tween> activeTweens = new List<Tween>();

    private void Start()
    {
        // Écoute des événements du GameManager
        GameManager.Instance.onGameStart.AddListener(InitializeGrid);
        GameManager.Instance.onScoreChanged.AddListener(OnScoreChanged);

        if (difficultyLevels == null || difficultyLevels.Length == 0)
        {
            Debug.LogError("Aucun niveau de difficulté configuré !");
        }
        if (gridContainer == null)
        {
            Debug.LogError("GridContainer non assigné !");
        }
        if (gameBoardRect != null)
        {
            boardWidth = gameBoardRect.rect.width;
            boardHeight = gameBoardRect.rect.height;
        }
        if (useGameBoardSize && gameBoardRect != null)
        {
            playAreaWidth = gameBoardRect.rect.width;
            playAreaHeight = gameBoardRect.rect.height;
        }
    }

    /// <summary>
    /// Ajuste certains paramètres pour les plateformes mobiles.
    /// </summary>
    private void AdjustForMobileIfNeeded()
    {
        if (Application.isMobilePlatform)
        {
            playAreaWidth = Screen.width * 0.9f;
            playAreaHeight = Screen.height * 0.6f;
            cardSpacing = mobileSpacing;
            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.transform.localScale = Vector3.one * mobileCardScale;
                }
            }
        }
    }

    /// <summary>
    /// Retourne une position aléatoire dans le playArea en évitant les chevauchements.
    /// </summary>
    private Vector2 GetValidCardPosition()
    {
        int maxAttempts = 50;
        int attempts = 0;
        while (attempts < maxAttempts)
        {
            Vector2 position = new Vector2(
                Random.Range(-playAreaWidth / 2, playAreaWidth / 2),
                Random.Range(-playAreaHeight / 2, playAreaHeight / 2)
            );
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
        return Vector2.zero;
    }

    /// <summary>
    /// Initialise la grille : création des cartes, sélection du Wanted, etc.
    /// Dans le mode Only One Color, on détermine la couleur du Wanted et pour les cartes non‑Wanted on affecte cycliquement une expression différente (excluant celle du Wanted).
    /// </summary>
    public void InitializeGrid()
    {
        AdjustForMobileIfNeeded();
        UpdateDifficultyLevel();

        // Nettoyer les cartes existantes
        foreach (var card in cards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        cards.Clear();

        int numberOfCards = Random.Range(currentLevel.minCards, currentLevel.maxCards + 1);

        // Choix du sprite pour le Wanted
        Sprite wantedSprite = GameManager.Instance.GetRandomSprite();
        string wantedColor = null;
        Sprite[] availableSprites = null;
        if (currentLevel.onlyOneColor)
        {
            // Recherche dans le groupe correspondant
            foreach (var group in GameManager.Instance.allCharacterSprites)
            {
                if (System.Array.Exists(group.expressions, s => s == wantedSprite))
                {
                    wantedColor = group.characterColor;
                    // On retire le sprite du Wanted pour que les autres cartes aient d'autres expressions
                    availableSprites = group.expressions.Where(s => s != wantedSprite).ToArray();
                    break;
                }
            }
        }

        int nonWantedCounter = 0;
        for (int i = 0; i < numberOfCards; i++)
        {
            GameObject cardObj = Instantiate(characterCardPrefab, gridContainer);
            CharacterCard card = cardObj.GetComponent<CharacterCard>();
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchoredPosition = GetValidCardPosition();

            if (i == 0)
            {
                // La première carte est le Wanted
                card.Initialize("Wanted", wantedSprite);
                wantedCard = card;
            }
            else if (currentLevel.onlyOneColor && wantedColor != null && availableSprites != null && availableSprites.Length > 0)
            {
                // Affectation cyclique d'une expression différente pour les non‑Wanted
                Sprite spriteToUse = availableSprites[nonWantedCounter % availableSprites.Length];
                card.Initialize("Card_" + i, spriteToUse);
                nonWantedCounter++;
            }
            else
            {
                // Sélection d'un sprite différent de celui du Wanted
                Sprite randomSprite;
                do
                {
                    randomSprite = GameManager.Instance.GetRandomSprite();
                } while (randomSprite == wantedSprite);
                card.Initialize("Card_" + i, randomSprite);
            }
            cards.Add(card);
        }

        if (wantedCard == null)
        {
            Debug.LogError("Wanted Card is null!");
        }
        else
        {
            // On conserve le Wanted tel quel
            GameManager.Instance.SelectNewWantedCharacter(wantedCard);
            FilterCardsByColor(wantedCard);
            ArrangeCardsBasedOnState();
        }

        // Masquer initialement toutes les cartes
        foreach (var card in cards)
        {
            card.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Anime l'entrée (scale 0 -> 1) de toutes les cartes et, à la fin, les arrange.
    /// </summary>
    public void AnimateCardsEntry()
    {
        int cardsCompleted = 0;
        foreach (var card in cards)
        {
            if (card == null) continue;
            card.gameObject.SetActive(true);
            card.transform.localScale = Vector3.zero;
            card.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    cardsCompleted++;
                    if (cardsCompleted >= cards.Count)
                    {
                        ArrangeCardsBasedOnState();
                    }
                });
        }
    }

    /// <summary>
    /// Crée un nouveau Wanted après un succès.
    /// </summary>
    public void CreateNewWanted()
    {
        UpdateDifficultyOnScoreChange();
        StartCoroutine(RouletteEffect());
    }

    /// <summary>
    /// Coroutine de l'effet roulette pour choisir un nouveau Wanted.
    /// En mode Only One Color, le nouveau Wanted reçoit un sprite et les autres cartes reçoivent cycliquement une expression différente (excluant celle du Wanted).
    /// </summary>
    private IEnumerator RouletteEffect()
    {
        yield return new WaitForSeconds(delayAfterSuccess);

        // Sauvegarder l'ancien wanted pour la transition
        CharacterCard oldWanted = wantedCard;
        
        // Réinitialiser la grille avec le nouveau nombre de cartes selon le niveau actuel
        InitializeGrid();

        // Trouver le nouveau wanted dans la nouvelle grille
        wantedCard = cards.FirstOrDefault(c => c.name == "Wanted");
        
        if (wantedCard == null)
        {
            Debug.LogError("Pas de wanted trouvé après InitializeGrid!");
            yield break;
        }

        // Mise à jour du GameManager avec le nouveau wanted
        GameManager.Instance.SelectNewWantedCharacter(wantedCard);
        
        yield return new WaitForSeconds(delayAfterSuccess);
        
        // Reprendre le jeu après la roulette
        GameManager.Instance.ResumeGame();
    }

    /// <summary>
    /// Met à jour le niveau de difficulté et sélectionne un état d'agencement.
    /// </summary>
    private void UpdateDifficultyLevel()
    {
        float currentScore = GameManager.Instance.displayedScore;
        
        // Trouver le niveau de difficulté approprié
        DifficultyLevel newLevel = difficultyLevels[0];
        for (int i = difficultyLevels.Length - 1; i >= 0; i--)
        {
            if (currentScore >= difficultyLevels[i].scoreThreshold)
            {
                newLevel = difficultyLevels[i];
                break;
            }
        }

        // Mettre à jour le niveau et l'état
        currentLevel = newLevel;
        currentState = currentLevel.possibleStates[Random.Range(0, currentLevel.possibleStates.Length)];

        // Mettre à jour l'affichage UI du niveau si nécessaire
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateDifficultyText(currentLevel.scoreThreshold, currentState);
        }
    }

    /// <summary>
    /// Arrange ou anime les cartes selon l'état sélectionné.
    /// </summary>
    private void ArrangeCardsBasedOnState()
    {
        StopAllCardMovements();
        // Mélanger les cartes avant de les arranger
        ShuffleCards();
        
        DOVirtual.DelayedCall(0.1f, () =>
        {
            switch (currentState)
            {
                case GridState.Aligned:
                    ArrangeCardsInLine();
                    break;
                case GridState.Columns:
                    ArrangeCardsInColumns();
                    break;
                case GridState.CircularAligned:
                    ArrangeCardsInCircles();
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
                case GridState.CircularAlignedMoving:
                    StartCircularMovement();
                    break;
                case GridState.PulsingMoving:
                    StartPulsingMovement();
                    break;
            }
        });
    }

    #region Arrangements & Mouvements classiques

    private void ArrangeCardsInLine()
    {
        int totalCards = cards.Count;
        float availableWidth = playAreaWidth * 0.9f;
        int maxColumnsAllowed = Mathf.FloorToInt(availableWidth / horizontalSpacing);
        int cardsPerRow = Mathf.Min(totalCards, maxColumnsAllowed);
        int rows = Mathf.CeilToInt((float)totalCards / cardsPerRow);
        float dynamicHorizontalSpacing = (cardsPerRow > 1) ? availableWidth / (cardsPerRow - 1) : 0;
        float startX = -availableWidth / 2;
        float startY = (rows - 1) * verticalSpacing / 2;

        for (int i = 0; i < totalCards; i++)
        {
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            float xPos = startX + col * dynamicHorizontalSpacing;
            float yPos = startY - row * verticalSpacing;
            RectTransform rectTransform = cards[i].GetComponent<RectTransform>();
            Tween tween = rectTransform.DOAnchorPos(new Vector2(xPos, yPos), 0.5f)
                .SetEase(Ease.OutBack);
            activeTweens.Add(tween);
        }
    }

    private void ArrangeCardsInColumns()
    {
        int totalCards = cards.Count;
        int columns = currentLevel.fixedColumns;
        if (totalCards < columns)
            columns = totalCards;
        int cardsPerColumn = Mathf.CeilToInt((float)totalCards / columns);
        float totalColumnsWidth = (columns - 1) * currentLevel.fixedColumnSpacing;
        float startX = -totalColumnsWidth / 2f;
        float startY = playAreaHeight / 2 - verticalSpacing;

        int currentCard = 0;
        for (int col = 0; col < columns && currentCard < totalCards; col++)
        {
            float xPos = startX + col * currentLevel.fixedColumnSpacing;
            for (int row = 0; row < cardsPerColumn && currentCard < totalCards; row++)
            {
                RectTransform rectTransform = cards[currentCard].GetComponent<RectTransform>();
                float yPos = startY - row * verticalSpacing;
                Tween tween = rectTransform.DOAnchorPos(new Vector2(xPos, yPos), 0.5f)
                    .SetEase(Ease.OutBack);
                activeTweens.Add(tween);
                currentCard++;
            }
        }
    }

    private void ArrangeCardsRandomly(bool moving, float speed = 2f)
    {
        StopAllCardMovements();
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
                Tween tween = rectTransform.DOAnchorPos(randomPos, 0.5f)
                    .SetEase(Ease.OutBack);
                activeTweens.Add(tween);
            }
        }
    }

    private void StartAlignedMovement()
    {
        StopAllCardMovements();
        int totalCards = cards.Count;
        float availableWidth = playAreaWidth * 0.9f;
        int cardsPerRow = Mathf.FloorToInt(availableWidth / horizontalSpacing);
        int rows = Mathf.CeilToInt((float)totalCards / cardsPerRow);
        float startX = -availableWidth / 2;
        float startY = (rows * verticalSpacing) / 2;

        for (int i = 0; i < totalCards; i++)
        {
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            bool moveRight = row % 2 == 0;
            float rowOffset = 0;
            if (row == rows - 1 && totalCards % cardsPerRow != 0)
            {
                int cardsInLastRow = totalCards % cardsPerRow;
                rowOffset = (cardsPerRow - cardsInLastRow) * horizontalSpacing / 2;
            }
            RectTransform rectTransform = cards[i].GetComponent<RectTransform>();
            float yPos = startY - (row * verticalSpacing);
            float xPos = moveRight
                ? startX + (col * horizontalSpacing) + rowOffset
                : -startX - (col * horizontalSpacing) - rowOffset;
            rectTransform.anchoredPosition = new Vector2(xPos, yPos);

            float moveSpeed = 100f * currentLevel.moveSpeed;
            Sequence sequence = DOTween.Sequence()
                .SetLoops(-1)
                .SetUpdate(true)
                .OnUpdate(() =>
                {
                    float x = rectTransform.anchoredPosition.x;
                    x += moveRight ? moveSpeed * Time.deltaTime : -moveSpeed * Time.deltaTime;
                    if (moveRight && x > -startX)
                        x = startX;
                    else if (!moveRight && x < startX)
                        x = -startX;
                    rectTransform.anchoredPosition = new Vector2(x, yPos);
                });
            activeTweens.Add(sequence);
        }
    }

    private void StartColumnsMovement()
    {
        StopAllCardMovements();
        int totalCards = cards.Count;
        int columns = currentLevel.fixedColumns;
        if (totalCards < columns)
            columns = totalCards;
        int maxRows = Mathf.CeilToInt((float)totalCards / columns);
        float totalTravel = (maxRows - 1) * verticalSpacing;
        float highestY = totalTravel / 2f;
        float lowestY = -highestY;
        float totalColumnsWidth = (columns - 1) * currentLevel.fixedColumnSpacing;
        float startX = -totalColumnsWidth / 2f;

        List<List<RectTransform>> columnsList = new List<List<RectTransform>>();
        int currentCard = 0;
        for (int col = 0; col < columns; col++)
        {
            columnsList.Add(new List<RectTransform>());
            for (int row = 0; row < maxRows && currentCard < totalCards; row++)
            {
                float xPos = startX + col * currentLevel.fixedColumnSpacing;
                RectTransform rectTransform = cards[currentCard].GetComponent<RectTransform>();
                int expectedCount = (totalCards % columns > col) ? maxRows : maxRows - 1;
                float gap = ((float)(maxRows - expectedCount)) * verticalSpacing / 2f;
                float initialY = highestY - gap - (row * verticalSpacing);
                rectTransform.anchoredPosition = new Vector2(xPos, initialY);
                columnsList[col].Add(rectTransform);
                currentCard++;
            }
        }

        for (int col = 0; col < columns; col++)
        {
            int capturedCol = col;
            bool moveDown = (capturedCol % 2 == 0);
            float speed = 100f * currentLevel.moveSpeed;
            Sequence seq = DOTween.Sequence()
                .SetLoops(-1)
                .SetUpdate(true)
                .OnUpdate(() =>
                {
                    float offset = Time.deltaTime * speed;
                    foreach (var rectTransform in columnsList[capturedCol])
                    {
                        Vector2 pos = rectTransform.anchoredPosition;
                        if (moveDown)
                        {
                            pos.y -= offset;
                            if (pos.y < lowestY) pos.y = highestY;
                        }
                        else
                        {
                            pos.y += offset;
                            if (pos.y > highestY) pos.y = lowestY;
                        }
                        rectTransform.anchoredPosition = pos;
                    }
                });
            activeTweens.Add(seq);
        }
    }

    private void ArrangeCardsInCircles()
    {
        StopAllCardMovements();
        int totalCards = cards.Count;
        float centerX = 0;
        float centerY = 0;
        float cardSize = horizontalSpacing * 0.8f;
        float maxRadius = Mathf.Min(playAreaWidth, playAreaHeight) * 0.45f;
        float baseRadius = cardSize * 2;
        float radiusIncrement = cardSize * 1.5f;
        List<int> cardsPerCircle = new List<int>();
        int remainingCards = totalCards;
        int currentCircle = 0;
        while (remainingCards > 0 && baseRadius + (currentCircle * radiusIncrement) <= maxRadius)
        {
            float currentRadius = baseRadius + (currentCircle * radiusIncrement);
            float circumference = 2 * Mathf.PI * currentRadius;
            int maxCardsInCircle = Mathf.FloorToInt(circumference / cardSize);
            int cardsInThisCircle = Mathf.Min(maxCardsInCircle, remainingCards);
            cardsPerCircle.Add(cardsInThisCircle);
            remainingCards -= cardsInThisCircle;
            currentCircle++;
        }
        if (remainingCards > 0 && cardsPerCircle.Count > 0)
        {
            cardsPerCircle[cardsPerCircle.Count - 1] += remainingCards;
        }
        PlaceCardsInCircles(cardsPerCircle, baseRadius, radiusIncrement, centerX, centerY, false);
    }

    private void StartCircularMovement()
    {
        StopAllCardMovements();
        int totalCards = cards.Count;
        float centerX = 0;
        float centerY = 0;
        float cardSize = horizontalSpacing * 0.8f;
        float extraSpacing = 20f;
        float baseRadius = cardSize * 2 + extraSpacing;
        int numCircles = Mathf.CeilToInt((float)totalCards / maxCardsPerCircle);
        float maxAllowedRadius = Mathf.Min(playAreaWidth, playAreaHeight) * 0.45f;
        float radiusIncrement = (numCircles > 1)
            ? (maxAllowedRadius - baseRadius) / (numCircles - 1)
            : 0;
        List<int> cardsPerCircle = new List<int>();
        int remainingCards = totalCards;
        for (int i = 0; i < numCircles; i++)
        {
            int cardsInThisCircle = Mathf.CeilToInt((float)remainingCards / (numCircles - i));
            cardsPerCircle.Add(cardsInThisCircle);
            remainingCards -= cardsInThisCircle;
        }
        PlaceCardsInCircles(cardsPerCircle, baseRadius, radiusIncrement, centerX, centerY, true);
    }

    private void PlaceCardsInCircles(List<int> cardsPerCircle, float baseRadius, float radiusIncrement,
        float centerX, float centerY, bool enableRotation)
    {
        int cardIndex = 0;
        for (int circle = 0; circle < cardsPerCircle.Count; circle++)
        {
            float radius = baseRadius + (circle * radiusIncrement);
            int cardsInCircle = cardsPerCircle[circle];
            for (int i = 0; i < cardsInCircle; i++)
            {
                if (cardIndex >= cards.Count) break;
                float angle = (i * 2 * Mathf.PI) / cardsInCircle;
                float xPos = centerX + radius * Mathf.Cos(angle);
                float yPos = centerY + radius * Mathf.Sin(angle);
                RectTransform rectTransform = cards[cardIndex].GetComponent<RectTransform>();
                Tween moveTween = rectTransform.DOAnchorPos(new Vector2(xPos, yPos), 0.5f)
                    .SetEase(Ease.OutBack);
                activeTweens.Add(moveTween);
                if (enableRotation)
                {
                    float rotationSpeed = 100f * currentLevel.moveSpeed * (circle % 2 == 0 ? 1 : -1);
                    Sequence seq = DOTween.Sequence()
                        .SetLoops(-1)
                        .SetUpdate(true)
                        .OnUpdate(() =>
                        {
                            angle += rotationSpeed * Time.deltaTime * Mathf.Deg2Rad;
                            float newX = centerX + radius * Mathf.Cos(angle);
                            float newY = centerY + radius * Mathf.Sin(angle);
                            rectTransform.anchoredPosition = new Vector2(newX, newY);
                        });
                    activeTweens.Add(seq);
                }
                cardIndex++;
            }
        }
    }

    private void StartPulsingMovement()
    {
        StopAllCardMovements();
        foreach (var card in cards)
        {
            StartContinuousCardMovement(card, currentLevel.moveSpeed);
            if (card != wantedCard)
            {
                card.transform.localScale = Vector3.one;
                float randomTargetScale = Random.Range(0.8f, 3f);
                float randomDuration = Random.Range(0.5f, 1.5f);
                float randomDelay = Random.Range(0f, 1f);
                Tween pulseTween = card.transform.DOScale(randomTargetScale, randomDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetDelay(randomDelay);
                activeTweens.Add(pulseTween);
            }
        }
    }

    private void StartContinuousCardMovement(CharacterCard card, float speed)
    {
        RectTransform rectTransform = card.GetComponent<RectTransform>();
        rectTransform.DOKill();
        void StartNewMovement()
        {
            Vector2 targetPos = GetValidCardPosition();
            float distance = Vector2.Distance(rectTransform.anchoredPosition, targetPos);
            float adjustedDuration = distance / (speed * 100f);
            rectTransform.DOAnchorPos(targetPos, adjustedDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(StartNewMovement);
        }
        StartNewMovement();
    }

    public void StopAllCardMovements()
    {
        foreach (var tween in activeTweens)
        {
            tween?.Kill();
        }
        activeTweens.Clear();
        foreach (var card in cards)
        {
            if (card != null)
            {
                RectTransform rectTransform = card.GetComponent<RectTransform>();
                rectTransform.DOKill();
            }
        }
    }

    /// <summary>
    /// Désactive toutes les cartes sauf le Wanted.
    /// </summary>
    public void HideAllButWanted()
    {
        StopAllCardMovements();
        foreach (var card in cards)
        {
            if (card == null) continue;
            if (card == wantedCard) continue;
            card.gameObject.SetActive(false);
        }
    }

    private void OnScoreChanged(float newScore)
    {
        UpdateDifficultyOnScoreChange();
    }

    public void UpdateDifficultyOnScoreChange()
    {
        UpdateDifficultyLevel();
        ArrangeCardsBasedOnState();
        FilterCardsByColor(wantedCard);
    }

    private void FilterCardsByColor(CharacterCard wantedCard)
    {
        if (currentLevel.onlyOneColor && wantedCard != null)
        {
            // Mode One Color : on filtre par couleur
            foreach (var group in GameManager.Instance.allCharacterSprites)
            {
                if (System.Array.Exists(group.expressions, s => s == wantedCard.characterSprite))
                {
                    // On active seulement les cartes de la même couleur
                    foreach (var card in cards)
                    {
                        bool sameColor = System.Array.Exists(group.expressions, s => s == card.characterSprite);
                        card.gameObject.SetActive(sameColor);
                    }
                    break;
                }
            }
        }
        else
        {
            // Mode normal : on active toutes les cartes
            foreach (var card in cards)
            {
                card.gameObject.SetActive(true);
            }
        }
    }

    // Correction : Méthode GetAverageColor protégée par try/catch pour éviter les erreurs si la texture n'est pas lisible
    private Color GetAverageColor(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogWarning("GetAverageColor : La texture est null.");
            return Color.white;
        }
        try
        {
            Color32[] pixels = texture.GetPixels32();
            int totalR = 0, totalG = 0, totalB = 0;
            foreach (Color32 pixel in pixels)
            {
                totalR += pixel.r;
                totalG += pixel.g;
                totalB += pixel.b;
            }
            int pixelCount = pixels.Length;
            return new Color(totalR / (255f * pixelCount),
                             totalG / (255f * pixelCount),
                             totalB / (255f * pixelCount));
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Erreur dans GetAverageColor sur la texture '" + texture.name +
                           "'. Assurez-vous que 'Read/Write Enabled' est activé. " + ex.Message);
            return Color.white;
        }
    }

    private float GetColorDifference(Color c1, Color c2)
    {
        return Mathf.Abs(c1.r - c2.r) +
               Mathf.Abs(c1.g - c2.g) +
               Mathf.Abs(c1.b - c2.b);
    }

    private void ShuffleCards()
    {
        // Mélanger la liste des cartes
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            var temp = cards[i];
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }
    #endregion
}
