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

    // Variable pour suivre si une transition est en cours
    private bool isTransitioningDifficulty = false;
    private bool isRouletteActive = false;

    private bool isCardAnimationRunning = false;

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
    public GridState CurrentState { get { return currentState; } }

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

    [Header("Pattern History Settings")]
    private Queue<GridState> lastUsedPatterns = new Queue<GridState>();
    private const int PATTERN_HISTORY_SIZE = 3; // Nombre de derniers patterns à mémoriser

    // Propriété publique pour indiquer si une roulette GridManager est active
    public bool IsRouletteActive { get { return isRouletteActive; } }

    private void Start()
    {
        // Utiliser une lambda pour appeler InitializeGrid avec le paramètre par défaut
        GameManager.Instance.onGameStart.AddListener(() => {
            // IMPORTANT: Initialiser la grille mais NE PAS animer les cartes tout de suite
            // Le paramètre false indique de ne pas animer les cartes
            InitializeGrid(false);
            
            // UIManager.WantedRouletteEffect s'occupera d'animer les cartes après la roulette
            Debug.Log("Jeu démarré - Grille initialisée sans animation (attente fin de roulette)");
        });
        
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

    public void InitializeGrid(bool shouldArrangeCards = true)
    {
        AdjustForMobileIfNeeded();
        UpdateDifficultyLevel();

        // IMPORTANT: S'assurer que toutes les cartes existantes sont détruites correctement
        // Détruire les cartes existantes
        foreach (var existingCard in cards)
        {
            if (existingCard != null)
                Destroy(existingCard.gameObject);
        }
        cards.Clear();
        wantedCard = null; // Réinitialiser explicitement le wanted

        // Détermine aléatoirement si le mode Only One Color sera actif
        onlyOneColorActive = currentLevel.onlyOneColor && (Random.value < 0.5f);

        // Détermine la transform parent à utiliser (GameBoard si disponible, sinon gridContainer)
        Transform parentTransform = gameBoardTransform != null ? gameBoardTransform : gridContainer;

        int numberOfCards = Random.Range(currentLevel.minCards, currentLevel.maxCards + 1);
        Debug.Log($"Initialisation d'une grille avec {numberOfCards} cartes (arrangeCards={shouldArrangeCards})");

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

        // Vérification de sécurité pour s'assurer qu'il y a exactement un wanted
        ValidateWantedCard();

        if (wantedCard == null)
        {
            Debug.LogError("Pas de wanted trouvé après InitializeGrid!");
            return;
        }

        GameManager.Instance.SelectNewWantedCharacter(wantedCard);
        FilterCardsByColor(wantedCard);
        
        // Masquer toutes les cartes pour préparer l'animation d'entrée
        foreach (var c in cards)
        {
            // Garder l'état de la carte mais réduire l'échelle à zéro
            c.gameObject.SetActive(true);
            c.transform.localScale = Vector3.zero;
        }
        
        // Toujours positionner les cartes selon le pattern actuel
        ArrangeCardsBasedOnStateWithoutAnimation();
        
        // Si demandé, animer les cartes
        if (shouldArrangeCards)
        {
            AnimateCardsEntry();
            Debug.Log("Initialisation complète - Cartes arrangées et animées");
        }
        else
        {
            Debug.Log("Initialisation complète - Cartes positionnées mais pas animées");
        }
    }

    // Nouvelle méthode pour valider/réparer le wanted card
    private void ValidateWantedCard()
    {
        // Vérifier combien de cartes sont marquées comme "Wanted"
        var wantedCards = cards.Where(c => c != null && c.characterName == "Wanted").ToList();
        
        if (wantedCards.Count == 0)
        {
            // Aucun wanted trouvé, créer un nouveau
            Debug.LogWarning("Aucune carte wanted trouvée. Création d'une nouvelle carte wanted.");
            
            if (cards.Count > 0)
            {
                // Convertir la première carte en wanted
                cards[0].Initialize("Wanted", GameManager.Instance.GetRandomSprite());
                wantedCard = cards[0];
            }
            else
            {
                // Situation critique, aucune carte disponible
                Debug.LogError("Aucune carte disponible pour créer un wanted!");
            }
        }
        else if (wantedCards.Count > 1)
        {
            // Trop de wanted, garder seulement le premier
            Debug.LogWarning($"Trouvé {wantedCards.Count} cartes wanted. Conservation uniquement de la première.");
            
            wantedCard = wantedCards[0];
            
            // Renommer les autres cartes wanted
            for (int i = 1; i < wantedCards.Count; i++)
            {
                Sprite randomSprite;
                do
                {
                    randomSprite = GameManager.Instance.GetRandomSprite();
                } while (randomSprite == wantedCard.characterSprite);
                
                wantedCards[i].Initialize("Card_" + (cards.Count + i), randomSprite);
            }
        }
        else
        {
            // Un seul wanted trouvé, c'est normal
            wantedCard = wantedCards[0];
        }
    }

    public void AnimateCardsEntry()
    {
        // Vérifier si une animation est déjà en cours
        if (isCardAnimationRunning)
        {
            Debug.LogWarning("Animation d'entrée des cartes déjà en cours - Mais on force quand même");
            // On continue quand même pour être sûr
        }
        
        // On permet l'animation même si roulette active
        if (isRouletteActive)
        {
            Debug.Log("Animation d'entrée des cartes pendant une roulette active - AUTORISÉE");
        }
        
        // VÉRIFICATION CRITIQUE: S'assurer que les cartes existent et sont correctement référencées
        if (cards.Count == 0)
        {
            Debug.LogError("ERREUR CRITIQUE: Aucune carte n'existe lors de l'appel à AnimateCardsEntry!");
            return;
        }
        
        int nullCards = cards.Count(c => c == null);
        if (nullCards > 0)
        {
            Debug.LogWarning($"ATTENTION: {nullCards} cartes nulles détectées sur {cards.Count} total");
            // Nettoyage des références nulles
            cards = cards.Where(c => c != null).ToList();
        }
        
        // IMPORTANT: Ne pas rappeler ArrangeCardsBasedOnStateWithoutAnimation ici
        // pour éviter un double placement des cartes
        
        isCardAnimationRunning = true;
        Debug.Log($"Animation d'entrée démarrée pour {cards.Count} cartes");
        
        // Stocker l'état actuel pour savoir s'il faut démarrer un mouvement après
        GridState stateAfterAnimation = currentState;
        bool needsMovementAfterAnimation = IsMovementState(stateAfterAnimation);
        
        // Mémoriser les positions actuelles pour chaque carte
        Dictionary<CharacterCard, Vector2> originalPositions = new Dictionary<CharacterCard, Vector2>();
        
        foreach (var card in cards)
        {
            if (card == null) continue;
            
            // Mémoriser la position actuelle
            RectTransform rt = card.GetComponent<RectTransform>();
            originalPositions[card] = rt.anchoredPosition;
            
            // Arrêter toutes les animations en cours sur cette carte
            DOTween.Kill(card.transform);
            
            // Activer la carte mais avec une échelle zéro
            card.gameObject.SetActive(true);
            card.transform.localScale = Vector3.zero;
        }
        
        Debug.Log($"Toutes les cartes sont prêtes pour l'animation d'entrée - Échelle zéro");
        
        // Force l'activation et l'animation des cartes
        int animationDelay = 0;
        int cardCount = cards.Count;
        foreach (var card in cards)
        {
            if (card == null) continue;
            
            // Récupérer la position mémorisée
            Vector2 originalPosition = originalPositions[card];
            
            // S'assurer que la carte est à la bonne position avant d'animer
            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchoredPosition = originalPosition;
            
            // Délai PLUS COURT entre chaque carte pour que l'animation se termine plus vite
            float delay = animationDelay * 0.01f; // Réduit à 0.01 pour une animation plus rapide
            
            // Animer la carte depuis échelle zéro vers l'échelle 1
            card.transform.DOScale(Vector3.one, 0.25f) // Animation plus rapide (0.25s)
                .SetDelay(delay)
                .SetEase(Ease.OutBack)
                .OnComplete(() => {
                    // S'assurer que l'échelle est exactement 1 après l'animation
                    if (card != null)
                    {
                        card.transform.localScale = Vector3.one;
                        
                        // Restaurer la position d'origine pour être sûr
                        card.GetComponent<RectTransform>().anchoredPosition = originalPosition;
                    }
                });
            
            animationDelay++;
        }
        
        // Calculer la durée totale de l'animation pour toutes les cartes
        float totalAnimationDuration = 0.25f + (cardCount * 0.01f) + 0.1f; // Ajout d'une marge de 0.1s
        
        Debug.Log($"Animation démarrée pour {cardCount} cartes - Durée totale estimée: {totalAnimationDuration}s");
        
        // Marquer la fin de l'animation après le délai calculé
        DOVirtual.DelayedCall(totalAnimationDuration, () => {
            // S'assurer que toutes les cartes ont la bonne échelle et la bonne position
            int fixedCards = 0;
            int activatedCards = 0;
            int repositionedCards = 0;
            
            foreach (var card in cards)
            {
                if (card != null)
                {
                    // Si la carte n'est pas active, l'activer
                    if (!card.gameObject.activeSelf)
                    {
                        card.gameObject.SetActive(true);
                        activatedCards++;
                    }
                    
                    // Si l'échelle n'est pas exactement 1, la corriger
                    if (card.transform.localScale != Vector3.one)
                    {
                        card.transform.localScale = Vector3.one;
                        fixedCards++;
                    }
                    
                    // Si la position a changé, la restaurer
                    RectTransform rt = card.GetComponent<RectTransform>();
                    if (originalPositions.ContainsKey(card) && Vector2.Distance(rt.anchoredPosition, originalPositions[card]) > 0.1f)
                    {
                        rt.anchoredPosition = originalPositions[card];
                        repositionedCards++;
                    }
                }
            }
            
            if (fixedCards > 0 || activatedCards > 0 || repositionedCards > 0)
            {
                Debug.Log($"Animation terminée - {fixedCards} cartes échelle ajustée, {activatedCards} cartes activées, {repositionedCards} cartes repositionnées");
            }
            else
            {
                Debug.Log("Animation terminée - Toutes les cartes sont correctement à l'échelle 1 et actives");
            }
            
            isCardAnimationRunning = false;
            
            // Si l'état requiert un mouvement, le démarrer maintenant que l'animation est terminée
            if (needsMovementAfterAnimation)
            {
                Debug.Log($"Animation terminée - Démarrage des mouvements pour l'état: {stateAfterAnimation}");
                StartMovementBasedOnState(stateAfterAnimation);
            }
        });
    }

    // Nouvelle méthode pour vérifier si un état nécessite un mouvement
    private bool IsMovementState(GridState state)
    {
        return state == GridState.SlowMoving || 
               state == GridState.FastMoving || 
               state == GridState.AlignedMoving || 
               state == GridState.ColumnsMoving || 
               state == GridState.CircularAlignedMoving || 
               state == GridState.PulsingMoving;
    }
    
    // Nouvelle méthode pour démarrer les mouvements après l'animation
    private void StartMovementBasedOnState(GridState state)
    {
        // Ne démarrer le mouvement que si aucune roulette n'est active
        if (isRouletteActive || isTransitioningDifficulty || UIManager.Instance.isRouletteRunning)
        {
            Debug.LogWarning("Mouvement reporté - une roulette est active");
            
            // Programmer une nouvelle tentative après un délai
            DOVirtual.DelayedCall(0.5f, () => {
                if (!isRouletteActive && !isTransitioningDifficulty && !UIManager.Instance.isRouletteRunning)
                {
                    StartMovementBasedOnState(state);
                }
            });
            return;
        }
        
        switch (state)
        {
            case GridState.SlowMoving:
                Debug.Log("Démarrage du mouvement lent après animation");
                foreach (var card in cards)
                {
                    if (card == null) continue;
                    StartContinuousCardMovement(card, currentLevel.moveSpeed);
                }
                break;
                
            case GridState.FastMoving:
                Debug.Log("Démarrage du mouvement rapide après animation");
                foreach (var card in cards)
                {
                    if (card == null) continue;
                    StartContinuousCardMovement(card, currentLevel.moveSpeed * 1.5f);
                }
                break;
                
            case GridState.AlignedMoving:
                Debug.Log("Démarrage du mouvement aligné après animation");
                StartAlignedMovement();
                break;
                
            case GridState.ColumnsMoving:
                Debug.Log("Démarrage du mouvement en colonnes après animation");
                StartColumnsMovement();
                break;
                
            case GridState.CircularAlignedMoving:
                Debug.Log("Démarrage du mouvement circulaire après animation");
                StartCircularMovement();
                break;
                
            case GridState.PulsingMoving:
                Debug.Log("Démarrage du mouvement pulsant après animation");
                // Ajouter un délai supplémentaire avant de démarrer le pulsing
                DOVirtual.DelayedCall(0.5f, () => {
                    Debug.Log("Démarrage effectif du mouvement pulsant après délai supplémentaire");
                    StartPulsingMovement();
                });
                break;
        }
    }

    public void CreateNewWanted()
    {
        // Si déjà en mode roulette, ne rien faire pour éviter une double initialisation
        if (isRouletteActive)
        {
            Debug.LogWarning("Tentative de démarrer CreateNewWanted alors qu'une roulette est déjà active!");
            return;
        }
        
        StartCoroutine(HideCardsAndStartRoulette());
    }

    private IEnumerator HideCardsAndStartRoulette()
    {
        // Faire disparaître toutes les cartes mais sans désactiver le parent
        foreach (var card in cards)
        {
            if (card != null)
            {
                card.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
            }
        }
        
        // Attendre que les cartes disparaissent complètement
        yield return new WaitForSeconds(0.5f);
        
        // Démarrer la roulette
        Debug.Log("Lancement de la roulette GridManager");
        
        // Préparer les nouvelles cartes
        yield return StartCoroutine(PrepareNewCards());
    }

    private IEnumerator PrepareNewCards()
    {
        // Marquer le début de la roulette
        isRouletteActive = true;
        Debug.Log("Début de la préparation des nouvelles cartes");
        
        // Attendre le délai après succès
        yield return new WaitForSeconds(delayAfterSuccess);

        // On met à jour la difficulté avant d'initialiser la grille
        isTransitioningDifficulty = true;
        UpdateDifficultyLevel();
        
        // S'assurer que le parent est actif
        Transform parentTransform = gameBoardTransform != null ? gameBoardTransform : transform;
        if (!parentTransform.gameObject.activeSelf)
        {
            Debug.LogWarning("Parent des cartes désactivé - Réactivation");
            parentTransform.gameObject.SetActive(true);
        }
        
        // Créer les cartes mais les garder cachées
        InitializeGrid(false);

        // S'assurer que toutes les cartes sont initialement à échelle zéro
        foreach (var card in cards)
        {
            if (card != null)
            {
                card.gameObject.SetActive(true);
                card.transform.localScale = Vector3.zero;
            }
        }

        // Recherche le nouveau wanted par la propriété characterName
        ValidateWantedCard();
        
        if (wantedCard == null)
        {
            Debug.LogError("Pas de wanted trouvé après InitializeGrid!");
            isRouletteActive = false;
            isTransitioningDifficulty = false;
            yield break;
        }

        // Vérifier que le wantedCard a un sprite
        if (wantedCard.characterSprite == null)
        {
            Debug.LogError("Le wantedCard n'a pas de sprite!");
            wantedCard.Initialize("Wanted", GameManager.Instance.GetRandomSprite());
        }
        
        wantedCard.transform.SetAsLastSibling();
        
        // Vérifier que toutes les cartes sont bien créées et initialisées
        foreach (var card in cards)
        {
            if (card == null || card.characterSprite == null)
            {
                Debug.LogWarning("Carte mal initialisée détectée, correction...");
                if (card != null && card.characterSprite == null)
                {
                    // Réinitialiser la carte avec un sprite valide
                    card.Initialize(card.characterName, GameManager.Instance.GetRandomSprite());
                }
            }
            
            // S'assurer que les cartes sont actives mais avec une échelle zéro
            if (card != null)
            {
                card.gameObject.SetActive(true);
                card.transform.localScale = Vector3.zero; // Prêt pour l'animation plus tard
            }
        }
        
        // Positionner explicitement les cartes pendant qu'elles sont encore cachées
        ArrangeCardsBasedOnStateWithoutAnimation();
        Debug.Log($"Cartes positionnées - État: {currentState}, Nombre de cartes: {cards.Count}");
        
        // Informer GameManager du nouveau wanted (cela déclenchera la roulette UI)
        GameManager.Instance.SelectNewWantedCharacter(wantedCard);
        
        // Les cartes seront rendues visibles par UIManager quand la roulette UI sera terminée
        isTransitioningDifficulty = false;
        isRouletteActive = false;
        Debug.Log("Fin de la préparation des nouvelles cartes");
    }

    private GridState GetNextPattern(GridState[] possibleStates)
    {
        if (possibleStates == null || possibleStates.Length == 0)
            return GridState.Static;

        // Si on n'a qu'un seul pattern possible, on le renvoie directement
        if (possibleStates.Length == 1)
            return possibleStates[0];

        // Créer une liste de patterns possibles en excluant les patterns récemment utilisés
        List<GridState> availablePatterns = new List<GridState>(possibleStates);
        
        // Retirer les patterns récemment utilisés de la liste des patterns disponibles
        foreach (var recentPattern in lastUsedPatterns)
        {
            availablePatterns.Remove(recentPattern);
        }

        // Si tous les patterns ont été utilisés récemment, on prend n'importe lequel sauf le dernier utilisé
        if (availablePatterns.Count == 0)
        {
            availablePatterns.AddRange(possibleStates);
            if (lastUsedPatterns.Count > 0)
            {
                availablePatterns.Remove(lastUsedPatterns.Peek());
            }
        }

        // Sélectionner un pattern aléatoire parmi les disponibles
        GridState selectedPattern = availablePatterns[Random.Range(0, availablePatterns.Count)];

        // Mettre à jour l'historique des patterns
        lastUsedPatterns.Enqueue(selectedPattern);
        if (lastUsedPatterns.Count > PATTERN_HISTORY_SIZE)
        {
            lastUsedPatterns.Dequeue();
        }

        return selectedPattern;
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

        bool levelChanged = (currentLevel != newLevel);
        
        // Si une roulette est en cours, ne pas marquer comme transition (c'est déjà géré)
        if (levelChanged && !isRouletteActive)
        {
            isTransitioningDifficulty = true;
            Debug.Log($"Transition de difficulté: {(currentLevel != null ? currentLevel.scoreThreshold : 0)} -> {newLevel.scoreThreshold}");
        }
        
        // Si on change de niveau de difficulté, on réinitialise l'historique des patterns
        if (levelChanged)
        {
            lastUsedPatterns.Clear();
        }

        currentLevel = newLevel;
        currentState = GetNextPattern(currentLevel.possibleStates);

        // Appliquer les dimensions spécifiques à l'état actuel
        ApplyStateSpecificDimensions(currentState);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateDifficultyText(currentLevel.scoreThreshold, currentState);
        }
        
        // Marquer la fin de la transition après un court délai (si pas en roulette)
        if (levelChanged && !isRouletteActive)
        {
            StartCoroutine(EndTransitionAfterDelay());
        }
    }
    
    private IEnumerator EndTransitionAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        isTransitioningDifficulty = false;
        Debug.Log("Fin de la transition de difficulté");
    }

    public void ArrangeCardsBasedOnState()
    {
        // Méthode publique qui expose l'arrangement des cartes
        ArrangeCardsBasedOnStateWithoutAnimation();
    }

    // Nouvelle méthode pour arranger les cartes sans animation
    private void ArrangeCardsBasedOnStateWithoutAnimation()
    {
        // Même avec une roulette active, on permet l'arrangement des cartes pour UIManager
        if (isRouletteActive && !isTransitioningDifficulty)
        {
            Debug.Log("Arrangement des cartes pendant une roulette active - AUTORISÉ pour l'UIManager");
        }
        
        Debug.Log("Début de l'arrangement des cartes - État: " + currentState);
        
        // Arrêter tous les mouvements et animations en cours
        StopAllCardMovements();
        
        // Mélanger les cartes avant de les arranger (pour des positions aléatoires)
        ShuffleCards();
        
        // Appliquer les dimensions spécifiques selon le mode
        ApplyStateSpecificDimensions(currentState);
        
        // Positionner les cartes en fonction de l'état actuel sans animation
        switch (currentState)
        {
            case GridState.Aligned:
                Debug.Log("Arrangeant les cartes en ligne");
                ArrangeCardsInLine();
                break;
            case GridState.Columns:
                Debug.Log("Arrangeant les cartes en colonnes");
                ArrangeCardsInColumns();
                break;
            case GridState.CircularAligned:
                Debug.Log("Arrangeant les cartes en cercles");
                ArrangeCardsInCircles();
                break;
            case GridState.Static:
                Debug.Log("Arrangeant les cartes aléatoirement (statique)");
                ArrangeCardsRandomly(false);
                break;
            case GridState.SlowMoving:
            case GridState.FastMoving:
                Debug.Log("Arrangeant les cartes aléatoirement (sans démarrer le mouvement)");
                ArrangeCardsRandomly(false); // Ne pas démarrer le mouvement tout de suite
                break;
            case GridState.AlignedMoving:
                Debug.Log("Positionnement pour mouvement aligné (sans démarrer le mouvement)");
                PositionCardsForAlignedMovement(); // Nouvelle méthode sans démarrer le mouvement
                break;
            case GridState.ColumnsMoving:
                Debug.Log("Positionnement pour mouvement en colonnes (sans démarrer le mouvement)");
                PositionCardsForColumnsMovement(); // Nouvelle méthode sans démarrer le mouvement
                break;
            case GridState.CircularAlignedMoving:
                Debug.Log("Positionnement pour mouvement circulaire (sans démarrer le mouvement)");
                PositionCardsForCircularMovement(); // Nouvelle méthode sans démarrer le mouvement
                break;
            case GridState.PulsingMoving:
                Debug.Log("Positionnement pour mouvement pulsant (sans démarrer le mouvement)");
                ArrangeCardsRandomly(false); // Positionner aléatoirement sans démarrer le mouvement
                break;
        }
        
        // IMPORTANT: Fixer les positions des cartes pour éviter tout repositionnement indésirable
        foreach (var card in cards)
        {
            if (card != null)
            {
                // Activer la carte si elle était désactivée
                if (!card.gameObject.activeSelf)
                {
                    card.gameObject.SetActive(true);
                }
                
                // Arrêter toute animation ou mouvement en cours sur cette carte
                DOTween.Kill(card.transform);
                
                // Mémoriser la position exacte pour éviter les déplacements non désirés
                var rectTransform = card.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = rectTransform.anchoredPosition;
            }
        }
        
        Debug.Log($"Fin de l'arrangement des cartes - {cards.Count} cartes positionnées");
    }

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
            
            // Pour l'étape de positionnement initial, placer directement sans animation
            if (isCardAnimationRunning || isRouletteActive)
            {
                // Positionnement direct sans animation
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
            }
            else
            {
                // Animation normale
            Tween tween = rectTransform.DOAnchorPos(new Vector2(xPos, yPos), 0.5f)
                .SetEase(Ease.OutBack);
            activeTweens.Add(tween);
            }
        }
    }
    
    // Nouvelles méthodes pour positionner les cartes sans démarrer le mouvement
    private void PositionCardsForAlignedMovement()
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
                
            // Positionnement direct sans animation
            rectTransform.anchoredPosition = new Vector2(xPos, yPos);
        }
    }
    
    private void PositionCardsForColumnsMovement()
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
        
        float usableHeight = playAreaHeight * 0.9f;
        float highestY = usableHeight / 2f;
        
        // Calculer la largeur totale occupée par toutes les colonnes
        float totalWidth = (columns - 1) * currentLevel.fixedColumnSpacing;
        float startX = -totalWidth / 2f;
        
        int currentCardIndex = 0;
        
        // Positionner les cartes dans chaque colonne
        for (int col = 0; col < columns; col++)
        {
            float xPos = startX + col * currentLevel.fixedColumnSpacing;
            int cardsInThisColumn = cardsPerColumn[col];
            
            // Calculer l'espace total occupé par cette colonne
            float columnHeight = (cardsInThisColumn - 1) * verticalSpacing;
            float columnStartY = columnHeight / 2f;
            
            for (int row = 0; row < cardsInThisColumn; row++)
            {
                RectTransform rectTransform = cards[currentCardIndex].GetComponent<RectTransform>();
                float initialY = columnStartY - (row * verticalSpacing);
                
                // Positionnement direct sans animation
                rectTransform.anchoredPosition = new Vector2(xPos, initialY);
                currentCardIndex++;
            }
        }
    }
    
    private void PositionCardsForCircularMovement()
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
        
        // Positionner les cartes dans chaque cercle sans animation
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
                
                // Positionnement DIRECT sans animation
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                
                // Log de débogage pour les premières et dernières cartes de chaque cercle
                if (i == 0 || i == cardsInCircle - 1)
                {
                    Debug.Log($"Carte {cardIndex} positionnée à x={xPos}, y={yPos}, angle={angle * Mathf.Rad2Deg}°");
                }
                
                cardIndex++;
            }
        }
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

    private void ArrangeCardsInColumns()
    {
        int totalCards = cards.Count;
        int columns = currentLevel.fixedColumns;
        if (totalCards < columns)
            columns = totalCards;
        int cardsPerColumn = Mathf.CeilToInt((float)totalCards / columns);
        
        // Calculer la largeur totale occupée par toutes les colonnes
        float totalWidth = (columns - 1) * currentLevel.fixedColumnSpacing;
        
        // Calculer le point de départ pour que les colonnes soient centrées
        float startX = -totalWidth / 2f;
        float startY = (playAreaHeight / 2f) - verticalSpacing;

        // IMPORTANT: Arrêter toutes les animations précédentes
        foreach (var card in cards)
        {
            if (card != null)
            {
                DOTween.Kill(card.transform);
                DOTween.Kill(card.GetComponent<RectTransform>());
            }
        }
        
        Debug.Log($"Position colonnes: startX={startX}, startY={startY}, colonnes={columns}, cartes par colonne={cardsPerColumn}");
        
        // Positionnement DIRECT des cartes sans animation
        int currentCard = 0;
        for (int col = 0; col < columns && currentCard < totalCards; col++)
        {
            float xPos = startX + col * currentLevel.fixedColumnSpacing;
            for (int row = 0; row < cardsPerColumn && currentCard < totalCards; row++)
            {
                RectTransform rectTransform = cards[currentCard].GetComponent<RectTransform>();
                float yPos = startY - row * verticalSpacing;
                
                // Positionnement DIRECT, sans animation
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                
                // Log pour le débogage
                if (col == 0 || col == columns-1)
                {
                    Debug.Log($"Carte {currentCard} positionnée à x={xPos}, y={yPos}");
                }
                
                currentCard++;
            }
        }
        
        // Force la mise à jour du canvas pour s'assurer que tout est correctement dessiné
        Canvas.ForceUpdateCanvases();
        
        Debug.Log($"Positionnement en colonnes terminé - {totalCards} cartes placées en {columns} colonnes");
    }

    private void ArrangeCardsRandomly(bool moving, float speed = 2f)
    {
        StopAllCardMovements();
        foreach (var card in cards)
        {
            if (card == null) continue;
            
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            Vector2 randomPos = GetValidCardPosition();
            
            // Positionnement direct sans animation
            rectTransform.anchoredPosition = randomPos;
            
            if (moving)
            {
                Debug.Log($"Démarrage du mouvement continu pour la carte {card.name} avec vitesse {speed}");
                StartContinuousCardMovement(card, speed);
            }
            }
        
        Debug.Log($"ArrangeCardsRandomly terminé - {cards.Count} cartes arrangées, moving={moving}, speed={speed}");
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
        
        // Obtenir les dimensions et la position du GameBoard
        Vector2 boardCenter = Vector2.zero;
        if (gameBoardRect != null)
        {
            boardCenter = new Vector2(
                gameBoardRect.rect.width / 2f,
                gameBoardRect.rect.height / 2f
            );
        }
        
        // Calculer la largeur totale occupée par toutes les colonnes
        float totalWidth = (columns - 1) * currentLevel.fixedColumnSpacing;
        float startX = -totalWidth / 2f;

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
                            if (pos.y < -highestY) pos.y = highestY;
                        }
                        else
                        {
                            pos.y += offset;
                            if (pos.y > highestY) pos.y = -highestY;
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
        
        // Calculer combien de cartes par cercle
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
        
        Debug.Log($"CircularAligned: {totalCards} cartes, {cardsPerCircle.Count} cercles");
        
        // Arrêter toutes les animations en cours
        foreach (var card in cards)
        {
            if (card != null)
            {
                DOTween.Kill(card.transform);
                DOTween.Kill(card.GetComponent<RectTransform>());
            }
        }
        
        // POSITIONNEMENT DIRECT (sans animation) des cartes en cercles
        int cardIndex = 0;
        for (int circle = 0; circle < cardsPerCircle.Count; circle++)
        {
            float radius = baseRadius + (circle * radiusIncrement);
            int cardsInCircle = cardsPerCircle[circle];
            
            Debug.Log($"Cercle {circle}: rayon={radius}, {cardsInCircle} cartes");
            
            for (int i = 0; i < cardsInCircle; i++)
            {
                if (cardIndex >= cards.Count) break;
                
                float angle = (i * 2 * Mathf.PI) / cardsInCircle;
                float xPos = centerX + radius * Mathf.Cos(angle);
                float yPos = centerY + radius * Mathf.Sin(angle);
                
                RectTransform rectTransform = cards[cardIndex].GetComponent<RectTransform>();
                
                // Positionnement DIRECT sans animation
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                
                // Log de débogage pour les premières et dernières cartes de chaque cercle
                if (i == 0 || i == cardsInCircle - 1)
                {
                    Debug.Log($"Carte {cardIndex} positionnée à x={xPos}, y={yPos}, angle={angle * Mathf.Rad2Deg}°");
                }
                
                cardIndex++;
            }
        }
        
        // Force la mise à jour du canvas pour s'assurer que tout est correctement dessiné
        Canvas.ForceUpdateCanvases();
        
        Debug.Log($"Positionnement circulaire terminé - {totalCards} cartes placées");
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
        // Arrêter d'abord tous les mouvements en cours
        StopAllCardMovements();
        
        Debug.Log("Démarrage du mouvement pulsant avec " + cards.Count + " cartes");
        
        // IMPORTANT: Si une roulette est active ou en transition, NE PAS démarrer l'animation directement
        if (isRouletteActive || isTransitioningDifficulty || UIManager.Instance.isRouletteRunning)
        {
            Debug.LogWarning("⚠️ Mouvement pulsant REPORTÉ - roulette ou transition en cours ⚠️");
            
            // Positionner d'abord les cartes sans animation de pulsation
        foreach (var card in cards)
        {
                if (card == null) continue;
                
                // Activer la carte et s'assurer qu'elle est visible sans animation
                card.gameObject.SetActive(true);
                card.transform.localScale = Vector3.one;
            }
            
            // Programmer une vérification ultérieure avec un délai plus long (2 secondes)
            DOVirtual.DelayedCall(2.0f, () => {
                if (!isRouletteActive && !isTransitioningDifficulty && !UIManager.Instance.isRouletteRunning)
                {
                    // La roulette est terminée, on peut démarrer l'animation avec sécurité
                    Debug.Log("Démarrage retardé des animations de pulsation après vérification");
                    StartPulsingAnimations();
                }
                else
                {
                    // Encore en roulette, reprogrammer une autre vérification
                    Debug.LogWarning("Mouvement pulsant toujours reporté - nouvel essai programmé");
                    DOVirtual.DelayedCall(1.5f, () => {
                        if (!isRouletteActive && !isTransitioningDifficulty && !UIManager.Instance.isRouletteRunning)
                        {
                            StartPulsingAnimations();
                        }
                        else
                        {
                            // Troisième tentative
                            DOVirtual.DelayedCall(1.5f, () => {
                                if (!isRouletteActive && !isTransitioningDifficulty && !UIManager.Instance.isRouletteRunning)
                                {
                                    StartPulsingAnimations();
                                }
                                else {
                                    Debug.LogError("Impossible de démarrer les animations de pulsation après 3 tentatives - forçage");
                                    // Force le démarrage des animations même si les conditions ne sont pas idéales
                                    StartPulsingAnimations(true);
                                }
                            });
                        }
                    });
                }
            });
            return;
        }
        
        // Si aucune roulette n'est active, démarrer directement les animations
        StartPulsingAnimations();
    }
    
    // Méthode séparée pour démarrer les animations de pulsation
    // Le paramètre forceStart permet de démarrer même si une roulette est active (usage exceptionnel)
    private void StartPulsingAnimations(bool forceStart = false)
    {
        // Vérifier à nouveau que nous ne sommes pas en roulette, sauf si forceStart est true
        if (!forceStart && (isRouletteActive || isTransitioningDifficulty || UIManager.Instance.isRouletteRunning))
        {
            Debug.LogWarning("StartPulsingAnimations: Annulé car une roulette est active");
            return;
        }
        
        Debug.Log("Démarrage des animations de pulsation" + (forceStart ? " (FORCÉ)" : ""));
        
        // Vérifier d'abord que toutes les cartes sont positionnées correctement
        foreach (var card in cards)
        {
            if (card == null) continue;
            
            // Arrêter les animations précédentes
            DOTween.Kill(card.transform);
            DOTween.Kill(card.GetComponent<RectTransform>());
            
            // Activer la carte et s'assurer qu'elle est visible
            card.gameObject.SetActive(true);
                card.transform.localScale = Vector3.one;
        }
        
        // Ensuite démarrer le mouvement continu pour chaque carte
        foreach (var card in cards)
        {
            if (card == null) continue;
            
            // Démarrer le mouvement avec une vitesse ajustée
            float moveSpeed = currentLevel.moveSpeed;
            StartContinuousCardMovement(card, moveSpeed);
            
            // N'ajouter l'effet de pulsation qu'aux cartes qui ne sont pas le wanted
            if (card != wantedCard)
            {
                // Attendre un délai aléatoire avant de démarrer la pulsation
                float randomDelay = Random.Range(0.3f, 0.6f);
                DOVirtual.DelayedCall(randomDelay, () => {
                    // Vérifier à nouveau que la carte est toujours valide
                    if (card != null && card.gameObject.activeSelf)
                    {
                        float randomTargetScale = Random.Range(0.8f, 1.5f); // Taille max augmentée à 1.5
                        float randomDuration = Random.Range(0.8f, 1.5f);
                        
                        // Créer une animation de pulsation plus douce
                Tween pulseTween = card.transform.DOScale(randomTargetScale, randomDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                            .SetEase(Ease.InOutSine);
                        
                activeTweens.Add(pulseTween);
                    }
                });
            }
        }
    }

    private void StartContinuousCardMovement(CharacterCard card, float speed)
    {
        if (card == null) return;
        
        RectTransform rectTransform = card.GetComponent<RectTransform>();
        if (rectTransform == null) return;
        
        // Arrêter toutes les animations en cours sur cette carte
        DOTween.Kill(rectTransform);
        
        
        // Log de débogage pour suivre le mouvement
        Debug.Log($"Démarrage du mouvement continu pour {card.name} avec vitesse {speed}");
        
        // Fonction récursive pour créer un mouvement continu
        void StartNewMovement()
        {
            // Vérifier que la carte existe toujours
            if (card == null || rectTransform == null || !card.gameObject.activeInHierarchy)
                return;
            
            // Obtenir une nouvelle position cible valide
            Vector2 targetPos = GetValidCardPosition();
            
            // Calculer la distance pour ajuster la durée
            float distance = Vector2.Distance(rectTransform.anchoredPosition, targetPos);
            float adjustedDuration = distance / (speed * 100f);
            
            // Commencer le mouvement avec suivi
            Tween moveTween = rectTransform.DOAnchorPos(targetPos, adjustedDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(StartNewMovement);
            
            // Ajouter le tween à la liste des tweens actifs
            activeTweens.Add(moveTween);
        }
        
        // Démarrer le premier mouvement
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
        if (GameManager.Instance == null) return;
        
        // Ignorer la mise à jour si :
        // 1. Une roulette est en cours
        // 2. Une transition de difficulté est déjà en cours
        if (!IsRouletteInProgress() && !isTransitioningDifficulty)
        {
            Debug.Log("Mise à jour de la difficulté suite à un changement de score");
            UpdateDifficultyLevel();
            
            // IMPORTANT: Ne PAS appeler ArrangeCardsBasedOnStateWithoutAnimation ici
            // car cela pourrait causer un effet visuel de double placement des cartes
            // Les cartes seront correctement placées lors de la prochaine roulette
        }
        else
        {
            Debug.Log($"Mise à jour de difficulté ignorée - Roulette: {IsRouletteInProgress()}, Transition: {isTransitioningDifficulty}");
        }
        
        FilterCardsByColor(wantedCard);
    }

    private bool IsRouletteInProgress()
    {
        // Utiliser la variable d'état au lieu de chercher les coroutines
        return isRouletteActive;
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

    // Méthode d'urgence pour débloquer le jeu si nécessaire
    public void ResetGame()
    {
        Debug.Log("🚨 RÉINITIALISATION D'URGENCE DU JEU 🚨");
        
        // Réinitialiser tous les états de contrôle
        isRouletteActive = false;
        isTransitioningDifficulty = false;
        
        StopAllCardMovements();
        StopAllCoroutines();
        
        // Détruire toutes les cartes existantes
        foreach (var existingCard in cards)
        {
            if (existingCard != null)
                Destroy(existingCard.gameObject);
        }
        cards.Clear();
        wantedCard = null;
        
        // Réinitialiser l'historique des patterns
        lastUsedPatterns.Clear();
        
        // Réinitialiser le jeu
        InitializeGrid(true);
        AnimateCardsEntry();
        
        Debug.Log("Jeu réinitialisé avec succès.");
    }
    #endregion
}
