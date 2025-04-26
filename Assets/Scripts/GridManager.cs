using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;
using UnityEngine.UI;

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

    [Header("Tablet Settings")]
    public float tabletCardScale = 0.9f;
    public float tabletSpacing = 120f;
    public float tabletVerticalSpacing = 180f;
    public float tabletBoardWidthMultiplier = 0.85f;
    public float tabletBoardHeightMultiplier = 0.7f;

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
        PulsingMoving,
        WaveMoving,
        WaveWrapAround,
        QuantumTeleport,
        GravityWells
    }

    [System.Serializable]
    public class DifficultyLevel
    {
        public int scoreThreshold;
        public int minCards;
        public int maxCards;
        public float moveSpeed;
        public GridState[] possibleStates;
        
        [Header("Infinite Difficulty")]
        [Tooltip("Si activé, la vitesse augmentera progressivement après chaque manche")]
        public bool infiniteDifficultyEnabled = false;
        [Tooltip("Augmentation de vitesse après chaque manche")]
        public float speedIncrementPerRound = 2f;

        [Header("Column Specific Settings (Only used if state is Columns or ColumnsMoving)")]
        public int fixedColumns = 2;
        public float fixedColumnSpacing = 150f;
        public float columnsMovementSpeed = 1.2f;
        public int minCardsColumns = 6;
        public int maxCardsColumns = 14;

        [Header("Wave Wrap Around Settings")]
        [Range(1, 10)]
        public int waveWrapAroundMaxRows = 3;
        public float waveWrapAroundSpeed = 1.5f;
        public int minCardsWaveWrapAround = 8;
        public int maxCardsWaveWrapAround = 16;

        [Header("Circular Movement Settings")]
        [Range(0.5f, 5f)]
        public float circularMovementSpeed = 1f;
        public int minCardsCircularMovement = 5;
        public int maxCardsCircularMovement = 12;

        [Header("Fast Movement Settings")]
        [Range(0.5f, 10f)]
        public float fastMovementSpeed = 2f;
        public int minCardsFastMovement = 8;
        public int maxCardsFastMovement = 15;

        [Header("Slow Movement Settings")]
        [Range(0.1f, 2f)]
        public float slowMovementSpeed = 0.7f;
        public int minCardsSlowMovement = 4;
        public int maxCardsSlowMovement = 10;

        [Header("Aligned Movement Settings")]
        public float alignedMovementSpeed = 1.3f;
        public int minCardsAlignedMovement = 5;
        public int maxCardsAlignedMovement = 12;

        [Header("Pulsing Movement Settings")]
        public float pulsingMovementSpeed = 1.8f;
        public int minCardsPulsingMovement = 6;
        public int maxCardsPulsingMovement = 12;

        [Header("Wave Movement Settings")]
        public float waveMovementSpeed = 1.5f;
        public int minCardsWaveMovement = 7;
        public int maxCardsWaveMovement = 14;

        [Header("Quantum Teleport Settings")]
        public float quantumTeleportSpeed = 1.7f;
        public int minCardsQuantumTeleport = 8;
        public int maxCardsQuantumTeleport = 16;

        [Header("Gravity Wells Settings")]
        public float gravityWellsSpeed = 1.4f;
        public int minCardsGravityWells = 7;
        public int maxCardsGravityWells = 15;

        [Header("Static Settings")]
        public int minCardsStatic = 4;
        public int maxCardsStatic = 9;

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
        
        // Vérifier périodiquement que la carte wanted est visible
        StartCoroutine(CheckWantedVisibilityPeriodically());
    }

    // Coroutine pour vérifier la visibilité de la carte wanted régulièrement
    private IEnumerator CheckWantedVisibilityPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            
            // Vérifier si le jeu est actif et si la roulette n'est pas en cours
            if (wantedCard == null || IsRouletteActive || !GameManager.Instance.isGameActive)
            {
                continue;
            }
            
            // Vérifier si la carte est suffisamment visible
            if (!wantedCard.IsPartiallyVisible())
            {
                // Déplacer légèrement la carte pour la rendre visible
                RectTransform rt = wantedCard.GetComponent<RectTransform>();
                Vector2 newPosition = rt.anchoredPosition + new Vector2(Random.Range(-50f, 50f), Random.Range(-50f, 50f));
                
                // S'assurer que la carte reste dans les limites du plateau
                newPosition.x = Mathf.Clamp(newPosition.x, -playAreaWidth/2 + 100, playAreaWidth/2 - 100);
                newPosition.y = Mathf.Clamp(newPosition.y, -playAreaHeight/2 + 100, playAreaHeight/2 - 100);
                
                // Appliquer la nouvelle position
                rt.anchoredPosition = newPosition;
            }
        }
    }

    private void AdjustForMobileIfNeeded()
    {
        if (Application.isMobilePlatform)
        {
            // Vérifier si c'est une tablette
            bool isTablet = IsTablet();
            
            if (isTablet)
            {
                Debug.Log("Ajustement pour tablette");
                playAreaWidth = Screen.width * tabletBoardWidthMultiplier;
                playAreaHeight = Screen.height * tabletBoardHeightMultiplier;
                cardSpacing = tabletSpacing;
                horizontalSpacing = tabletSpacing;
                verticalSpacing = tabletVerticalSpacing;
                
                foreach (var card in cards)
                {
                    if (card != null)
                    {
                        card.transform.localScale = Vector3.one * tabletCardScale;
                    }
                }
            }
            else
            {
                Debug.Log("Ajustement pour mobile");
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
    }
    
    // Méthode pour détecter les tablettes basée sur la taille d'écran
    private bool IsTablet()
    {
        // Résolution minimum d'une tablette (en général 1280x720 ou plus)
        float minTabletDiagonal = 1500f; // Valeur approximative pour identifier une tablette
        
        // Calculer la diagonale en pixels
        float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
        
        // Log pour le débogage
        Debug.Log($"GridManager - Détection tablette: Diagonale écran = {screenDiagonal}px, Width = {Screen.width}, Height = {Screen.height}");
        
        return screenDiagonal >= minTabletDiagonal;
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

        int numberOfCards;
        // Utiliser les variables spécifiques en fonction de l'état actuel
        if (currentState == GridState.CircularAligned || currentState == GridState.CircularAlignedMoving) {
            numberOfCards = Random.Range(currentLevel.minCardsCircularMovement, currentLevel.maxCardsCircularMovement + 1);
            Debug.Log($"Initialisation d'une grille circulaire avec {numberOfCards} cartes (arrangeCards={shouldArrangeCards})");
        } 
        // Utiliser les variables spécifiques au mouvement rapide si l'état actuel est FastMoving
        else if (currentState == GridState.FastMoving) {
            numberOfCards = Random.Range(currentLevel.minCardsFastMovement, currentLevel.maxCardsFastMovement + 1);
            Debug.Log($"Initialisation d'une grille FastMoving avec {numberOfCards} cartes (arrangeCards={shouldArrangeCards})");
        }
        else if (currentState == GridState.SlowMoving) {
            numberOfCards = Random.Range(currentLevel.minCardsSlowMovement, currentLevel.maxCardsSlowMovement + 1);
            Debug.Log($"Initialisation d'une grille SlowMoving avec {numberOfCards} cartes (arrangeCards={shouldArrangeCards})");
        }
        else if (currentState == GridState.AlignedMoving || currentState == GridState.Aligned) {
            numberOfCards = Random.Range(currentLevel.minCardsAlignedMovement, currentLevel.maxCardsAlignedMovement + 1);
            Debug.Log($"Initialisation d'une grille Aligned/AlignedMoving avec {numberOfCards} cartes (arrangeCards={shouldArrangeCards})");
        }
        else if (currentState == GridState.ColumnsMoving || currentState == GridState.Columns) {
            numberOfCards = Random.Range(currentLevel.minCardsColumns, currentLevel.maxCardsColumns + 1);
            Debug.Log($"Initialisation d'une grille Columns/ColumnsMoving avec {numberOfCards} cartes (arrangeCards={shouldArrangeCards})");
        }
        else if (currentState == GridState.PulsingMoving) {
            numberOfCards = Random.Range(currentLevel.minCardsPulsingMovement, currentLevel.maxCardsPulsingMovement + 1);
            Debug.Log($"Initialisation d'une grille PulsingMoving avec {numberOfCards} cartes (arrangeCards={shouldArrangeCards})");
        }
        else if (currentState == GridState.WaveMoving) {
            numberOfCards = Random.Range(currentLevel.minCardsWaveMovement, currentLevel.maxCardsWaveMovement + 1);
            Debug.Log($"Initialisation d'une grille WaveMoving avec {numberOfCards} cartes (arrangeCards={shouldArrangeCards})");
        }
        else if (currentState == GridState.WaveWrapAround) {
            numberOfCards = Random.Range(currentLevel.minCardsWaveWrapAround, currentLevel.maxCardsWaveWrapAround + 1);
            Debug.Log($"Initialisation d'une grille WaveWrapAround avec {numberOfCards} cartes (arrangeCards={shouldArrangeCards})");
        }
        else if (currentState == GridState.QuantumTeleport) {
            numberOfCards = Random.Range(currentLevel.minCardsQuantumTeleport, currentLevel.maxCardsQuantumTeleport + 1);
            Debug.Log($"Initialisation d'une grille QuantumTeleport avec {numberOfCards} cartes (arrangeCards={shouldArrangeCards})");
        }
        else if (currentState == GridState.GravityWells) {
            numberOfCards = Random.Range(currentLevel.minCardsGravityWells, currentLevel.maxCardsGravityWells + 1);
            Debug.Log($"Initialisation d'une grille GravityWells avec {numberOfCards} cartes (arrangeCards={shouldArrangeCards})");
        }
        else if (currentState == GridState.Static) {
            numberOfCards = Random.Range(currentLevel.minCardsStatic, currentLevel.maxCardsStatic + 1);
            Debug.Log($"Initialisation d'une grille Static avec {numberOfCards} cartes (arrangeCards={shouldArrangeCards})");
        }
        else {
            numberOfCards = Random.Range(currentLevel.minCards, currentLevel.maxCards + 1);
            Debug.Log($"Initialisation d'une grille avec {numberOfCards} cartes (arrangeCards={shouldArrangeCards})");
        }

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
        // Marquer cette carte comme étant la carte recherchée
        wantedCardComponent.SetAsWanted(true);
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
            // S'assurer que les cartes normales sont au-dessus de la carte wanted
            cardComponent.SetAsWanted(false);
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
        Debug.Log($"🔍 Validation de la wanted card - Nombre total de cartes : {cards.Count}");
        
        // Vérifier combien de cartes sont marquées comme "Wanted"
        var wantedCards = cards.Where(c => c != null && (c.characterName == "Wanted" || (GameManager.Instance != null && GameManager.Instance.wantedCharacter == c))).ToList();
        
        // Vérifier d'abord si GameManager a déjà une référence valide
        if (GameManager.Instance != null && GameManager.Instance.wantedCharacter != null)
        {
            CharacterCard gmWanted = GameManager.Instance.wantedCharacter;
            
            // Vérifier si cette carte existe toujours dans notre liste
            if (cards.Contains(gmWanted))
            {
                Debug.Log($"✅ GameManager a une référence wanted valide : {gmWanted.characterName} (ID: {gmWanted.GetInstanceID()})");
                
                // S'assurer que le flag isWanted est correctement défini sur cette carte
                if (gmWanted.characterName != "Wanted")
                {
                    Debug.LogWarning($"Correction: La carte référencée dans GameManager n'a pas le nom 'Wanted'");
                    gmWanted.Initialize("Wanted", gmWanted.characterSprite);
                }
                
                // Définir cette carte comme notre wantedCard
                wantedCard = gmWanted;
                wantedCard.SetAsWanted(true);
                
                // Réinitialiser les autres cartes marquées comme wanted
                foreach (var card in wantedCards)
                {
                    if (card != wantedCard)
                    {
                        card.characterName = "Card_" + (cards.IndexOf(card));
                        card.SetAsWanted(false);
                        Debug.Log($"❌ Carte wanted supplémentaire réinitialisée : {cards.IndexOf(card)}");
                    }
                }
                
                return; // Travail terminé, on a une carte wanted valide
            }
            else
            {
                Debug.LogWarning("⚠️ La référence du wanted dans GameManager n'existe plus dans notre liste de cartes!");
                // On continuera avec la logique standard ci-dessous
            }
        }
        
        // Logique standard si GameManager n'a pas de référence valide
        if (wantedCards.Count == 0)
        {
            // Aucun wanted trouvé, créer un nouveau
            Debug.LogWarning("Aucune carte wanted trouvée. Création d'une nouvelle carte wanted.");
            
            if (cards.Count > 0)
            {
                // Convertir la première carte en wanted
                cards[0].Initialize("Wanted", GameManager.Instance.GetRandomSprite());
                cards[0].SetAsWanted(true);
                wantedCard = cards[0];
                
                // IMPORTANT: Synchroniser avec GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.wantedCharacter = wantedCard;
                }
                
                Debug.Log($"📌 Nouvelle wanted card créée avec sprite : {wantedCard.characterSprite.name}");
            }
            else
            {
                // Situation critique, aucune carte disponible
                Debug.LogError("❌ ERREUR CRITIQUE : Aucune carte disponible pour créer un wanted!");
            }
        }
        else if (wantedCards.Count > 1)
        {
            // Trop de wanted, garder seulement le premier
            Debug.LogWarning($"Trouvé {wantedCards.Count} cartes wanted. Conservation uniquement de la première.");
            
            wantedCard = wantedCards[0];
            wantedCard.SetAsWanted(true);
            
            // IMPORTANT: Synchroniser avec GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.wantedCharacter = wantedCard;
            }
            
            Debug.Log($"📌 Conservation de la wanted card : {wantedCard.characterSprite.name}");
            
            // Réinitialiser les autres cartes
            for (int i = 1; i < wantedCards.Count; i++)
            {
                wantedCards[i].characterName = "Card_" + (cards.IndexOf(wantedCards[i]));
                wantedCards[i].SetAsWanted(false);
                Debug.Log($"❌ Carte wanted supplémentaire réinitialisée : {i}");
            }
        }
        else
        {
            // Une seule carte wanted trouvée (situation normale)
            wantedCard = wantedCards[0];
            
            // IMPORTANT: Synchroniser avec GameManager
            if (GameManager.Instance != null && GameManager.Instance.wantedCharacter != wantedCard)
            {
                Debug.Log($"Synchronisation de GameManager avec la carte wanted: {wantedCard.characterSprite.name}");
                GameManager.Instance.wantedCharacter = wantedCard;
            }
            
            Debug.Log($"✅ Carte wanted identifiée correctement : {wantedCard.characterSprite.name}");
        }
        
        // Vérification finale et synchronisation avec GameManager
        if (wantedCard != null)
        {
            if (GameManager.Instance != null && GameManager.Instance.wantedCharacter != wantedCard)
            {
                Debug.LogError($"⚠️ Échec de synchronisation! GridManager.wantedCard ≠ GameManager.wantedCharacter");
                Debug.LogError($"GridManager.wantedCard : {wantedCard.characterSprite.name}");
                Debug.LogError($"GameManager.wantedCharacter : {GameManager.Instance.wantedCharacter?.characterSprite?.name ?? "null"}");
                
                // Force la synchronisation
                GameManager.Instance.wantedCharacter = wantedCard;
            }
            else
            {
                Debug.Log($"✅ Synchronisation réussie : GridManager et GameManager ont la même référence wanted");
            }
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
               state == GridState.PulsingMoving ||
               state == GridState.WaveMoving ||
               state == GridState.WaveWrapAround ||
               state == GridState.QuantumTeleport ||
               state == GridState.GravityWells;
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
                    StartContinuousCardMovement(card, currentLevel.slowMovementSpeed);
                }
                break;
                
            case GridState.FastMoving:
                Debug.Log("Démarrage du mouvement rapide après animation");
                foreach (var card in cards)
                {
                    if (card == null) continue;
                    StartContinuousCardMovement(card, currentLevel.fastMovementSpeed);
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
                
            case GridState.WaveMoving:
                Debug.Log("Démarrage du mouvement ondulant après animation");
                StartWaveMovement();
                break;

            case GridState.WaveWrapAround:
                Debug.Log("Démarrage du mouvement de vague avec wrap around");
                StartWaveWrapAroundMovement();
                break;

            case GridState.QuantumTeleport:
                Debug.Log("Démarrage de la téléportation quantique après animation");
                StartQuantumTeleport();
                break;
                
            case GridState.GravityWells:
                Debug.Log("Démarrage des puits gravitationnels après animation");
                StartGravityWellsMovement();
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
        
        // Augmenter la vitesse si le mode difficulté infinie est activé
        IncreaseSpeedForInfiniteDifficulty();
        
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
        float currentScore = GameManager.Instance.InternalScore;
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
                StopAllCardMovements(); // S'assurer qu'aucun mouvement n'est actif
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
            case GridState.WaveMoving:
                Debug.Log("Positionnement pour mouvement ondulant (sans démarrer le mouvement)");
                PositionCardsForWaveMovement(); // Nouvelle méthode pour le motif de vagues
                break;
            case GridState.WaveWrapAround:
                Debug.Log("Positionnement pour mouvement de vague avec wrap around (sans démarrer le mouvement)");
                PositionCardsForWaveWrapAroundMovement(); // Nouvelle méthode pour le mouvement de vague avec wrap around
                break;
            case GridState.QuantumTeleport:
                Debug.Log("Positionnement pour téléportation quantique (sans démarrer le mouvement)");
                ArrangeCardsRandomly(false); // Positionner aléatoirement sans démarrer le mouvement
                break;
            case GridState.GravityWells:
                Debug.Log("Positionnement pour puits gravitationnels (sans démarrer le mouvement)");
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
        Debug.Log("DÉBUT ArrangeCardsInLine - Positionnement des cartes en grille ordonnée");
        StopAllCardMovements(); // S'assurer qu'aucun mouvement précédent n'interfère
        
        int totalCards = cards.Count;
        float availableWidth = playAreaWidth * 0.9f;
        int maxColumnsAllowed = Mathf.FloorToInt(availableWidth / horizontalSpacing);
        int cardsPerRow = Mathf.Min(totalCards, maxColumnsAllowed);
        int rows = Mathf.CeilToInt((float)totalCards / cardsPerRow);
        float dynamicHorizontalSpacing = (cardsPerRow > 1) ? availableWidth / (cardsPerRow - 1) : 0;
        float startX = -availableWidth / 2;
        float startY = (rows - 1) * verticalSpacing / 2;

        Debug.Log($"ArrangeCardsInLine: totalCards={totalCards}, cardsPerRow={cardsPerRow}, rows={rows}, spacing={dynamicHorizontalSpacing}");

        // Vérifier que nous avons des cartes à positionner
        if (totalCards == 0 || cards == null)
        {
            Debug.LogError("ArrangeCardsInLine: Pas de cartes à positionner!");
            return;
        }

        for (int i = 0; i < totalCards; i++)
        {
            if (cards[i] == null) continue;
            
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            float xPos = startX + col * dynamicHorizontalSpacing;
            float yPos = startY - row * verticalSpacing;
            RectTransform rectTransform = cards[i].GetComponent<RectTransform>();
            
            Debug.Log($"Card {i}: row={row}, col={col}, xPos={xPos}, yPos={yPos}");
            
            // Positionnement direct sans animation dans tous les cas pour résoudre le problème
            rectTransform.anchoredPosition = new Vector2(xPos, yPos);
            
            // Force la mise à jour pour s'assurer que les changements sont appliqués immédiatement
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
        
        // Force la mise à jour du canvas
        Canvas.ForceUpdateCanvases();
        
        
        Debug.Log("FIN ArrangeCardsInLine - Toutes les cartes ont été positionnées en grille");
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

    // Nouvelle méthode pour positionner les cartes pour le pattern WaveMoving
    private void PositionCardsForWaveMovement()
    {
        StopAllCardMovements();
        int totalCards = cards.Count;
        float availableWidth = playAreaWidth * 0.9f;
        float availableHeight = playAreaHeight * 0.9f;
        
        // Calculer le nombre optimal de rangées et de colonnes
        int maxColumns = Mathf.FloorToInt(availableWidth / horizontalSpacing);
        int maxRows = Mathf.CeilToInt((float)totalCards / maxColumns);
        
        // S'assurer que nous avons au moins 2 rangées pour l'effet de vague
        maxRows = Mathf.Max(maxRows, 2);
        
        // Ajuster si nécessaire pour mieux répartir les cartes
        if (maxRows > 6) {
            maxRows = 6; // Limiter à 6 rangées maximum pour un meilleur effet visuel
            maxColumns = Mathf.CeilToInt((float)totalCards / maxRows);
        }
        
        float startX = -availableWidth / 2;
        float startY = availableHeight / 2 - verticalSpacing;
        
        // Calculer l'espacement horizontal dynamique
        float dynamicHorizontalSpacing = (maxColumns > 1) ? availableWidth / (maxColumns - 1) : 0;
        
        Debug.Log($"Wave pattern: {maxRows} rangées, {maxColumns} colonnes max");
        
        // Positionner les cartes en grille
        int cardIndex = 0;
        for (int row = 0; row < maxRows && cardIndex < totalCards; row++)
        {
            int cardsInThisRow = Mathf.Min(maxColumns, totalCards - cardIndex);
            float rowWidth = (cardsInThisRow - 1) * dynamicHorizontalSpacing;
            float rowStartX = -rowWidth / 2; // Centrer chaque rangée
            
            for (int col = 0; col < cardsInThisRow; col++)
            {
                if (cardIndex >= totalCards) break;
                
                RectTransform rectTransform = cards[cardIndex].GetComponent<RectTransform>();
                float xPos = rowStartX + col * dynamicHorizontalSpacing;
                float yPos = startY - row * verticalSpacing;
                
                // Positionnement direct sans animation
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                cardIndex++;
            }
        }
    }

    // Nouvelle méthode pour démarrer le mouvement ondulant
    private void StartWaveMovement()
    {
        Debug.Log("Démarrage du mouvement de vague amélioré");
        StopAllCardMovements();
        
        foreach (var card in cards)
        {
            if (card == null) continue;
            
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            Vector2 startPos = rectTransform.anchoredPosition;
            
            // Attribuer une phase aléatoire pour varier le mouvement
            float randomPhase = Random.Range(0f, Mathf.PI * 2);
            float amplitude = Random.Range(20f, 40f); // Amplitude de l'ondulation
            float frequency = Random.Range(0.8f, 1.2f); // Fréquence de l'ondulation
            float horizontalSpeed = Random.Range(50f, 100f) * currentLevel.waveMovementSpeed; // Vitesse de déplacement horizontal
            
            // Créer une séquence d'animation qui ne se termine jamais
            Sequence waveSequence = DOTween.Sequence();
            
            // Utiliser un callback OnUpdate pour créer le mouvement combiné
            waveSequence.SetLoops(-1) // Boucle infinie
                .SetUpdate(true)
                .OnUpdate(() => {
                    if (card == null || rectTransform == null) return;
                    
                    // Calculer le mouvement ondulant en fonction du temps
                    float time = Time.time * frequency * currentLevel.waveMovementSpeed;
                    float wave = amplitude * Mathf.Sin(time + randomPhase);
                    
                    // Calculer le mouvement horizontal
                    float horizontalMovement = Mathf.PingPong(Time.time * horizontalSpeed, playAreaWidth) - (playAreaWidth / 2);
                    
                    // Combiner les mouvements
                    rectTransform.anchoredPosition = new Vector2(horizontalMovement, startPos.y + wave);
                });
            
            activeTweens.Add(waveSequence);
        }
    }

    private void StartWaveWrapAroundMovement()
    {
        Debug.Log("Démarrage du mouvement de vague avec wrap around");
        StopAllCardMovements();
        
        foreach (var card in cards)
        {
            if (card == null) continue;
            
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            Vector2 startPos = rectTransform.anchoredPosition;
            
            // Attribuer une phase aléatoire pour varier le mouvement
            float randomPhase = Random.Range(0f, Mathf.PI * 2);
            float amplitude = Random.Range(20f, 40f); // Amplitude de l'ondulation
            float frequency = Random.Range(0.8f, 1.2f); // Fréquence de l'ondulation
            float horizontalSpeed = Random.Range(50f, 100f) * currentLevel.waveWrapAroundSpeed; // Vitesse de déplacement horizontal
            
            // Créer une séquence d'animation qui ne se termine jamais
            Sequence waveSequence = DOTween.Sequence();
            
            // Utiliser un callback OnUpdate pour créer le mouvement combiné
            waveSequence.SetLoops(-1) // Boucle infinie
                .SetUpdate(true)
                .OnUpdate(() => {
                    if (card == null || rectTransform == null) return;
                    
                    // Calculer le mouvement ondulant en fonction du temps
                    float time = Time.time * frequency * currentLevel.waveWrapAroundSpeed;
                    float wave = amplitude * Mathf.Sin(time + randomPhase);
                    
                    // Calculer le mouvement horizontal avec wrap around
                    float horizontalMovement = (Time.time * horizontalSpeed) % playAreaWidth;
                    if (horizontalMovement < 0) horizontalMovement += playAreaWidth;
                    horizontalMovement -= playAreaWidth / 2; // Centrer le mouvement
                    
                    // Combiner les mouvements
                    rectTransform.anchoredPosition = new Vector2(horizontalMovement, startPos.y + wave);
                });
            
            activeTweens.Add(waveSequence);
        }
    }

    // Nouvelle méthode pour gérer les dimensions selon l'état
    private void ApplyStateSpecificDimensions(GridState state)
    {
        // Si l'état est lié à Aligned ou Columns (moving ou non), utiliser les dimensions en dur
        if (state == GridState.AlignedMoving || state == GridState.ColumnsMoving || state == GridState.WaveWrapAround)
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

            float moveSpeed = 100f * currentLevel.alignedMovementSpeed;
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
            float speed = 100f * currentLevel.columnsMovementSpeed;
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
                    float rotationSpeed = 100f * currentLevel.circularMovementSpeed * (circle % 2 == 0 ? 1 : -1);
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
            float moveSpeed = currentLevel.pulsingMovementSpeed;
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
                        float randomTargetScale = Random.Range(0.8f, 3f); // Taille max augmentée à 1.5
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

    // Nouvelle méthode pour augmenter la vitesse en mode difficulté infinie
    public void IncreaseSpeedForInfiniteDifficulty()
    {
        if (!currentLevel.infiniteDifficultyEnabled)
            return;
            
        Debug.Log("Mode difficulté infinie: Augmentation de la vitesse");
        
        float speedIncrement = currentLevel.speedIncrementPerRound;
        const float MAX_SPEED = 5.0f;
        
        // Augmenter la vitesse de base tout en la limitant à MAX_SPEED
        currentLevel.moveSpeed = Mathf.Min(currentLevel.moveSpeed + speedIncrement, MAX_SPEED);
        
        // Augmenter les vitesses spécifiques aux patterns
        currentLevel.circularMovementSpeed = Mathf.Min(currentLevel.circularMovementSpeed + speedIncrement, MAX_SPEED);
        currentLevel.fastMovementSpeed = Mathf.Min(currentLevel.fastMovementSpeed + speedIncrement, MAX_SPEED);
        currentLevel.slowMovementSpeed = Mathf.Min(currentLevel.slowMovementSpeed + speedIncrement, MAX_SPEED);
        currentLevel.alignedMovementSpeed = Mathf.Min(currentLevel.alignedMovementSpeed + speedIncrement, MAX_SPEED);
        currentLevel.columnsMovementSpeed = Mathf.Min(currentLevel.columnsMovementSpeed + speedIncrement, MAX_SPEED);
        currentLevel.pulsingMovementSpeed = Mathf.Min(currentLevel.pulsingMovementSpeed + speedIncrement, MAX_SPEED);
        currentLevel.waveMovementSpeed = Mathf.Min(currentLevel.waveMovementSpeed + speedIncrement, MAX_SPEED);
        currentLevel.waveWrapAroundSpeed = Mathf.Min(currentLevel.waveWrapAroundSpeed + speedIncrement, MAX_SPEED);
        currentLevel.quantumTeleportSpeed = Mathf.Min(currentLevel.quantumTeleportSpeed + speedIncrement, MAX_SPEED);
        currentLevel.gravityWellsSpeed = Mathf.Min(currentLevel.gravityWellsSpeed + speedIncrement, MAX_SPEED);
        
        Debug.Log($"Vitesses augmentées de {speedIncrement}. Nouvelle vitesse de base: {currentLevel.moveSpeed}");
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
        Debug.Log("🔁 Réinitialisation du jeu après continuation 🔁");
        
        // Réinitialiser tous les états de contrôle
        isRouletteActive = false;
        isTransitioningDifficulty = false;
        
        StopAllCardMovements();
        StopAllCoroutines();
        
        // Détruire toutes les cartes existantes et nettoyer proprement
        Debug.Log($"Nettoyage des cartes existantes : {cards.Count} cartes");
        foreach (var existingCard in cards)
        {
            if (existingCard != null)
            {
                DOTween.Kill(existingCard.transform); // Arrêter toutes les animations
                Destroy(existingCard.gameObject);
            }
        }
        cards.Clear();
        wantedCard = null;
        
        // Réinitialiser l'historique des patterns
        lastUsedPatterns.Clear();
        
        // Initialiser une nouvelle grille mais ne pas animer tout de suite
        // IMPORTANT: Mettre false pour InitializeGrid pour ne pas animer les cartes
        // et éviter de déclencher automatiquement une roulette
        InitializeGrid(false); // false = ne pas animer encore
        
        // Valider et assurer qu'on a une carte wanted
        ValidateWantedCard();
        
        if (wantedCard != null)
        {
            // Informer le GameManager de la nouvelle wanted card
            // NOTE: Cela va déclencher la roulette UI
            GameManager.Instance.SelectNewWantedCharacter(wantedCard);
        }
        else
        {
            Debug.LogError("❌ Pas de wanted card après ResetGame!");
            // Fallback - animer les cartes directement au cas où
            AnimateCardsEntry();
        }
        
        Debug.Log("Jeu réinitialisé avec succès.");
    }

    // Nouvelle méthode pour le mouvement de téléportation quantique
    private void StartQuantumTeleport()
    {
        Debug.Log("Démarrage de la téléportation quantique améliorée");
        StopAllCardMovements();
        
        // Utiliser un seul Tween global pour gérer toutes les téléportations
        Sequence globalTeleportSequence = DOTween.Sequence();
        globalTeleportSequence.SetLoops(-1);
        globalTeleportSequence.SetUpdate(true);
        
        // Configuration des téléportations pour chaque carte
        foreach (var card in cards)
        {
            if (card == null) continue;
            
            // Téléporter immédiatement pour montrer l'effet
            DOVirtual.DelayedCall(Random.Range(0.5f, 2.0f), () => {
                if (card != null) TeleportCard(card, true);
            });
        }
        
        // Chaque intervalle, choisir des cartes aléatoires à téléporter
        globalTeleportSequence.AppendCallback(() => {
            // Téléporter un nombre aléatoire de cartes
            int numCardsToTeleport = Mathf.Max(1, cards.Count / 4); // Au moins 25% des cartes
            List<CharacterCard> shuffledCards = new List<CharacterCard>(cards);
            ShuffleList(shuffledCards);
            
            for (int i = 0; i < numCardsToTeleport && i < shuffledCards.Count; i++)
            {
                if (shuffledCards[i] != null)
                {
                    TeleportCard(shuffledCards[i], false);
                }
            }
        });
        
        // Intervalle entre les téléportations en masse
        globalTeleportSequence.AppendInterval(1.5f / currentLevel.quantumTeleportSpeed);
        
        activeTweens.Add(globalTeleportSequence);
        
        // Démarrer également des mouvements lents pour toutes les cartes
        foreach (var card in cards)
        {
            if (card == null) continue;
            // Mouvement très lent pour mieux voir les téléportations
            StartContinuousCardMovement(card, currentLevel.quantumTeleportSpeed * 0.4f);
        }
    }

    // Méthode pour téléporter une carte avec effet visuel amélioré
    private void TeleportCard(CharacterCard card, bool initialTeleport)
    {
        if (card == null) return;
        
        RectTransform rectTransform = card.GetComponent<RectTransform>();
        if (rectTransform == null) return;
        
        // Arrêter tout mouvement continu en cours pour cette carte
        DOTween.Kill(rectTransform);
        
        // Sauvegarder l'échelle actuelle
        Vector3 originalScale = card.transform.localScale;
        Vector2 originalPosition = rectTransform.anchoredPosition;
        
        // Obtenir une nouvelle position bien éloignée de la position actuelle
        Vector2 newPosition;
        int attempts = 0;
        do {
            newPosition = GetValidCardPosition();
            attempts++;
        } while (Vector2.Distance(originalPosition, newPosition) < playAreaWidth * 0.3f && attempts < 5);
        
        // Effet de flash avec scaling + changement de rotation
        Sequence teleportSequence = DOTween.Sequence();
        
        // Ajouter un petit flash de couleur avant la téléportation
        if (card.GetComponent<Image>() != null)
        {
            Image cardImage = card.GetComponent<Image>();
            Color originalColor = cardImage.color;
            
            // Flash en blanc
            teleportSequence.Append(
                cardImage.DOColor(new Color(1f, 1f, 1f, 1f), 0.1f)
            );
            
            // Revenir à la couleur d'origine après la téléportation
            teleportSequence.AppendCallback(() => {
                if (cardImage != null)
                    cardImage.DOColor(originalColor, 0.2f);
            });
        }
        
        // Disparition avec rétrécissement rapide
        teleportSequence.Append(
            card.transform.DOScale(Vector3.zero, 0.15f)
                .SetEase(Ease.InBack)
        );
        
        // Téléportation instantanée
        teleportSequence.AppendCallback(() => {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = newPosition;
                
                // Ajouter un effet particule ou flash à l'endroit d'apparition si possible
                Debug.Log($"Carte téléportée de {originalPosition} à {newPosition}");
            }
        });
        
        // Réapparition avec effet de rebond
        teleportSequence.Append(
            card.transform.DOScale(originalScale * 1.3f, 0.2f)
                .SetEase(Ease.OutBack)
        );
        
        // Retour à l'échelle normale
        teleportSequence.Append(
            card.transform.DOScale(originalScale, 0.1f)
                .SetEase(Ease.OutQuad)
        );
        
        // Jouer la séquence
        teleportSequence.Play();
        
        // Démarrer un nouveau mouvement lent depuis la nouvelle position après un court délai
        DOVirtual.DelayedCall(0.5f, () => {
            if (card != null && card.gameObject.activeInHierarchy)
            {
                // Mouvement lent pour mieux voir les téléportations suivantes
                StartContinuousCardMovement(card, currentLevel.quantumTeleportSpeed * 0.4f);
            }
        });
    }

    // Méthode utilitaire pour mélanger une liste
    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    
    // Variables pour les puits gravitationnels
    private List<Vector2> gravityWells = new List<Vector2>();
    private List<float> gravityWellStrengths = new List<float>();
    private List<float> gravityWellLifetimes = new List<float>();
    private List<float> gravityWellRadii = new List<float>();
    private const int MAX_GRAVITY_WELLS = 5;
    private const float MIN_WELL_DISTANCE = 200f;
    
    // Nouvelle méthode pour le mouvement des puits gravitationnels
    private void StartGravityWellsMovement()
    {
        Debug.Log("Démarrage du mouvement des puits gravitationnels");
        StopAllCardMovements();
        
        // Initialiser les cartes avec un mouvement lent
        foreach (var card in cards)
        {
            if (card == null) continue;
            StartContinuousCardMovement(card, currentLevel.gravityWellsSpeed * 0.3f);
        }
        
        // Créer une séquence pour gérer les puits gravitationnels
        Sequence gravitySequence = DOTween.Sequence();
        gravitySequence.SetLoops(-1);
        gravitySequence.SetUpdate(true);
        
        // Initialiser la liste des puits gravitationnels
        gravityWells.Clear();
        gravityWellStrengths.Clear();
        gravityWellLifetimes.Clear();
        gravityWellRadii.Clear();
        
        // Créer des puits gravitationnels initiaux
        CreateInitialGravityWells();
        
        // Cycle pour gérer les puits gravitationnels
        gravitySequence.AppendCallback(() => {
            // Mettre à jour les puits gravitationnels existants
            UpdateGravityWells();
            
            // Appliquer les forces gravitationnelles aux cartes
            ApplyGravityWellForces();
        });
        
        gravitySequence.AppendInterval(0.05f); // Mise à jour très fréquente
        
        activeTweens.Add(gravitySequence);
    }
    
    // Créer les puits gravitationnels initiaux
    private void CreateInitialGravityWells()
    {
        // Créer 2 puits gravitationnels initiaux
        for (int i = 0; i < 2; i++)
        {
            CreateGravityWell();
        }
    }
    
    // Créer un nouveau puits gravitationnel
    private void CreateGravityWell()
    {
        if (gravityWells.Count >= MAX_GRAVITY_WELLS) return;
        
        // Trouver une position valide pour le puits
        Vector2 wellPosition;
        int attempts = 0;
        do {
            float x = Random.Range(-playAreaWidth/2 * 0.8f, playAreaWidth/2 * 0.8f);
            float y = Random.Range(-playAreaHeight/2 * 0.8f, playAreaHeight/2 * 0.8f);
            wellPosition = new Vector2(x, y);
            attempts++;
            
            // Vérifier si la position est assez éloignée des autres puits
            bool tooClose = false;
            foreach (var existingWell in gravityWells)
            {
                if (Vector2.Distance(wellPosition, existingWell) < MIN_WELL_DISTANCE)
                {
                    tooClose = true;
                    break;
                }
            }
            
            if (!tooClose) break;
            
        } while (attempts < 10);
        
        // Force gravitationnelle aléatoire (positive = attraction, négative = répulsion)
        float strength = Random.Range(0.7f, 1.5f) * currentLevel.gravityWellsSpeed;
        if (Random.value < 0.3f) strength *= -1; // 30% de chance d'être répulsif
        
        // Durée de vie du puits gravitationnel
        float lifetime = Random.Range(3f, 6f);
        
        // Rayon d'influence
        float radius = Random.Range(250f, 500f);
        
        // Ajouter le puits gravitationnel aux listes
        gravityWells.Add(wellPosition);
        gravityWellStrengths.Add(strength);
        gravityWellLifetimes.Add(lifetime);
        gravityWellRadii.Add(radius);
        
        Debug.Log($"Nouveau puits gravitationnel créé à {wellPosition}, force={strength}, durée={lifetime}s, rayon={radius}");
    }
    
    // Mettre à jour les puits gravitationnels
    private void UpdateGravityWells()
    {
        // Mettre à jour la durée de vie des puits et supprimer ceux qui ont expiré
        for (int i = gravityWells.Count - 1; i >= 0; i--)
        {
            gravityWellLifetimes[i] -= Time.deltaTime;
            if (gravityWellLifetimes[i] <= 0)
            {
                // Supprimer le puits gravitationnel
                gravityWells.RemoveAt(i);
                gravityWellStrengths.RemoveAt(i);
                gravityWellLifetimes.RemoveAt(i);
                gravityWellRadii.RemoveAt(i);
                
                Debug.Log("Puits gravitationnel supprimé");
                
                // Créer un nouveau puits aléatoirement
                if (Random.value < 0.7f)
                {
                    CreateGravityWell();
                }
            }
        }
        
        // Créer un nouveau puits si besoin (faible probabilité)
        if (gravityWells.Count < MAX_GRAVITY_WELLS && Random.value < 0.005f)
        {
            CreateGravityWell();
        }
    }
    
    // Appliquer les forces gravitationnelles aux cartes
    private void ApplyGravityWellForces()
    {
        foreach (var card in cards)
        {
            if (card == null) continue;
            
            RectTransform cardRect = card.GetComponent<RectTransform>();
            Vector2 cardPosition = cardRect.anchoredPosition;
            Vector2 totalForce = Vector2.zero;
            
            // Calculer la force totale exercée par tous les puits
            for (int i = 0; i < gravityWells.Count; i++)
            {
                Vector2 wellPosition = gravityWells[i];
                float strength = gravityWellStrengths[i];
                float radius = gravityWellRadii[i];
                
                // Calculer la distance au puits
                float distance = Vector2.Distance(cardPosition, wellPosition);
                
                // Appliquer la force si la carte est dans le rayon d'influence
                if (distance < radius)
                {
                    // Calculer la direction vers le puits
                    Vector2 direction = (wellPosition - cardPosition).normalized;
                    
                    // Force inversement proportionnelle au carré de la distance (loi gravitationnelle)
                    float forceMagnitude = strength * (1 - (distance / radius));
                    
                    // Ajouter cette force à la force totale
                    totalForce += direction * forceMagnitude;
                }
            }
            
            // Appliquer la force à la carte si elle est significative
            if (totalForce.magnitude > 0.01f)
            {
                // Limiter la magnitude de la force
                if (totalForce.magnitude > 10f)
                {
                    totalForce = totalForce.normalized * 10f;
                }
                
                // Appliquer la force comme déplacement
                Vector2 newPosition = cardPosition + (totalForce * Time.deltaTime * 60f);
                
                // Maintenir la carte dans la zone de jeu
                newPosition.x = Mathf.Clamp(newPosition.x, -playAreaWidth/2 * 0.9f, playAreaWidth/2 * 0.9f);
                newPosition.y = Mathf.Clamp(newPosition.y, -playAreaHeight/2 * 0.9f, playAreaHeight/2 * 0.9f);
                
                // Déplacer la carte
                cardRect.anchoredPosition = newPosition;
            }
        }
    }

    private void PositionCardsForWaveWrapAroundMovement()
    {
        Debug.Log("Positionnement initial des cartes pour le mouvement de vague avec wrap around");
        StopAllCardMovements();
        
        // Mélanger les cartes pour une distribution aléatoire
        ShuffleCards();
        
        int totalCards = cards.Count;
        float availableWidth = playAreaWidth * 0.9f;
        float availableHeight = playAreaHeight * 0.85f; // Utiliser 85% de la hauteur
        
        // Pour ce pattern, on va placer les cartes sur un nombre limité de lignes
        int maxRows = currentLevel.waveWrapAroundMaxRows; // Utiliser la valeur du niveau de difficulté actuel
        
        // Calculer le nombre de cartes par ligne
        int cardsPerRow = Mathf.CeilToInt((float)totalCards / maxRows);
        if (cardsPerRow < 1) cardsPerRow = 1;
        
        // Calculer le nombre de lignes nécessaires (ne pas dépasser maxRows)
        int rows = Mathf.Min(maxRows, Mathf.CeilToInt((float)totalCards / cardsPerRow));
        if (rows < 1) rows = 1;
        
        // Calculer le spacing horizontal pour répartir les cartes uniformément
        float actualHorizontalSpacing = availableWidth / cardsPerRow;
        
        // Estimer la hauteur d'une carte
        float cardHeight = 200f;
        
        // Calculer la position de départ horizontale
        float startX = -availableWidth / 2 + (actualHorizontalSpacing / 2); // Centrer horizontalement
        
        // Décider de la position verticale en fonction du nombre de lignes
        float startY;
        
        if (rows <= 5) {
            // Pour 1-5 lignes: centrer verticalement
            // Calculer l'espace total occupé par les cartes verticalement
            float totalCardsHeight = (rows - 1) * verticalSpacing + cardHeight;
            
            // Centrer simplement en divisant l'espace disponible
            startY = totalCardsHeight / 2;
            
            // Si une seule ligne ou deux lignes, s'assurer que les cartes sont vraiment au centre
            if (rows <= 2) {
                startY = 0; // Position centrale exacte
            }
            
            Debug.Log($"WaveWrapAround - Lignes (1-5): {rows} - Mode centré - Start Y: {startY}, Total height: {totalCardsHeight}");
        } else {
            // Pour 6-10 lignes: aligner en haut avec un padding de 3%
            float topPadding = availableHeight * 0.03f; // 3% de padding en haut
            startY = availableHeight / 2 - topPadding;
            Debug.Log($"WaveWrapAround - Lignes (6-10): {rows} - Mode haut (padding 3%) - Start Y: {startY}");
        }
        
        // Positionner les cartes avec une variation de hauteur initiale pour un effet de vague
        for (int i = 0; i < totalCards; i++)
        {
            if (cards[i] == null) continue;
            
            RectTransform rectTransform = cards[i].GetComponent<RectTransform>();
            if (rectTransform == null) continue;
            
            // Calculer la position dans la grille
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;
            
            // Ajouter une variation de hauteur aléatoire pour un effet de vague initial
            float initialWaveOffset = Mathf.Sin(col * 0.5f) * 20f; // Variation sinusoïdale de 20 unités
            
            // Calculer la position finale
            float xPos = startX + (col * actualHorizontalSpacing);
            float yPos = startY - (row * verticalSpacing) + initialWaveOffset;
            
            // Appliquer la position
            rectTransform.anchoredPosition = new Vector2(xPos, yPos);
            
            // Appliquer une légère rotation aléatoire pour plus de naturel
            rectTransform.rotation = Quaternion.Euler(0, 0, Random.Range(-5f, 5f));
        }
    }
    #endregion
}
