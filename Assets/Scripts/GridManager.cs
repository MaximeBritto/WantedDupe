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
    public Transform gameBoardTransform; // Référence au Transform du GameBoard

    [Header("Roulette Settings")]
    public float rouletteDuration = 2f;         // Durée de l'effet roulette
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

    // Dimensions en dur pour Aligned Movement et Column Movement
    private const float FIXED_PLAY_AREA_WIDTH = 1107.2f;
    private const float FIXED_PLAY_AREA_HEIGHT = 1475.1f;

    public List<CharacterCard> cards = new List<CharacterCard>();
    public CharacterCard wantedCard { get; private set; }

    // Variable qui détermine si le mode Only One Color est actif
    private bool onlyOneColorActive = false;

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

    // Liste des tweens actifs
    private List<Tween> activeTweens = new List<Tween>();

    private void Start()
    {
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
        // En cas d'échec, retourne une position aléatoire
        return new Vector2(
            Random.Range(-playAreaWidth / 2, playAreaWidth / 2),
            Random.Range(-playAreaHeight / 2, playAreaHeight / 2)
        );
    }

    public void InitializeGrid()
    {
        AdjustForMobileIfNeeded();
        UpdateDifficultyLevel();

        // Détermine aléatoirement si le mode Only One Color sera actif
        onlyOneColorActive = currentLevel.onlyOneColor && (Random.value < 0.5f);

        // Détermine la transform parent à utiliser (GameBoard si disponible, sinon gridContainer)
        Transform parentTransform = gameBoardTransform != null ? gameBoardTransform : gridContainer;

        // Détruire les cartes existantes
        foreach (var existingCard in cards)
        {
            if (existingCard != null)
                Destroy(existingCard.gameObject);
        }
        cards.Clear();

        int numberOfCards = Random.Range(currentLevel.minCards, currentLevel.maxCards + 1);

        // Choix du sprite pour le wanted
        Sprite wantedSprite = GameManager.Instance.GetRandomSprite();
        string wantedColor = null;
        Sprite[] availableSprites = null;
        if (onlyOneColorActive)
        {
            foreach (var group in GameManager.Instance.allCharacterSprites)
            {
                if (System.Array.Exists(group.expressions, s => s == wantedSprite))
                {
                    wantedColor = group.characterColor;
                    availableSprites = group.expressions.Where(s => s != wantedSprite).ToArray();
                    break;
                }
            }
        }

        // Création du wanted
        GameObject wantedObj = Instantiate(characterCardPrefab, parentTransform);
        // On n'utilise plus le nom du GameObject pour identifier le wanted (on utilisera la propriété characterName)
        CharacterCard wantedCardComponent = wantedObj.GetComponent<CharacterCard>();
        RectTransform wantedRt = wantedObj.GetComponent<RectTransform>();
        wantedRt.anchoredPosition = GetValidCardPosition();
        // Initialise le wanted avec "Wanted" comme characterName
        wantedCardComponent.Initialize("Wanted", wantedSprite);
        // Place le wanted au-dessus des autres cartes
        wantedObj.transform.SetAsLastSibling();
        wantedCard = wantedCardComponent;
        cards.Add(wantedCardComponent);

        // Création des autres cartes
        for (int i = 1; i < numberOfCards; i++)
        {
            GameObject cardObj = Instantiate(characterCardPrefab, parentTransform);
            CharacterCard cardComponent = cardObj.GetComponent<CharacterCard>();
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchoredPosition = GetValidCardPosition();

            if (onlyOneColorActive && wantedColor != null && availableSprites != null && availableSprites.Length > 0)
            {
                Sprite spriteToUse = availableSprites[Random.Range(0, availableSprites.Length)];
                cardComponent.Initialize("Card_" + i, spriteToUse);
            }
            else
            {
                Sprite randomSprite;
                do
                {
                    randomSprite = GameManager.Instance.GetRandomSprite();
                } while (randomSprite == wantedSprite);
                cardComponent.Initialize("Card_" + i, randomSprite);
            }
            cards.Add(cardComponent);
        }

        if (wantedCard == null)
        {
            Debug.LogError("Pas de wanted trouvé après InitializeGrid!");
            return;
        }

        GameManager.Instance.SelectNewWantedCharacter(wantedCard);
        FilterCardsByColor(wantedCard);
        ArrangeCardsBasedOnState();

        // Masquer toutes les cartes pour préparer l'animation d'entrée
        foreach (var c in cards)
        {
            c.gameObject.SetActive(false);
        }
    }

    public void AnimateCardsEntry()
    {
        // Lance l'animation d'entrée de toutes les cartes simultanément
        foreach (var card in cards)
        {
            if (card == null) continue;
            card.gameObject.SetActive(true);
            card.transform.localScale = Vector3.zero;
            card.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
        // Après 0.3s, réarranger les cartes
        DOVirtual.DelayedCall(0.3f, () =>
        {
            ArrangeCardsBasedOnState();
        });
    }

    public void CreateNewWanted()
    {
        StartCoroutine(HideCardsAndStartRoulette());
    }

    private IEnumerator HideCardsAndStartRoulette()
    {
        // Faire disparaître toutes les cartes sauf le wanted
        foreach (var card in cards)
        {
            if (card != null && card != wantedCard)
            {
                card.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
            }
        }
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(RouletteEffect());
    }

    private IEnumerator RouletteEffect()
    {
        
        yield return new WaitForSeconds(delayAfterSuccess);

        UpdateDifficultyOnScoreChange();
        InitializeGrid();

        // Recherche le nouveau wanted par la propriété characterName
        wantedCard = cards.FirstOrDefault(c => c.characterName == "Wanted");
        if (wantedCard == null)
        {
            Debug.LogError("Pas de wanted trouvé après InitializeGrid!");
            yield break;
        }
        wantedCard.transform.SetAsLastSibling();
        GameManager.Instance.SelectNewWantedCharacter(wantedCard);

        // IMPORTANT : on ne lance plus AnimateCardsEntry() ici.
        // On attend simplement un court délai, puis on reprend le jeu.
        yield return new WaitForSeconds(0.5f);

    }

    private void UpdateDifficultyLevel()
    {
        float currentScore = GameManager.Instance.internalScore;
        DifficultyLevel newLevel = difficultyLevels[0];
        for (int i = difficultyLevels.Length - 1; i >= 0; i--)
        {
            if (currentScore >= difficultyLevels[i].scoreThreshold)
            {
                newLevel = difficultyLevels[i];
                break;
            }
        }
        currentLevel = newLevel;
        currentState = currentLevel.possibleStates[Random.Range(0, currentLevel.possibleStates.Length)];
        
        // Appliquer les dimensions spécifiques à l'état actuel
        ApplyStateSpecificDimensions(currentState);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateDifficultyText(currentLevel.scoreThreshold, currentState);
        }
    }

    private void ArrangeCardsBasedOnState()
    {
        StopAllCardMovements();
        ShuffleCards();
        
        // Appliquer les dimensions temporaires selon le mode
        ApplyStateSpecificDimensions(currentState);
        
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

    // Nouvelle méthode pour gérer les dimensions selon l'état
    private void ApplyStateSpecificDimensions(GridState state)
    {
        // Si l'état est lié à Aligned ou Columns (moving ou non), utiliser les dimensions en dur
        if (state == GridState.AlignedMoving || state == GridState.ColumnsMoving)
        {
            playAreaWidth = FIXED_PLAY_AREA_WIDTH;
            playAreaHeight = FIXED_PLAY_AREA_HEIGHT;
        }
        else
        {
            // Pour tous les autres états, restaurer les dimensions du GameBoard
            if (useGameBoardSize && gameBoardRect != null)
            {
                playAreaWidth = gameBoardRect.rect.width;
                playAreaHeight = gameBoardRect.rect.height;
            }
            else
            {
                // Si l'option useGameBoardSize n'est pas activée, utiliser les valeurs par défaut
                // Ces valeurs sont celles définies dans l'inspecteur ou celles modifiées par d'autres scripts
                // Elles seront déjà à jour, donc pas besoin de les changer
            }
        }
    }

    #region Arrangements & Mouvements

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
        
        // Distribuer les cartes uniformément entre les colonnes
        int[] cardsPerColumn = new int[columns];
        int remainingCards = totalCards;
        
        // Distribuer d'abord le minimum de cartes par colonne
        int minCardsPerColumn = remainingCards / columns;
        for (int i = 0; i < columns; i++)
        {
            cardsPerColumn[i] = minCardsPerColumn;
            remainingCards -= minCardsPerColumn;
        }
        
        // Distribuer les cartes restantes une par une
        for (int i = 0; i < remainingCards; i++)
        {
            cardsPerColumn[i]++;
        }
        
        // Calculer la hauteur maximale nécessaire pour une colonne
        int maxCardsInAnyColumn = cardsPerColumn.Max();
        
        // Utiliser playAreaHeight au lieu de calculer seulement basé sur le nombre de cartes
        float usableHeight = playAreaHeight * 0.9f; // Utiliser 90% de la hauteur disponible
        
        // Garantir que l'espacement est suffisant pour le nombre de cartes
        float actualVerticalSpacing = verticalSpacing;
        if ((maxCardsInAnyColumn - 1) * verticalSpacing > usableHeight)
        {
            actualVerticalSpacing = usableHeight / (maxCardsInAnyColumn - 1);
        }
        
        float highestY = usableHeight / 2f;
        float lowestY = -highestY;
        float totalColumnsWidth = (columns - 1) * currentLevel.fixedColumnSpacing;
        float startX = -totalColumnsWidth / 2f;

        List<List<RectTransform>> columnsList = new List<List<RectTransform>>();
        int currentCardIndex = 0;
        
        // Positionner les cartes dans chaque colonne
        for (int col = 0; col < columns; col++)
        {
            columnsList.Add(new List<RectTransform>());
            float xPos = startX + col * currentLevel.fixedColumnSpacing;
            int cardsInThisColumn = cardsPerColumn[col];
            
            // Calculer l'espace total occupé par cette colonne
            float columnHeight = (cardsInThisColumn - 1) * actualVerticalSpacing;
            float columnStartY = columnHeight / 2f;
            
            for (int row = 0; row < cardsInThisColumn; row++)
            {
                RectTransform rectTransform = cards[currentCardIndex].GetComponent<RectTransform>();
                // Positionner uniformément depuis le haut
                float initialY = columnStartY - (row * actualVerticalSpacing);
                rectTransform.anchoredPosition = new Vector2(xPos, initialY);
                columnsList[col].Add(rectTransform);
                currentCardIndex++;
            }
        }

        // Configurer l'animation de déplacement
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
        // Si l'utilisateur vient de cliquer sur une mauvaise carte, ne pas arrêter les mouvements
        if (GameManager.Instance != null && GameManager.Instance.justClickedWrongCard)
        {
            // Simplement retourner sans rien faire pour maintenir le mouvement des cartes
            return;
        }
        
        // Sinon, arrêter tous les mouvements comme d'habitude
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
                if (rectTransform != null)
                {
                    rectTransform.DOKill();
                }
                if (card.transform != null)
                {
                    card.transform.DOKill();
                }
            }
        }
    }

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

    private void UpdateDifficultyOnScoreChange()
    {
        UpdateDifficultyLevel();
        ArrangeCardsBasedOnState();
        FilterCardsByColor(wantedCard);
    }

    private void FilterCardsByColor(CharacterCard wantedCard)
    {
        if (onlyOneColorActive && wantedCard != null)
        {
            foreach (var group in GameManager.Instance.allCharacterSprites)
            {
                if (System.Array.Exists(group.expressions, s => s == wantedCard.characterSprite))
                {
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
            foreach (var card in cards)
            {
                card.gameObject.SetActive(true);
            }
        }
    }

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
