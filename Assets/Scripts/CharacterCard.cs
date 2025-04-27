using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using UnityEngine.EventSystems;

public class CharacterCard : MonoBehaviour, IPointerDownHandler
{
    [Header("Character Info")]
    public string characterName;
    public Sprite characterSprite;
    
    [Header("References")]
    public Image characterImage;
    public Button cardButton;

    [Header("Device Settings")]
    public float tabletRaycastPadding = 15f;  // Ajustement pour les tablettes

    private CardAnimation cardAnimation;
    private bool alreadyClicked = false;
    private bool isWanted = false;
    private Canvas cardCanvas;
    
    private void Awake()
    {
        // Vérifier si nous avons un Button
        cardButton = GetComponent<Button>();
        if (cardButton == null)
        {
            Debug.LogError($"Pas de composant Button sur {gameObject.name} - Ajout automatique");
            cardButton = gameObject.AddComponent<Button>();
        }
        
        // Vérifier si nous avons une Image
        characterImage = GetComponent<Image>();
        if (characterImage == null)
        {
            Debug.LogError($"Pas de composant Image sur {gameObject.name}");
            return;
        }
        
        // S'assurer que l'image peut recevoir les raycast
        characterImage.raycastTarget = true;
        
        // Configurer le bouton
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(OnCardClicked);
        
        cardAnimation = GetComponent<CardAnimation>();
        
        // Ajuster la taille du collider pour mobile ou tablette
        if (Application.isMobilePlatform)
        {
            // Détecter si c'est une tablette
            bool isTablet = IsTablet();
            
            if (isTablet)
            {
                // Zone de touch plus précise pour tablette
                characterImage.raycastPadding = new Vector4(tabletRaycastPadding, tabletRaycastPadding, tabletRaycastPadding, tabletRaycastPadding);
                Debug.Log($"Raycast padding ajusté pour tablette: {tabletRaycastPadding}");
            }
            else
            {
                // Agrandir la zone de touch pour mobile
                characterImage.raycastPadding = new Vector4(10, 10, 10, 10);
            }
        }

        // IMPORTANT: Ne pas créer de Canvas individuel
        // Supprimer tout Canvas existant pour éviter les problèmes
        Canvas existingCanvas = GetComponent<Canvas>();
        if (existingCanvas != null)
        {
            Debug.Log($"Suppression du Canvas sur {gameObject.name} pour éviter les problèmes avec RectMask2D");
            Destroy(existingCanvas);
            
            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                Destroy(raycaster);
        }
        
        // Assurer que l'objet est actif et visible
        gameObject.SetActive(true);
        characterImage.enabled = true;
    }
    
    // Méthode pour détecter les tablettes
    private bool IsTablet()
    {
        // Vérifier si c'est un appareil mobile d'abord
        if (!Application.isMobilePlatform)
            return false;
            
        // Résolution minimum d'une tablette
        float minTabletDiagonal = 1500f;
        
        // Calculer la diagonale en pixels
        float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
        
        return screenDiagonal >= minTabletDiagonal;
    }

    public void Initialize(string name, Sprite sprite)
    {
        characterName = name;
        characterSprite = sprite;
        characterImage.sprite = characterSprite;
    }

    // Méthode pour définir si cette carte est la carte recherchée
    public void SetAsWanted(bool wanted)
    {
        isWanted = wanted;
        
        // Si c'est la carte recherchée, ajuster l'ordre d'affichage
        // NOTE: Nous n'utilisons plus Canvas.sortingOrder mais la propriété siblingIndex pour l'ordre
        if (isWanted)
        {
            // Mettre la carte wanted "sous" les autres cartes (indice plus petit)
            transform.SetSiblingIndex(0);
        }
        else
        {
            // Mettre les autres cartes "au-dessus" (indice plus grand)
            transform.SetSiblingIndex(transform.parent.childCount - 1);
        }
    }

    // Ajuster l'ordre d'affichage de la carte - Méthode conservée pour compatibilité
    public void SetCardRenderOrder(int orderValue)
    {
        // Si orderValue est plus petit, la carte doit être "sous" les autres (siblingIndex plus petit)
        // Si orderValue est plus grand, la carte doit être "au-dessus" (siblingIndex plus grand)
        if (orderValue <= 50)
        {
            transform.SetSiblingIndex(0); // Mettre au bas de la pile
        }
        else
        {
            transform.SetSiblingIndex(transform.parent.childCount - 1); // Mettre au sommet de la pile
        }
    }

