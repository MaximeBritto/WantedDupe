using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas Orders")]
    public Canvas mainCanvas;
    public Canvas gridCanvas;
    public Canvas uiCanvas;
    public Canvas overlayCanvas;

    [Header("Wanted Panel")]
    public Image wantedCharacterImage;
    public RectTransform wantedPanel;
    public RectTransform wantedImageRect;
    
    [Header("Game Info")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI bestScoreText;  // Nouveau texte pour le meilleur score
    public RectTransform gameInfoPanel;
    
    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public Button startButton;
    public Button restartButton;

    [Header("Roulette Effect")]
    public float rouletteDuration = 2f;      // Durée totale de la roulette
    public float changeImageDelay = 0.1f;      // Délai entre chaque changement d'image

    [Header("Wanted Panel Sizes")]
    public Vector2 finalWantedPosition = new Vector2(0, -100);
    public Vector2 finalWantedSize = new Vector2(300, 400);
    public Vector2 rouletteWantedSize = new Vector2(500, 600);
    public Vector2 roulettePosition = new Vector2(0, 0);
    public float rouletteScale = 1.2f;
    public float wantedImageScale = 0.6f;
    public bool isRouletteRunning = false;
    private bool gridManagerRouletteActive = false;

    [Header("Game Board")]
    public RectTransform gameBoard;
    public Image gameBoardImage;

    [Header("Mobile Settings")]
    public bool isMobileDevice;
    public float mobileScaleFactor = 0.7f;
    public Vector2 mobileWantedSize = new Vector2(200, 300);
    public Vector2 mobileWantedPosition = new Vector2(0, 800);

    [Header("Tablet Settings")]
    public bool isTabletDevice;
    public float tabletScaleFactor = 0.85f;
    public Vector2 tabletWantedSize = new Vector2(300, 400);
    public Vector2 tabletWantedPosition = new Vector2(0, 600);
    public float tabletCardSpacing = 150f;
    public float tabletTimerScale = 1.2f;
    public float tabletTimerFontSize = 80f;
    public float tabletScoreFontSize = 70f;

    [Header("Safe Area")]
    public GameObject SafeArea;
    public GameObject Board;

    [Header("Difficulty Display")]
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI currentStateText;

    [Header("Background")]
    public BackgroundManager backgroundManager;

    [Header("Continue Game")]
    public Button continueButton;  // Bouton pour regarder la pub et continuer
    private bool hasUsedContinue = false;  // Flag pour suivre si le joueur a déjà utilisé le continue
    private bool isNewGame = true;  // Flag pour suivre si c'est une nouvelle partie

    [Header("Combo Images")]
    public Image[] comboImages;  // Référence aux 5 images de combo

    [Header("Score Display")]
    public TextMeshProUGUI increScore;  // Nouveau texte pour afficher le score par 5

    private Vector2 timerInitialPosition;
    private AdMobAdsScript adMobAdsScript;
    private float prevTimeRemaining = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
            
        // Détecter le type d'appareil
        isMobileDevice = Application.isMobilePlatform;
        isTabletDevice = IsTablet();
        
        // Appliquer les ajustements appropriés
        if (isTabletDevice)
        {
            AdjustUIForTablet();
        }
        else if (isMobileDevice)
        {
            AdjustUIForMobile();
        }
    }

    // Détection des tablettes basée sur la taille d'écran
    private bool IsTablet()
    {
        // Vérifier si c'est un appareil mobile d'abord
        if (!Application.isMobilePlatform)
            return false;
            
        // Résolution minimum d'une tablette (en général 1280x720 ou plus)
        float minTabletDiagonal = 2200f; // Valeur approximative pour identifier une tablette
        
        // Calculer la diagonale en pixels
        float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
        
        // Log pour le débogage
        Debug.Log($"Détection tablette: Diagonale écran = {screenDiagonal}px, Width = {Screen.width}, Height = {Screen.height}");
        
        return screenDiagonal >= minTabletDiagonal;
    }

    // Nouvelle méthode pour configurer l'UI tablette
    private void AdjustUIForTablet()
    {
        Debug.Log("Configuration de l'interface pour tablette");
        
        // Configurer le wanted panel
        finalWantedSize = tabletWantedSize;
        finalWantedPosition = tabletWantedPosition;
        
        // Configurer le timer
        if (timerText != null)
        {
            timerText.fontSize = tabletTimerFontSize;
            timerText.enableAutoSizing = false;
            timerText.rectTransform.localScale = new Vector3(tabletTimerScale, tabletTimerScale, tabletTimerScale);
        }
        
        // Configurer le score
        if (scoreText != null)
        {
            scoreText.fontSize = tabletScoreFontSize;
            scoreText.enableAutoSizing = false;
        }
        
        // Informer le GridManager des ajustements pour tablette
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            gridManager.playAreaWidth = Screen.width * 0.85f;
            gridManager.playAreaHeight = Screen.height * 0.7f;
            gridManager.cardSpacing = tabletCardSpacing;
            gridManager.horizontalSpacing = tabletCardSpacing;
        }
    }

    // Nouvelle méthode pour configurer l'UI mobile avec un contrôle plus précis
    private void AdjustUIForMobile()
    {
        // Configurer le wanted panel
        finalWantedSize = mobileWantedSize;
        finalWantedPosition = mobileWantedPosition;
        
        // Configurer spécifiquement le timer de façon moins invasive
        if (timerText != null)
        {
            // Modifier seulement les paramètres critiques pour la visibilité
            timerText.fontSize = 100; // Taille TRÈS grande pour le mobile
            timerText.enableAutoSizing = false; // Désactiver l'auto-sizing
            
            // Augmenter l'échelle du timer
            timerText.rectTransform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            
            // Log pour débogage
            Debug.Log($"Timer configuré pour mobile: fontSize={timerText.fontSize}, scale={timerText.rectTransform.localScale}");
        }
        
        // Configurer spécifiquement le score
        if (scoreText != null)
        {
            scoreText.fontSize = 60;
            scoreText.enableAutoSizing = false;
        }
    }

    private void Start()
    {
        gridCanvas.sortingOrder = 0;
        uiCanvas.sortingOrder = 1;
        overlayCanvas.sortingOrder = 2;

        // Initialiser increScore à 0
        if (increScore != null)
        {
            increScore.text = "0";
        }

        DisableRaycastOnPanel(wantedPanel);
        DisableRaycastOnPanel(gameInfoPanel);
        
        GameManager.Instance.onGameStart.AddListener(OnGameStart);
        GameManager.Instance.onGameOver.AddListener(OnGameOver);
        GameManager.Instance.onNewWantedCharacter.AddListener(UpdateWantedCharacter);
        GameManager.Instance.onScoreChanged.AddListener(UpdateScoreText);
        
        menuPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        
        startButton.onClick.AddListener(StartGame);
        restartButton.onClick.AddListener(StartGame);

        if (gameBoardImage != null)
        {
            gameBoardImage.color = new Color(0, 0, 0, 0.2f);
        }

        // Déjà configuré dans Awake
        isMobileDevice = Application.isMobilePlatform;

        if (SafeArea != null)
        {
            SafeArea.SetActive(false);
            Board.SetActive(false);
        }
        if (uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(false);
        }

        if (backgroundManager != null)
        {
            backgroundManager.gameObject.SetActive(true);
        }

        if (timerText != null)
        {
            timerInitialPosition = timerText.rectTransform.anchoredPosition;
        }

        adMobAdsScript = FindObjectOfType<AdMobAdsScript>();
        
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners(); // Supprimer les listeners existants
            continueButton.onClick.AddListener(() => {
                // Désactiver le bouton immédiatement pour éviter les doubles clics
                continueButton.interactable = false;
                
                Debug.Log("Bouton Continue cliqué - tentative d'affichage de la pub récompensée");
                
                if (adMobAdsScript != null)
                {
                    // Vérifier si la pub est déjà chargée
                    bool isAdLoaded = adMobAdsScript.IsRewardedAdLoaded();
                    Debug.Log($"État de la pub récompensée: {(isAdLoaded ? "Chargée" : "Non chargée")}");
                    
                    if (!isAdLoaded)
                    {
                        // Tenter un chargement immédiat
                        adMobAdsScript.LoadRewardedAd();
                        
                        // Attendre un peu et vérifier à nouveau
                        StartCoroutine(TryShowRewardedAfterDelay());
                    }
                    else
                    {
                        // La pub est déjà chargée, on peut l'afficher directement
                        adMobAdsScript.ShowRewardedAd();
                        HideContinueButton();
                    }
                }
                else
                {
                    Debug.LogError("AdMobAdsScript est null - impossible d'afficher la pub récompensée");
                    // Ré-activer le bouton en cas d'erreur
                    continueButton.interactable = true;
                }
            });
        }

        // Rendre toutes les images de combo invisibles au démarrage
        if (comboImages != null)
        {
            foreach (Image comboImage in comboImages)
            {
                if (comboImage != null)
                {
                    comboImage.gameObject.SetActive(false);
                }
            }
        }
        
        // Démarrer la musique du menu
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StartMenuMusic();
        }
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
        if (GameManager.Instance != null && GameManager.Instance.isGameActive)
        {
            UpdateUI();
            
            // Si nous sommes sur mobile ou tablette, vérifier périodiquement l'affichage du timer
            if ((isMobileDevice || isTabletDevice) && Time.frameCount % 60 == 0 && timerText != null)
            {
                // Vérifier si le timer semble trop petit
                bool needsFixing = false;
                
                if (isTabletDevice && (timerText.fontSize < tabletTimerFontSize || timerText.rectTransform.localScale.x < tabletTimerScale))
                {
                    needsFixing = true;
                }
                else if (isMobileDevice && (timerText.fontSize < 90 || timerText.rectTransform.localScale.x < 1.4f))
                {
                    needsFixing = true;
                }
                
                if (needsFixing)
                {
                    FixTimerDisplay();
                }
            }
        }
    }

    private void UpdateUI()
    {
        if (GameManager.Instance == null) return;
        
        // Mettre à jour le score
        scoreText.text = $"{GameManager.Instance.displayedScore}";
        
        // Mettre à jour le timer
        float timeRemaining = GameManager.Instance.timeRemaining;
        string timeText = $"{Mathf.CeilToInt(timeRemaining)}";
        
        // Appliquer le texte seulement s'il a changé pour éviter des rafraîchissements inutiles
        if (timerText.text != timeText) {
            timerText.text = timeText;
            
            // Sur mobile, vérifier que la taille de police est correcte après chaque mise à jour
            if (isMobileDevice) {
                if (timerText.fontSize < 90 || timerText.rectTransform.localScale.x < 1.4f) {
                    FixTimerDisplay();
                }
            }
        }
        
        if (timeRemaining <= 10f)
        {
            timerText.color = Color.red;
            if (!DOTween.IsTweening(timerText.transform))
            {
                timerText.rectTransform.DOShakeAnchorPos(0.5f, 5f, 20, 90, false, true)
                    .SetLoops(-1, LoopType.Restart)
                    .SetId("TimerShake");
            }
        }
        else
        {
            // Vérifier si le temps a augmenté depuis la dernière frame
            if (prevTimeRemaining > 0 && timeRemaining > prevTimeRemaining && 
                Mathf.FloorToInt(timeRemaining) > Mathf.FloorToInt(prevTimeRemaining))
            {
                // Animer le timer quand il augmente
                AnimateTimerIncrease();
            }
            
            timerText.color = Color.white;
            if (DOTween.IsTweening("TimerShake"))
            {
                DOTween.Kill("TimerShake");
                timerText.rectTransform.anchoredPosition = timerInitialPosition;
            }
        }
        
        // Sauvegarder le temps actuel pour comparer à la prochaine frame
        prevTimeRemaining = timeRemaining;
    }
    
    private void AnimateTimerIncrease()
    {
        // Arrêter toute animation en cours sur le timer
        DOTween.Kill(timerText.transform);
        
        // Changer la couleur du timer en vert pour indiquer le bonus
        timerText.color = Color.green;
        
        // Créer une séquence d'animation
        Sequence seq = DOTween.Sequence();
        
        // Agrandir légèrement le texte
        seq.Append(timerText.transform.DOScale(1.3f, 0.15f).SetEase(Ease.OutBack))
            .Append(timerText.transform.DOScale(1f, 0.1f));
        
        // Ajouter un petit rebond
        seq.Join(timerText.rectTransform.DOShakePosition(0.2f, 5f, 3, 90, false, true));
        
        // Revenir à la couleur blanche après l'animation
        seq.OnComplete(() => {
            timerText.color = Color.white;
        });
    }

    // Méthode appelée par GridManager quand sa roulette démarre
    public void OnGridManagerRouletteStarted()
    {
        gridManagerRouletteActive = true;
        Debug.Log("UIManager notifié: GridManager roulette démarrée");
    }
    
    // Méthode appelée par GridManager quand sa roulette se termine
    public void OnGridManagerRouletteEnded()
    {
        gridManagerRouletteActive = false;
        Debug.Log("UIManager notifié: GridManager roulette terminée");
    }

    private void UpdateWantedCharacter(CharacterCard character)
    {
        // Si une roulette est déjà en cours, ne pas en démarrer une nouvelle
        if (isRouletteRunning)
        {
            Debug.LogWarning("UIManager: une roulette est déjà en cours - Ignorée");
            return;
        }
        
        Debug.Log("UIManager: Démarrage de la roulette UI avec le sprite: " + character.characterSprite.name);
        StartCoroutine(WantedRouletteEffect(character));
    }

    private IEnumerator WantedRouletteEffect(CharacterCard finalCharacter)
    {
        // Vérifier à nouveau si une roulette est déjà active
        if (isRouletteRunning)
        {
            Debug.LogWarning("UIManager: WantedRouletteEffect annulé - une roulette est déjà active");
            yield break;
        }
            
        GameManager.Instance.PauseGame();
        isRouletteRunning = true;
        
        // S'assurer que le gridCanvas est actif
        bool wasGridActive = gridCanvas.gameObject.activeSelf;
        
        // Désactiver temporairement la grille pour éviter un pattern en arrière-plan
        gridCanvas.gameObject.SetActive(false);
        
        // Vérifications de sécurité pour la carte à afficher
        if (finalCharacter == null)
        {
            Debug.LogError("❌ ERREUR: WantedRouletteEffect appelé avec une carte null!");
            
            // Utiliser une carte par défaut (celle du GameManager si disponible)
            if (GameManager.Instance.wantedCharacter != null)
            {
                finalCharacter = GameManager.Instance.wantedCharacter;
                Debug.Log("♻️ Récupération de la carte depuis GameManager: " + finalCharacter.characterName);
            }
            else
            {
                // Situation critique - terminer la roulette sans animation
                isRouletteRunning = false;
                gridCanvas.gameObject.SetActive(wasGridActive);
                GameManager.Instance.ResumeGame();
                yield break;
            }
        }
        
        if (finalCharacter.characterSprite == null)
        {
            Debug.LogError("❌ ERREUR: La carte finale n'a pas de sprite!");
            // Assigner un sprite par défaut
            finalCharacter.characterSprite = GameManager.Instance.GetRandomSprite();
        }
        
        Debug.Log($"🎲 Démarrage de la roulette UI pour: {finalCharacter.characterName} (sprite: {finalCharacter.characterSprite.name})");
        
        if (wantedCharacterImage.sprite == null)
        {
            wantedCharacterImage.sprite = GameManager.Instance.GetRandomSprite();
        }
        
        // Animation de démarrage de la roulette
        Sequence startSequence = DOTween.Sequence();
        startSequence.Join(wantedPanel.DOAnchorPos(roulettePosition, 0.5f))
                     .Join(wantedPanel.DOSizeDelta(rouletteWantedSize, 0.5f))
                     .Join(wantedPanel.transform.DOScale(rouletteScale, 0.5f));
        yield return startSequence.WaitForCompletion();

        float elapsedTime = 0;
        while (elapsedTime < rouletteDuration)
        {
            Sprite randomSprite = GameManager.Instance.GetRandomSprite();
            wantedCharacterImage.sprite = randomSprite;
            float imageRatio = randomSprite.rect.width / randomSprite.rect.height;
            float rouletteHeight = rouletteWantedSize.y * wantedImageScale;
            float rouletteWidth = rouletteHeight * imageRatio;
            wantedImageRect.sizeDelta = new Vector2(rouletteWidth, rouletteHeight);
            yield return new WaitForSeconds(changeImageDelay);
            elapsedTime += changeImageDelay;
        }

        // Ralentir la roulette en fin de cycle
        float[] finalDelays = { 0.2f, 0.3f, 0.4f, 0.5f };
        foreach (float delay in finalDelays)
        {
            Sprite randomSprite = GameManager.Instance.GetRandomSprite();
            wantedCharacterImage.sprite = randomSprite;
            yield return new WaitForSeconds(delay);
        }

        // IMPORTANT: S'assurer que le sprite final est correctement affiché
        // Double vérification pour être sûr que la carte finale est à jour
        CharacterCard currentWanted = GameManager.Instance.wantedCharacter;
        if (currentWanted != null && currentWanted != finalCharacter)
        {
            Debug.LogWarning("⚠️ Changement détecté pendant la roulette!");
            Debug.Log($"Original: {finalCharacter.characterName} -> Nouveau: {currentWanted.characterName}");
            finalCharacter = currentWanted;
        }
        
        // Afficher le sprite final du wanted
        if (finalCharacter != null && finalCharacter.characterSprite != null)
        {
            wantedCharacterImage.sprite = finalCharacter.characterSprite;
            Debug.Log($"✅ Fin de la roulette - Affichage du sprite final: {finalCharacter.characterSprite.name}");
        }
        else
        {
            Debug.LogError("❌ ERREUR: Impossible d'afficher le sprite final - wanted ou sprite null!");
        }
        
        AudioManager.Instance.PlayCorrect();

        yield return new WaitForSeconds(0.5f);

        // Calcul de la taille finale de l'image
        float finalImageRatio = finalCharacter.characterSprite.rect.width / finalCharacter.characterSprite.rect.height;
        float finalHeight = finalWantedSize.y * wantedImageScale;
        float finalWidth = finalHeight * finalImageRatio;
        Vector2 finalImageSize = new Vector2(finalWidth, finalHeight);

        Sequence endSequence = DOTween.Sequence();
        endSequence.Join(wantedPanel.DOAnchorPos(finalWantedPosition, 0.5f))
                   .Join(wantedPanel.DOSizeDelta(finalWantedSize, 0.5f))
                   .Join(wantedPanel.transform.DOScale(1f, 0.5f))
                   .Join(wantedImageRect.DOSizeDelta(finalImageSize, 0.5f));
        yield return endSequence.WaitForCompletion();

        // IMPORTANT: S'assurer que le canvas de la grille est réactivé
        if (!gridCanvas.gameObject.activeSelf)
        {
            Debug.Log("IMPORTANT: Réactivation du gridCanvas qui était inactif");
            gridCanvas.gameObject.SetActive(true);
        }
        
        // Attendre un court délai pour s'assurer que la grille est complètement réactivée
        yield return new WaitForSeconds(0.1f);
        
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            // Assurer la synchronisation avec la grille
            if (gridManager.wantedCard != finalCharacter)
            {
                Debug.LogWarning("⚠️ Désynchronisation entre UIManager et GridManager");
                if (GameManager.Instance.wantedCharacter != null)
                {
                    Debug.Log($"Correction avec GameManager.wantedCharacter: {GameManager.Instance.wantedCharacter.characterName}");
                    finalCharacter = GameManager.Instance.wantedCharacter;
                }
            }
            
            // Anime toutes les cartes
            gridManager.AnimateCardsEntry();
        }
        else
        {
            Debug.LogError("GridManager non trouvé!");
        }

        // Reprendre la partie quand toutes les animations sont terminées
        GameManager.Instance.ResumeGame();
        isRouletteRunning = false;
    }
    
    // Méthode de secours qui force l'affichage des cartes après un délai
    private IEnumerator ForceShowCardsBackup(GridManager gridManager, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Si les cartes ne sont toujours pas visibles, les forcer à l'être
        if (gridManager != null)
        {
            bool anyInvisibleCards = false;
            bool anyInvalidScale = false;
            
            Debug.Log("SOLUTION DE SECOURS: Vérification des cartes...");
            
            foreach (var card in gridManager.cards)
            {
                if (card != null)
                {
                    // Vérifier si la carte est invisible
                    if (!card.gameObject.activeSelf)
                    {
                        anyInvisibleCards = true;
                        card.gameObject.SetActive(true);
                        Debug.Log($"Carte {card.name} forcée à être active");
                    }
                    
                    // Vérifier si la carte a une échelle incorrecte
                    if (Vector3.Distance(card.transform.localScale, Vector3.one) > 0.01f)
                    {
                        anyInvalidScale = true;
                        Debug.Log($"Carte {card.name} avait une échelle incorrecte: {card.transform.localScale}");
                        
                        // Arrêter toute animation en cours sur cette carte
                        DOTween.Kill(card.transform);
                        
                        // Forcer l'échelle à exactement 1
                        card.transform.localScale = Vector3.one;
                    }
                }
            }
            
            if (anyInvisibleCards || anyInvalidScale)
            {
                Debug.LogWarning("SOLUTION DE SECOURS APPLIQUÉE: Des problèmes de visibilité ou d'échelle des cartes ont été corrigés");
                
                // Tenter de réactiver la grille si elle était désactivée
                if (!gridCanvas.gameObject.activeSelf)
                {
                    gridCanvas.gameObject.SetActive(true);
                    Debug.LogWarning("SOLUTION DE SECOURS: Le canvas de la grille a été réactivé");
                }
                
                // IMPORTANT: Ne PAS appeler ArrangeCardsBasedOnState ici car cela pourrait
                // causer un double placement des cartes et générer l'effet visuel indésirable
                // Laisser les cartes à leur position actuelle
                Debug.Log("SOLUTION DE SECOURS: Cartes corrigées sans repositionnement pour éviter le bug visuel");
            }
            else
            {
                Debug.Log("SOLUTION DE SECOURS: Toutes les cartes sont correctement visibles et à l'échelle 1");
            }
        }
    }

    public void OnGameStart()
    {
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        
        // Réinitialiser increScore à 0
        if (increScore != null)
        {
            increScore.text = "0";
        }

        // Faire disparaître toutes les images de combo
        if (comboImages != null)
        {
            foreach (Image img in comboImages)
            {
                if (img != null)
                {
                    img.gameObject.SetActive(false);
                    img.transform.localScale = Vector3.one;
                }
            }
        }

        // S'assurer que les boutons sont actifs et interactifs pour la prochaine fois
        if (restartButton != null)
        {
            restartButton.interactable = true;
        }
        
        if (continueButton != null)
        {
            continueButton.interactable = true;
        }

        if (SafeArea != null)
        {
            SafeArea.SetActive(true);
            Board.SetActive(true);
        }
        if (uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(true);
            
            // Pour les appareils mobiles, corriger le timer au démarrage du jeu
            if (isMobileDevice && timerText != null)
            {
                // Petit délai pour s'assurer que tout est initialisé
                Invoke("FixTimerDisplay", 0.1f);
            }
        }

        if (wantedPanel != null)
        {
            wantedPanel.transform.localScale = Vector3.one;
            wantedPanel.sizeDelta = finalWantedSize;
            wantedPanel.anchoredPosition = finalWantedPosition;
            
            if (wantedImageRect != null && wantedCharacterImage.sprite != null)
            {
                float imageRatio = wantedCharacterImage.sprite.rect.width / wantedCharacterImage.sprite.rect.height;
                float height = finalWantedSize.y * wantedImageScale;
                float width = height * imageRatio;
                wantedImageRect.sizeDelta = new Vector2(width, height);
            }
        }
    }

    private void OnGameOver()
    {
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        if (SafeArea != null)
        {
            SafeArea.SetActive(false);
            Board.SetActive(false);
        }
        if (uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(false);
        }
        
        if (GameManager.Instance != null)
        {
            // Afficher le score actuel
            finalScoreText.text = $"Score : {GameManager.Instance.displayedScore}";
            
            // Afficher le meilleur score
            if (bestScoreText != null)
            {
                bestScoreText.text = $"Best Score : {GameManager.Instance.bestScore}";
            }
        }

        // CORRECTION: S'assurer que le bouton restart est toujours interactif
        if (restartButton != null)
        {
            restartButton.interactable = true;
            // Réassigner le listener au cas où il aurait été perdu
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(StartGame);
        }

        // Gérer l'affichage du bouton continue
        if (continueButton != null)
        {
            // Afficher le bouton continue uniquement si c'est une première partie
            // ou si le joueur n'a pas déjà utilisé le continue dans cette session
            bool shouldShowContinue = isNewGame && !hasUsedContinue;
            continueButton.gameObject.SetActive(shouldShowContinue);
            continueButton.interactable = true; // S'assurer que le bouton est interactif
            
            Debug.Log($"Bouton continue affiché: {shouldShowContinue}, isNewGame: {isNewGame}, hasUsedContinue: {hasUsedContinue}");
            
            // Précharger la pub récompensée si le bouton est affiché
            if (shouldShowContinue && adMobAdsScript != null)
            {
                Debug.Log("Préchargement de la pub récompensée pour le bouton Continue");
                adMobAdsScript.LoadRewardedAd();
            }
        }
        
        // Démarrer la musique du menu game over
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StartMenuMusic();
        }
    }

    private void StartGame()
    {
        Debug.Log("StartGame appelé - Réinitialisation des flags isNewGame et hasUsedContinue");
        
        // IMPORTANT: Toujours réinitialiser ces flags lors du démarrage d'une nouvelle partie
        isNewGame = true;
        hasUsedContinue = false;
        
        // Mettre à jour l'affichage du meilleur score
        if (bestScoreText != null && GameManager.Instance != null)
        {
            bestScoreText.text = $"Meilleur Score : {GameManager.Instance.bestScore}";
        }
        
        // S'assurer que les boutons sont dans un état correct
        if (restartButton != null)
        {
            restartButton.interactable = true;
        }
        
        if (continueButton != null)
        {
            continueButton.interactable = true;
        }
        
        GameManager.Instance.StartGame();
    }

    public void UpdateDifficultyText(int threshold, GridManager.GridState state)
    {
        if (difficultyText != null)
        {
            difficultyText.text = $"Niveau {threshold / 500 + 1}";
        }
        if (currentStateText != null)
        {
            string stateText = state switch
            {
                GridManager.GridState.Aligned => "Mode Aligné",
                GridManager.GridState.Columns => "Mode Colonnes",
                GridManager.GridState.Static => "Mode Statique",
                GridManager.GridState.SlowMoving => "Mode Mobile Lent",
                GridManager.GridState.FastMoving => "Mode Mobile Rapide",
                _ => state.ToString()
            };
            currentStateText.text = stateText;
        }
    }

    private void UpdateScoreText(float score)
    {
        if (GameManager.Instance.isGameActive)
        {            
            int scoreModulo = (int)GameManager.Instance.displayedScore % 5;
            
            if (scoreModulo == 0 && GameManager.Instance.displayedScore > 0)
            {
                if (comboImages != null && comboImages.Length >= 5)
                {
                    comboImages[4].gameObject.SetActive(true);
                    comboImages[4].transform.localScale = Vector3.zero;
                    
                    comboImages[4].transform.DOScale(1.2f, 0.3f)
                        .SetEase(Ease.OutBack)
                        .OnComplete(() => {
                            StartCoroutine(DisappearAllImagesAndUpdateScore(0.5f));
                        });
                    AudioManager.Instance?.PlayComboSound();
                }
                return;
            }
            
            if (comboImages != null)
            {
                for (int i = 0; i < comboImages.Length; i++)
                {
                    if (comboImages[i] != null)
                    {
                        bool shouldBeActive = i < scoreModulo;
                        
                        if (shouldBeActive && !comboImages[i].gameObject.activeSelf)
                        {
                            comboImages[i].gameObject.SetActive(true);
                            comboImages[i].transform.localScale = Vector3.zero;
                            
                            comboImages[i].transform.DOScale(1.2f, 0.3f)
                                .SetEase(Ease.OutBack)
                                .OnComplete(() => {
                                    comboImages[i].transform.DOScale(1f, 0.15f).SetEase(Ease.InOutBack);
                                });
                            AudioManager.Instance?.PlayComboSound();
                        }
                    }
                }
            }
        }
    }

    private IEnumerator DisappearAllImagesAndUpdateScore(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Jouer le son de disparition
        AudioManager.Instance?.PlayComboDisappearSound();
        
        // Animation de disparition des images
        foreach (Image img in comboImages)
        {
            if (img != null)
            {
                img.transform.DOScale(0f, 0.3f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => {
                        img.gameObject.SetActive(false);
                        img.transform.localScale = Vector3.one;
                    });
            }
        }

        // Attendre que l'animation de disparition soit terminée
        yield return new WaitForSeconds(0.3f);

        // Mettre à jour et animer le increScore
        int scoreBy5 = ((int)GameManager.Instance.displayedScore / 5) * 5;
        increScore.text = scoreBy5.ToString();
        
        // Jouer le son d'augmentation du score
        AudioManager.Instance?.PlayScoreIncreaseSound();
        
        // Animation du score
        increScore.transform.DOScale(1.5f, 0.3f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
                increScore.transform.DOScale(1f, 0.15f).SetEase(Ease.InOutBack);
            });
            
        // Ajouter une rotation pour plus d'effet
        increScore.transform.DORotate(new Vector3(0, 0, 15f), 0.15f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
                increScore.transform.DORotate(Vector3.zero, 0.15f).SetEase(Ease.InOutBack);
            });
    }

    private void OnDestroy()
    {
        DOTween.Kill("TimerShake");
    }

    private void HideContinueButton()
    {
        if (continueButton != null)
        {
            Debug.Log("Masquage du bouton Continue et marquage comme utilisé");
            continueButton.gameObject.SetActive(false);
            hasUsedContinue = true;
            // CORRECTION: Ne pas modifier isNewGame ici car cela empêche le bouton restart de fonctionner
            // isNewGame = false;  -- SUPPRIMÉ
        }
    }

    // Remplacer par une méthode plus sûre qui préserve les références
    private void FixTimerDisplay()
    {
        if (timerText == null) return;
        
        Debug.Log("Correction de l'affichage du timer");
        
        // Sauvegarder la valeur actuelle
        string currentText = timerText.text;
        
        // Appliquer des paramètres selon le type d'appareil
        if (isTabletDevice)
        {
            timerText.fontSize = tabletTimerFontSize;
            timerText.enableAutoSizing = false;
            timerText.rectTransform.localScale = new Vector3(tabletTimerScale, tabletTimerScale, tabletTimerScale);
        }
        else if (isMobileDevice)
        {
            timerText.fontSize = 100; // Taille extrêmement grande
            timerText.enableAutoSizing = false;
            timerText.rectTransform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        }
        
        // Remettre le texte (pour forcer un rafraîchissement)
        timerText.text = currentText;
        
        // Forcer une mise à jour du layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(timerText.rectTransform);
        Canvas.ForceUpdateCanvases();
    }

    private IEnumerator TryShowRewardedAfterDelay()
    {
        // Attendre un court délai pour donner une chance à la pub de se charger
        yield return new WaitForSeconds(1.5f);
        
        if (adMobAdsScript != null)
        {
            bool isAdLoaded = adMobAdsScript.IsRewardedAdLoaded();
            Debug.Log($"Deuxième vérification de la pub récompensée: {(isAdLoaded ? "Chargée" : "Toujours non chargée")}");
            
            if (isAdLoaded)
            {
                adMobAdsScript.ShowRewardedAd();
                HideContinueButton();
            }
            else
            {
                // Informer l'utilisateur que la pub n'est pas disponible
                Debug.LogWarning("La pub récompensée n'a pas pu être chargée");
                
                // Ré-activer le bouton pour permettre un nouvel essai
                if (continueButton != null)
                {
                    continueButton.interactable = true;
                }
            }
        }
    }

    public void OnRewardedAdClosed(bool wasError)
    {
        Debug.Log($"Pub récompensée fermée. Erreur: {wasError}");
        
        // Si c'était une erreur, réactiver le bouton pour permettre une nouvelle tentative
        if (wasError && continueButton != null)
        {
            continueButton.interactable = true;
            Debug.Log("Réactivation du bouton Continue après erreur");
        }
    }
}
