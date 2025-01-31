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

    public void InitializeGrid()
    {
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

        // Créer toutes les cartes avec des positions aléatoires
        for (int i = 0; i < numberOfCards; i++)
        {
            GameObject cardObj = Instantiate(characterCardPrefab, gridContainer);
            CharacterCard card = cardObj.GetComponent<CharacterCard>();
            
            // Position aléatoire dans la zone de jeu
            RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
            float xPos = Random.Range(-playAreaWidth/2, playAreaWidth/2);
            float yPos = Random.Range(-playAreaHeight/2, playAreaHeight/2);
            rectTransform.anchoredPosition = new Vector2(xPos, yPos);
            
            Debug.Log($"Carte {i} créée à la position : {xPos}, {yPos}");

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
            
            // Assigner une nouvelle position aléatoire
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            Vector2 randomPos = new Vector2(
                Random.Range(-playAreaWidth/2, playAreaWidth/2),
                Random.Range(-playAreaHeight/2, playAreaHeight/2)
            );
            rectTransform.anchoredPosition = randomPos;
            
            Debug.Log($"Position de la carte {card.characterName}: {rectTransform.anchoredPosition}");
        }
        
        StartCoroutine(AnimateCardsEntryCoroutine());
    }

    private IEnumerator AnimateCardsEntryCoroutine()
    {
        float delayBetweenCards = 0.1f;
        
        foreach (var card in cards)
        {
            // Animer l'apparition avec un effet de rebond
            card.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack);
            
            // Animer la position avec un effet de rebond
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            Vector2 currentPos = rectTransform.anchoredPosition;
            Vector2 targetPos = new Vector2(
                Random.Range(-playAreaWidth/2, playAreaWidth/2),
                Random.Range(-playAreaHeight/2, playAreaHeight/2)
            );
            
            rectTransform.DOAnchorPos(targetPos, 0.3f)
                .SetEase(Ease.OutBack);
            
            yield return new WaitForSeconds(delayBetweenCards);
        }
    }
} 