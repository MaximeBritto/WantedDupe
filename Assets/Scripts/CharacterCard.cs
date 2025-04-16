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

    private CardAnimation cardAnimation;
    private bool alreadyClicked = false;

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
        
        // Ajuster la taille du collider pour mobile
        if (Application.isMobilePlatform)
        {
            // Agrandir la zone de touch
            characterImage.raycastPadding = new Vector4(10, 10, 10, 10);
        }
    }

    public void Initialize(string name, Sprite sprite)
    {
        characterName = name;
        characterSprite = sprite;
        characterImage.sprite = characterSprite;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (alreadyClicked) return; // On ignore les clics ultérieurs

        if (!GameManager.Instance.isGameActive || UIManager.Instance.isRouletteRunning)
            return;

        if (GameManager.Instance.wantedCharacter == this)
        {
            // Logique de réussite - Marquer cette carte spécifique comme cliquée
            alreadyClicked = true;
            AudioManager.Instance.PlayCorrect();
            StartCoroutine(HandleCorrectClick());
        }
        else
        {
            // Logique d'erreur
            // Ne pas marquer la carte comme cliquée pour permettre d'autres essais
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

        if (GameManager.Instance.wantedCharacter == this)
        {
            // Logique de réussite - Marquer cette carte spécifique comme cliquée
            alreadyClicked = true;
            AudioManager.Instance.PlayCorrect();
            StartCoroutine(HandleCorrectClick());
        }
        else
        {
            // Logique d'erreur
            // Ne pas marquer la carte comme cliquée pour permettre d'autres essais
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
        for (float t = 0; t < 1f; t += 0.05f)
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
        
        // Masquer la carte juste avant d'ajouter le score pour éviter
        // tout effet de déplacement visible
        DOTween.Sequence()
            .AppendInterval(0.9f)  // Attendre presque jusqu'à la fin
            .Append(transform.DOScale(0f, 0.1f))  // Disparaître rapidement
            .SetEase(Ease.InBack);
        
        // Attendre 1 seconde totale avant d'ajouter le score
        yield return new WaitForSeconds(1f);
        
        // Ensuite ajouter le score et déclencher la séquence suivante
        GameManager.Instance.AddScore();
    }

    private void OnMouseDown()
    {
        Debug.Log($"OnMouseDown sur {gameObject.name}");
    }
} 