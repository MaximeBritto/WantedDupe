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

        isMobileDevice = Application.isMobilePlatform;
        if (isMobileDevice)
        {
            ConfigureForPortrait();
        }

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
            continueButton.onClick.AddListener(() => {
                if (adMobAdsScript != null)
                {
                    adMobAdsScript.LoadRewardedAd();
                    adMobAdsScript.ShowRewardedAd();
                    HideContinueButton();  // Masquer le bouton après utilisation
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
        }
    }

    private void UpdateUI()
    {
        if (GameManager.Instance == null) return;
        
        scoreText.text = $"{GameManager.Instance.displayedScore}";
        float timeRemaining = GameManager.Instance.timeRemaining;
        timerText.text = $"{Mathf.CeilToInt(timeRemaining)}";
        
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
            if (!DOTween.IsTweening(timerText.transform) || DOTween.IsTweening("TimerShake"))
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

        // Afficher le sprite final du wanted
        wantedCharacterImage.sprite = finalCharacter.characterSprite;
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
            Debug.Log("UIManager: Préparation des cartes après roulette");
            
            // VÉRIFICATION SUPPLÉMENTAIRE: S'assurer que le parent des cartes est actif
            Transform parentTransform = gridManager.gameBoardTransform != null ? 
                gridManager.gameBoardTransform : gridManager.transform;
            if (!parentTransform.gameObject.activeSelf)
            {
                Debug.LogWarning("CORRECTION: Le parent des cartes était désactivé - Réactivation");
                parentTransform.gameObject.SetActive(true);
            }
            
            // Identifier le pattern actuel
            string patternType = gridManager.CurrentState.ToString();
            Debug.Log($"UIManager: Pattern détecté: {patternType}");
            
            // Préparer toutes les cartes à être à échelle zéro AVANT le placement
            foreach (var card in gridManager.cards)
            {
                if (card != null)
                {
                    card.gameObject.SetActive(true);
                    card.transform.localScale = Vector3.zero;
                    
                    // Stopper toute animation en cours sur cette carte
                    DOTween.Kill(card.transform);
                }
            }
            
            // UN SEUL APPEL à ArrangeCardsBasedOnState pour éviter les doubles placements
            // Cela positionne initialement les cartes au bon endroit
            gridManager.ArrangeCardsBasedOnState();
            
            // Log pour déboguer le nombre de cartes
            Debug.Log($"Nombre de cartes à animer: {gridManager.cards.Count}");
            
            // Attendre un court délai avant d'animer l'entrée des cartes
            yield return new WaitForSeconds(0.4f);
            
            // Animer l'entrée des cartes maintenant que tout est correctement positionné
            Debug.Log("UIManager: Animation des cartes après positionnement");
            gridManager.AnimateCardsEntry();
            
            // Ajouter une solution de secours pour s'assurer que les cartes sont visibles
            StartCoroutine(ForceShowCardsBackup(gridManager, 0.5f));
        }
        else
        {
            Debug.LogError("UIManager: GridManager introuvable après la roulette!");
        }
        
        GameManager.Instance.ResumeGame();
        isRouletteRunning = false;
        Debug.Log("UIManager: Fin de la roulette UI");
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

        if (SafeArea != null)
        {
            SafeArea.SetActive(true);
            Board.SetActive(true);
        }
        if (uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(true);
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

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(isNewGame);
        }
    }

    private void StartGame()
    {
        isNewGame = true;  // Réinitialiser le flag de nouvelle partie
        hasUsedContinue = false;  // Réinitialiser le flag d'utilisation du continue
        
        // Mettre à jour l'affichage du meilleur score
        if (bestScoreText != null && GameManager.Instance != null)
        {
            bestScoreText.text = $"Meilleur Score : {GameManager.Instance.bestScore}";
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

    private void ConfigureForPortrait()
    {
        finalWantedSize = mobileWantedSize;
        finalWantedPosition = mobileWantedPosition;
        if (scoreText != null) scoreText.fontSize = 40;
        if (timerText != null) timerText.fontSize = 40;
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
            continueButton.gameObject.SetActive(false);
            hasUsedContinue = true;
            isNewGame = false;  // Indiquer que ce n'est plus une nouvelle partie
        }
    }
}