    // Utilisé pour vérifier si la carte est au moins partiellement visible et cliquable
    public bool IsPartiallyVisible()
    {
        // Collecter toutes les cartes qui pourraient chevaucher celle-ci
        CharacterCard[] allCards = FindObjectsOfType<CharacterCard>();
        RectTransform myRect = GetComponent<RectTransform>();
        Rect myBounds = new Rect(myRect.position.x - myRect.rect.width/2, 
                                myRect.position.y - myRect.rect.height/2, 
                                myRect.rect.width, myRect.rect.height);
        
        // Surface totale et zone couverte
        float myArea = myRect.rect.width * myRect.rect.height;
        float coveredArea = 0f;
        
        foreach (CharacterCard otherCard in allCards)
        {
            if (otherCard == this) continue;
            
            RectTransform otherRect = otherCard.GetComponent<RectTransform>();
            Rect otherBounds = new Rect(otherRect.position.x - otherRect.rect.width/2, 
                                      otherRect.position.y - otherRect.rect.height/2, 
                                      otherRect.rect.width, otherRect.rect.height);
            
            // Calculer l'intersection
            Rect intersection = new Rect();
            if (myBounds.Overlaps(otherBounds))
            {
                float xMin = Mathf.Max(myBounds.x, otherBounds.x);
                float yMin = Mathf.Max(myBounds.y, otherBounds.y);
                float xMax = Mathf.Min(myBounds.x + myBounds.width, otherBounds.x + otherBounds.width);
                float yMax = Mathf.Min(myBounds.y + myBounds.height, otherBounds.y + otherBounds.height);
                
                intersection.x = xMin;
                intersection.y = yMin;
                intersection.width = xMax - xMin;
                intersection.height = yMax - yMin;
                
                coveredArea += intersection.width * intersection.height;
            }
        }
        
        // Si plus de 90% est couvert, considérer comme non visible
        return (coveredArea / myArea) < 0.9f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (alreadyClicked) return; // On ignore les clics ultérieurs

        if (!GameManager.Instance.isGameActive || UIManager.Instance.isRouletteRunning)
            return;

        // IMPORTANT: Afficher des logs de débogage pour diagnostiquer le problème
        Debug.Log($"OnPointerDown sur {characterName} - isWanted={isWanted}");
        Debug.Log($"GameManager.wantedCharacter={GameManager.Instance.wantedCharacter?.characterName}, this={gameObject.name}");
        
        // CORRECTION: utiliser isWanted au lieu de comparer les références
        // Cela permet d'éviter les problèmes de références différentes pour le même objet
        if (isWanted && GameManager.Instance.wantedCharacter == this)
        {
            Debug.Log("☑️ CARTE CORRECTE DÉTECTÉE!");
            // Logique de réussite - Marquer cette carte spécifique comme cliquée
            alreadyClicked = true;
            AudioManager.Instance.PlayCorrect();
            StartCoroutine(HandleCorrectClick());
        }
        else
        {
            // Logique d'erreur
            // Ne pas marquer la carte comme cliquée pour permettre d'autres essais
            Debug.Log("❌ CARTE INCORRECTE: attendu=" + GameManager.Instance.wantedCharacter?.characterName);
            cardAnimation.PlayWrongAnimation();
            AudioManager.Instance.PlayWrong();
            GameManager.Instance.ApplyTimePenalty();
            // Ne PAS arrêter le mouvement de la carte
        }
    }

    private void OnCardClicked()
    {
        if (alreadyClicked) return; // On ignore les clics ultérieurs

        if (!GameManager.Instance.isGameActive || UIManager.Instance.isRouletteRunning)
            return;
            
        // IMPORTANT: Afficher des logs de débogage pour diagnostiquer le problème
        Debug.Log($"OnCardClicked sur {characterName} - isWanted={isWanted}");
        Debug.Log($"GameManager.wantedCharacter={GameManager.Instance.wantedCharacter?.characterName}, this={gameObject.name}");

        // CORRECTION: utiliser isWanted au lieu de comparer les références
        // Cela permet d'éviter les problèmes de références différentes pour le même objet
        if (isWanted && GameManager.Instance.wantedCharacter == this)
        {
            Debug.Log("☑️ CARTE CORRECTE DÉTECTÉE!");
            // Logique de réussite - Marquer cette carte spécifique comme cliquée
            alreadyClicked = true;
            AudioManager.Instance.PlayCorrect();
            StartCoroutine(HandleCorrectClick());
        }
        else
        {
            // Logique d'erreur
            // Ne pas marquer la carte comme cliquée pour permettre d'autres essais
            Debug.Log("❌ CARTE INCORRECTE: attendu=" + GameManager.Instance.wantedCharacter?.characterName);
            cardAnimation.PlayWrongAnimation();
            AudioManager.Instance.PlayWrong();
            GameManager.Instance.ApplyTimePenalty();
            // Ne PAS arrêter le mouvement de la carte
        }
    }

