using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public float playAreaWidth = 1200f;    // Zone de jeu plus large
    public float playAreaHeight = 800f;    // Zone de jeu plus haute
    public int numberOfCards = 24;         // Plus de cartes
    
    [Header("Prefabs")]
    public GameObject characterCardPrefab;
    
    [Header("References")]
    public RectTransform gridContainer;
    
    [Header("Roulette Settings")]
    public float rouletteDuration = 2f;    // Durée de l'effet roulette
    public float highlightDelay = 0.1f;    // Délai entre chaque highlight
    public float delayAfterSuccess = 1f;   // Délai après avoir trouvé le bon wanted

    [Header("Mobile Settings")]
    public float cardSizeMobile = 100f;  // Taille des cartes sur mobile
    public float minSpacingMobile = 10f; // Espacement minimum entre les cartes

    [Header("Card Settings")]
    public int minCards = 16;
    public int maxCards = 24;
    public float minCardDistance = 100f;  // Distance minimale entre les cartes

    public List<CharacterCard> cards = new List<CharacterCard>();
    private CharacterCard wantedCard;

    private void Start()
    {
        GameManager.Instance.onGameStart.AddListener(InitializeGrid);
        
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
    }

    private void AdjustForMobileIfNeeded()
    {
        if (Application.isMobilePlatform)
        {
            // Ajuster le nombre de cartes selon la taille de l'écran
            float screenRatio = (float)Screen.width / Screen.height;
            if (screenRatio < 0.7f)  // Format portrait
            {
                numberOfCards = 16;  // Moins de cartes sur mobile en portrait
            }
            
            // Ajuster la zone de jeu
            playAreaWidth = Screen.width * 0.9f;
            playAreaHeight = Screen.height * 0.6f;
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
        Debug.Log("Début InitializeGrid");
        
        // Nettoyer la grille existante
        foreach (var card in cards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        cards.Clear();
        Debug.Log($"Grid nettoyée, nombre de cartes : {cards.Count}");

        // Créer le wanted card avec un sprite aléatoire
        Sprite wantedSprite = GameManager.Instance.GetRandomSprite();
        List<Sprite> usedSprites = new List<Sprite>();
        usedSprites.Add(wantedSprite);

        // Nombre aléatoire de cartes
        numberOfCards = Random.Range(minCards, maxCards + 1);

        // Créer toutes les cartes avec des positions aléatoires
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
            if (card == null)
            {
                Debug.LogError("Carte null trouvée!");
                continue;
            }
            
            // Activer la carte
            card.gameObject.SetActive(true);
            card.transform.localScale = Vector3.zero;
            
            // Animation d'apparition
            card.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => StartContinuousCardMovement(card));
        }
    }
} 