using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public float playAreaWidth = 800f;
    public float playAreaHeight = 600f;
    public int numberOfCards = 16;
    
    [Header("Prefabs")]
    public GameObject characterCardPrefab;
    
    [Header("References")]
    public RectTransform gridContainer;
    
    private List<CharacterCard> cards = new List<CharacterCard>();
    private CharacterCard wantedCard;

    private void Start()
    {
        GameManager.Instance.onGameStart.AddListener(InitializeGrid);
    }

    public void InitializeGrid()
    {
        // Nettoyer la grille existante
        foreach (var card in cards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        cards.Clear();

        // Créer le wanted card avec un sprite aléatoire
        Sprite wantedSprite = GameManager.Instance.GetRandomSprite();
        List<Sprite> usedSprites = new List<Sprite>();
        usedSprites.Add(wantedSprite);  // Pour éviter les doublons

        Debug.Log($"Sprite Wanted choisi");

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

            Sprite cardSprite;
            // La première carte est toujours le wanted
            if (i == 0)
            {
                cardSprite = wantedSprite;
                wantedCard = card;
                Debug.Log("Carte Wanted créée");
            }
            else
            {
                // Pour les autres cartes, choisir un sprite différent
                do
                {
                    cardSprite = GameManager.Instance.GetRandomSprite();
                } while (cardSprite == wantedSprite);
                usedSprites.Add(cardSprite);
            }

            card.Initialize(i == 0 ? "Wanted" : $"Card_{i}", cardSprite);
            cards.Add(card);
        }

        // Mélanger l'ordre des cartes dans la hiérarchie
        ShuffleCardsOrder();

        // Important : définir la carte wanted dans le GameManager
        if (wantedCard != null)
        {
            Debug.Log($"Setting Wanted Card in GameManager - ID: {wantedCard.GetInstanceID()}");
            Debug.Log($"Wanted Card Name: {wantedCard.characterName}");
            Debug.Log($"Wanted Card Sprite: {wantedCard.characterSprite.name}");
            GameManager.Instance.SelectNewWantedCharacter(wantedCard);
        }
        else
        {
            Debug.LogError("Wanted Card is null!");
        }
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
        // Choisir un nouveau sprite wanted
        Sprite newWantedSprite = GameManager.Instance.GetRandomSprite();
        
        // Mélanger les positions des cartes
        foreach (var card in cards)
        {
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            float xPos = Random.Range(-playAreaWidth/2, playAreaWidth/2);
            float yPos = Random.Range(-playAreaHeight/2, playAreaHeight/2);
            rectTransform.anchoredPosition = new Vector2(xPos, yPos);
        }

        // Choisir une carte au hasard pour être le nouveau wanted
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

        // Mélanger l'ordre des cartes
        ShuffleCardsOrder();

        // Mettre à jour le GameManager
        GameManager.Instance.SelectNewWantedCharacter(wantedCard);
    }
} 