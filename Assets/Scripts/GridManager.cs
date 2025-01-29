using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int rows = 4;
    public int columns = 4;
    public float spacing = 10f;
    
    [Header("Prefabs")]
    public GameObject characterCardPrefab;
    
    [Header("References")]
    public RectTransform gridContainer;
    
    private List<CharacterCard> cards = new List<CharacterCard>();

    private void Start()
    {
        GameManager.Instance.onGameStart.AddListener(InitializeGrid);
    }

    public void InitializeGrid()
    {
        // Nettoyer la grille existante
        foreach (var card in cards)
        {
            Destroy(card.gameObject);
        }
        cards.Clear();

        // Créer la nouvelle grille
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                GameObject cardObj = Instantiate(characterCardPrefab, gridContainer);
                CharacterCard card = cardObj.GetComponent<CharacterCard>();
                
                // Positionner la carte
                RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
                float xPos = j * (rectTransform.rect.width + spacing);
                float yPos = -i * (rectTransform.rect.height + spacing);
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);

                // Initialiser la carte avec un sprite aléatoire
                Sprite randomSprite = GameManager.Instance.GetRandomSprite();
                card.Initialize($"Character_{i}_{j}", randomSprite);
                
                cards.Add(card);
            }
        }

        // Sélectionner un personnage recherché au hasard
        int randomIndex = Random.Range(0, cards.Count);
        GameManager.Instance.SelectNewWantedCharacter(cards[randomIndex]);
    }
} 