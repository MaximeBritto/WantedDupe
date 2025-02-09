using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class BackgroundManager : MonoBehaviour
{
    [Header("Configuration")]
    public RectTransform backgroundContainer;
    public GameObject backgroundTilePrefab;
    public float scrollSpeed = 100f;  // Plus la valeur est petite, plus le défilement est rapide
    public int rows = 5;
    public int tilesPerRow = 8;
    public float tileSize = 100f;
    public float spacing = 10f;

    [Header("Apparence")]
    [Range(0f, 1f)]
    public float spriteOpacity = 0.2f;  // Contrôle l'opacité des sprites

    private List<RectTransform> backgroundTiles = new List<RectTransform>();
    private float totalWidth;
    private float tileWidth;
    private float currentOffset = 0f;

    private void Start()
    {
        CreateBackgroundTiles();
        StartScrolling();
    }

    private void CreateBackgroundTiles()
    {
        tileWidth = tileSize + spacing;
        // On ajoute une colonne supplémentaire pour éviter les gaps
        totalWidth = tileWidth * (tilesPerRow + 1);

        // Créer la grille de tuiles
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < tilesPerRow + 1; col++)
            {
                GameObject tile = Instantiate(backgroundTilePrefab, backgroundContainer);
                RectTransform rectTransform = tile.GetComponent<RectTransform>();
                Image image = tile.GetComponent<Image>();

                // Positionner la tuile
                float xPos = col * tileWidth;
                float yPos = (row - rows/2) * tileWidth;
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                rectTransform.sizeDelta = new Vector2(tileSize, tileSize);

                // Assigner une image aléatoire
                if (GameManager.Instance != null)
                {
                    image.sprite = GameManager.Instance.GetRandomSprite();
                    image.color = new Color(1, 1, 1, spriteOpacity);
                }

                backgroundTiles.Add(rectTransform);
            }
        }
    }

    private void StartScrolling()
    {
        // Démarrer le défilement continu
        DOTween.To(() => currentOffset, x => {
            currentOffset = x;
            UpdateTilesPosition();
        }, totalWidth, scrollSpeed)
        .SetEase(Ease.Linear)
        .SetLoops(-1);
    }

    private void UpdateTilesPosition()
    {
        foreach (var tile in backgroundTiles)
        {
            // Calculer la nouvelle position
            float newX = tile.anchoredPosition.x - (Time.deltaTime * 60f / scrollSpeed);
            
            // Si la tuile sort complètement à gauche
            if (newX <= -tileWidth)
            {
                // La replacer à droite
                newX += totalWidth;
            }
            
            // Mettre à jour la position
            tile.anchoredPosition = new Vector2(newX, tile.anchoredPosition.y);
        }
    }

    // Méthode pour mettre à jour l'opacité en temps réel si nécessaire
    public void UpdateOpacity(float newOpacity)
    {
        spriteOpacity = Mathf.Clamp01(newOpacity);
        foreach (var tile in backgroundTiles)
        {
            Image image = tile.GetComponent<Image>();
            if (image != null)
            {
                Color color = image.color;
                color.a = spriteOpacity;
                image.color = color;
            }
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(transform);
    }
} 