    private IEnumerator HandleCorrectClick()
    {
        // Arrêter immédiatement le timer
        GameManager.Instance.PauseGame();
        
        // Stocker la position actuelle et l'échelle de la carte
        Vector3 originalPosition = transform.position;
        Vector3 originalLocalPosition = transform.localPosition;
        Vector3 originalAnchoredPosition = GetComponent<RectTransform>().anchoredPosition;
        
        // Stopper toute animation en cours sur cette carte
        DOTween.Kill(transform);
        
        // Garder cette carte visible mais faire disparaître toutes les autres
        var gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            // Pause le GridManager pour éviter tout repositionnement pendant notre animation
            gridManager.StopAllCardMovements();
            
            foreach (var card in gridManager.cards)
            {
                if (card != null && card != this)
                {
                    // Arrêter toute animation sur les autres cartes
                    DOTween.Kill(card.transform);
                    card.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
                }
            }
            
            // Mettre en évidence la bonne carte trouvée
            transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack)
                .OnComplete(() => {
                    transform.DOScale(1f, 0.2f).SetEase(Ease.InOutBack);
                });
        }
        
        // S'assurer que la carte reste fixe à sa position actuelle
        for (float t = 0; t < 0.5f; t += 0.05f)
        {
            // Forcer la position à rester la même
            transform.position = originalPosition;
            transform.localPosition = originalLocalPosition;
            GetComponent<RectTransform>().anchoredPosition = originalAnchoredPosition;
            
            // Attendre une courte période
            yield return new WaitForSeconds(0.05f);
        }
        
        // IMPORTANT: Figer définitivement la position en désactivant tout
        // mouvement possible venant d'autres scripts
        // Cela empêchera complètement la carte de bouger pendant la transition
        RectTransform rt = GetComponent<RectTransform>();
        var layoutElement = GetComponent<UnityEngine.UI.LayoutElement>();
        if (layoutElement != null)
            layoutElement.ignoreLayout = true;
        
        // Animation de la carte vers l'étoile correspondante
        if (UIManager.Instance != null && UIManager.Instance.comboImages != null)
        {
            // Calculer quelle étoile doit être activée (basé sur le score modulo 5)
            int currentScore = (int)GameManager.Instance.displayedScore;
            int targetStarIndex = currentScore % 5;
            
            // Si targetStarIndex est 0 et le score n'est pas 0, alors c'est un multiple de 5
            if (targetStarIndex == 0 && currentScore > 0)
            {
                targetStarIndex = 4; // Utiliser la 5ème étoile (index 4)
            }
            else if (currentScore > 0)
            {
                // Sinon, utiliser l'index correspondant à la prochaine étoile
                targetStarIndex = targetStarIndex - 1;
            }
            
            // S'assurer que l'index est valide
            if (targetStarIndex >= 0 && targetStarIndex < UIManager.Instance.comboImages.Length)
            {
                // Obtenir la position de l'étoile cible
                Vector3 starPosition = UIManager.Instance.comboImages[targetStarIndex].transform.position;
                
                Debug.Log($"Animation de la carte vers l'étoile {targetStarIndex+1}");
                
                // Créer une séquence d'animation pour déplacer la carte vers l'étoile
                Sequence cardToStarSequence = DOTween.Sequence();
                
                // Déplacer la carte vers l'étoile avec un effet de rebond
                cardToStarSequence
                    .Append(transform.DOMove(starPosition, 0.5f).SetEase(Ease.InOutQuad))
                    .Join(transform.DOScale(0.5f, 0.5f).SetEase(Ease.InOutQuad))
                    .Join(transform.DORotate(new Vector3(0, 0, 360), 0.5f, RotateMode.FastBeyond360).SetEase(Ease.InOutQuad))
                    .OnComplete(() => {
                        // Faire disparaître la carte à l'arrivée
                        transform.DOScale(0f, 0.2f).SetEase(Ease.InBack);
                        
                        // Jouer une animation sur l'étoile
                        UIManager.Instance.comboImages[targetStarIndex].transform.DOScale(1.5f, 0.2f).SetEase(Ease.OutBack)
                            .OnComplete(() => {
                                UIManager.Instance.comboImages[targetStarIndex].transform.DOScale(1f, 0.1f).SetEase(Ease.InOutBack);
                            });
                    });
                
                // Attendre que l'animation se termine
                yield return new WaitForSeconds(0.8f);
            }
            else
            {
                // Fallback si l'index est invalide
                Debug.LogError($"Index d'étoile invalide: {targetStarIndex}");
                transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
                yield return new WaitForSeconds(0.3f);
            }
        }
        else
        {
            // Fallback si les étoiles ne sont pas disponibles
            transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
            yield return new WaitForSeconds(0.3f);
        }
        
        // Ensuite ajouter le score et déclencher la séquence suivante
        GameManager.Instance.AddScore();
    }

    private void OnMouseDown()
    {
        Debug.Log($"OnMouseDown sur {gameObject.name}");
    }
} 