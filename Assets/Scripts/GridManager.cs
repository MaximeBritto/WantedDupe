using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

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
    public float rouletteDuration = 2f;
    public float highlightDelay = 0.1f;
    public float delayAfterSuccess = 1f;

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
    /// On les arrête tous quand on change d'état.
    /// </summary>
    private List<Tween> activeTweens = new List<Tween>();

    private void Start()
    {
        // Exemple : On écoute des events du GameManager (adapté à votre code)
        GameManager.Instance.onGameStart.AddListener(InitializeGrid);
        GameManager.Instance.onScoreChanged.AddListener(OnScoreChanged);

        if (difficultyLevels == null || difficultyLevels.Length == 0)
        {
            Debug.LogError("Aucun niveau de difficulté configuré!");
        }

        if (gridContainer == null)
        {
            Debug.LogError("GridContainer non assigné!");
        }

        // Récupération des dimensions du board si besoin
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
    /// Ajuste certains paramètres si on est sur mobile (taille d'écran, etc.).
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
    /// Retourne une position aléatoire dans le playArea,
    /// en vérifiant qu'elle ne soit pas trop proche d'une autre carte déjà placée.
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

        // Par défaut, on renvoie (0,0) si pas trouvé
        return Vector2.zero;
    }

    /// <summary>
    /// Initialise la grille (création des cartes, Wanted, etc.),
    /// mais NE les affiche pas (AnimateCardsEntry n'est pas appelé ici).
    /// </summary>
    public void InitializeGrid()
    {
        AdjustForMobileIfNeeded();

        UpdateDifficultyLevel();

        // Nettoyer les cartes existantes
        foreach (var card in cards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        cards.Clear();

        // Nombre de cartes entre minCards et maxCards
        int numberOfCards = Random.Range(currentLevel.minCards, currentLevel.maxCards + 1);

        // Choix du sprite Wanted
        Sprite wantedSprite = GameManager.Instance.GetRandomSprite();

        for (int i = 0; i < numberOfCards; i++)
        {
            GameObject cardObj = Instantiate(characterCardPrefab, gridContainer);
            CharacterCard card = cardObj.GetComponent<CharacterCard>();

            // Position aléatoire
            Vector2 position = GetValidCardPosition();
            RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = position;

            // Sprite
            if (i == 0)
            {
                // Première carte = Wanted
                card.Initialize("Wanted", wantedSprite);
                wantedCard = card;
            }
            else
            {
                // Choisir un sprite différent
                Sprite randomSprite;
                do
                {
                    randomSprite = GameManager.Instance.GetRandomSprite();
                } while (randomSprite == wantedSprite);

                card.Initialize($"Card_{i}", randomSprite);
            }

            cards.Add(card);
        }

        if (wantedCard == null)
        {
            Debug.LogError("Wanted Card is null!");
        }
        else
        {
            // Indiquer au GameManager la carte Wanted
            GameManager.Instance.SelectNewWantedCharacter(wantedCard);
        }

        // Masquer d'abord toutes les cartes
        foreach (var card in cards)
        {
            card.gameObject.SetActive(false);
        }

        // IMPORTANT : On NE fait PAS AnimateCardsEntry() ici.
        // => Les cartes sont créées mais restent invisibles.
        // Vous pourrez appeler ShowCards() ou AnimateCardsEntry() plus tard,
        // au moment où vous voulez qu'elles apparaissent.
    }

    /// <summary>
    /// Montre et anime les cartes (entry scale 0->1), puis arrange selon l'état.
    /// À appeler après la Roulette ou quand vous le jugez nécessaire.
    /// </summary>
    public void ShowCards()
    {
        AnimateCardsEntry();
    }

    /// <summary>
    /// Méthode appelée quand on veut re-créer un Wanted (après un succès, par exemple).
    /// </summary>
    public void CreateNewWanted()
    {
        UpdateDifficultyOnScoreChange();
        StartCoroutine(RouletteEffect());
    }

    /// <summary>
    /// Effet "roulette" pour choisir un nouveau Wanted.
    /// </summary>
    private IEnumerator RouletteEffect()
    {
        yield return new WaitForSeconds(delayAfterSuccess);

        Sprite newWantedSprite = GameManager.Instance.GetRandomSprite();

        int newWantedIndex = Random.Range(0, cards.Count);
        wantedCard = cards[newWantedIndex];
        wantedCard.Initialize("Wanted", newWantedSprite);

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

        GameManager.Instance.SelectNewWantedCharacter(wantedCard);

        // Exemple : on pourrait décider de montrer les cartes
        // juste après la roulette, si besoin :
        // ShowCards();
    }

    /// <summary>
    /// Lance un mouvement aléatoire continu pour la carte spécifiée.
    /// </summary>
    private void StartContinuousCardMovement(CharacterCard card, float speed)
    {
        RectTransform rectTransform = card.GetComponent<RectTransform>();
        rectTransform.DOKill(); // Stoppe d'éventuels tweens en cours sur cette carte

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

    /// <summary>
    /// Anime l'entrée (scale 0 -> 1) de toutes les cartes.
    /// On attend que TOUTES les cartes aient fini avant d'appeler ArrangeCardsBasedOnState().
    /// </summary>
    public void AnimateCardsEntry()
    {
        int cardsCompleted = 0;

        foreach (var card in cards)
        {
            if (card == null) continue;

            card.gameObject.SetActive(true);
            card.transform.localScale = Vector3.zero;

            // Tween d'apparition
            card.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => {
                    cardsCompleted++;
                    // Lorsque toutes les cartes ont fini leur tween d'entrée,
                    // on exécute ArrangeCardsBasedOnState() pour éviter tout conflit de scale
                    if (cardsCompleted >= cards.Count)
                    {
                        ArrangeCardsBasedOnState();
                    }
                });
        }
    }

    /// <summary>
    /// Met à jour le niveau de difficulté en fonction du score
    /// et sélectionne un nouvel état (au hasard parmi ceux autorisés).
    /// </summary>
    private void UpdateDifficultyLevel()
    {
        float currentScore = GameManager.Instance.currentScore;
        currentLevel = difficultyLevels[0];
        foreach (var level in difficultyLevels)
        {
            if (currentScore >= level.scoreThreshold)
                currentLevel = level;
        }
        currentState = currentLevel.possibleStates[Random.Range(0, currentLevel.possibleStates.Length)];
    }

    /// <summary>
    /// Selon l'état sélectionné (currentState), on arrange ou on anime les cartes différemment.
    /// </summary>
    private void ArrangeCardsBasedOnState()
    {
        StopAllCardMovements();

        // Petit délai pour laisser le temps de "finir" l'animation d'entrée
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

    #region Agencements & Mouvements classiques

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
            Tweener tween = rectTransform.DOAnchorPos(new Vector2(xPos, yPos), 0.5f)
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
                Tweener tween = rectTransform.DOAnchorPos(new Vector2(xPos, yPos), 0.5f)
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
                // Mouvement continu
                StartContinuousCardMovement(card, speed);
            }
            else
            {
                Tweener tween = rectTransform.DOAnchorPos(randomPos, 0.5f)
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
        List<List<float>> initialYList = new List<List<float>>();

        int currentCard = 0;
        for (int col = 0; col < columns; col++)
        {
            columnsList.Add(new List<RectTransform>());
            initialYList.Add(new List<float>());

            for (int row = 0; row < maxRows && currentCard < totalCards; row++)
            {
                float xPos = startX + col * currentLevel.fixedColumnSpacing;
                int expectedCount = (totalCards % columns > col) ? maxRows : maxRows - 1;
                float gap = ((float)(maxRows - expectedCount)) * verticalSpacing / 2f;
                float initialY = highestY - gap - (row * verticalSpacing);

                RectTransform rt = cards[currentCard].GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(xPos, initialY);

                columnsList[col].Add(rt);
                initialYList[col].Add(initialY);

                currentCard++;
            }
        }

        // Animation "descend" ou "monte" en boucle
        for (int col = 0; col < columns; col++)
        {
            int capturedCol = col; // Capture locale pour éviter le problème de fermeture (closure)
            bool moveDown = (capturedCol % 2 == 0);
            float speed = 100f * currentLevel.moveSpeed;

            Sequence seq = DOTween.Sequence()
                .SetLoops(-1)
                .SetUpdate(true)
                .OnUpdate(() =>
                {
                    float offset = Time.deltaTime * speed;
                    for (int j = 0; j < columnsList[capturedCol].Count; j++)
                    {
                        Vector2 pos = columnsList[capturedCol][j].anchoredPosition;
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
                        columnsList[capturedCol][j].anchoredPosition = pos;
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
                Tweener moveTween = rectTransform.DOAnchorPos(new Vector2(xPos, yPos), 0.5f)
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

    #endregion

    /// <summary>
    /// État PulsingMoving : toutes les cartes bougent aléatoirement, sauf que
    /// toutes sauf la Wanted ont un scale aléatoire + durée aléatoire + décalage aléatoire.
    /// </summary>
    private void StartPulsingMovement()
    {
        StopAllCardMovements();

        foreach (var card in cards)
        {
            // Mouvement aléatoire continu
            StartContinuousCardMovement(card, currentLevel.moveSpeed);

            // Effet pulsation pour toutes les cartes sauf la Wanted
            if (card != wantedCard)
            {
                card.transform.localScale = Vector3.one;

                float randomTargetScale = Random.Range(0.8f, 3f);
                float randomDuration = Random.Range(0.5f, 1.5f);
                float randomDelay = Random.Range(0f, 1f);

                Tweener pulseTween = card.transform.DOScale(randomTargetScale, randomDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetDelay(randomDelay);

                activeTweens.Add(pulseTween);
            }
        }
    }

    /// <summary>
    /// Stoppe tous les tweens en cours, pour ré-initialiser avant un nouvel arrangement.
    /// </summary>
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
    /// Désactive toutes les cartes sauf la Wanted (pratique pour fin de partie, manche ratée, etc.).
    /// </summary>
    public void HideAllButWanted()
    {
        // Arrête d'abord tous les tweens afin que les callbacks n'interfèrent pas
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
        // UIManager.Instance.UpdateDifficultyText(currentLevel.scoreThreshold, currentState);
    }
}
