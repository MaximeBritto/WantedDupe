using UnityEngine;
using UnityEngine.UI;

public class CharacterCard : MonoBehaviour
{
    [Header("Character Info")]
    public string characterName;
    public Sprite characterSprite;
    
    [Header("References")]
    public Image characterImage;
    public Button cardButton;

    private CardAnimation cardAnimation;

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
            if (characterImage != null)
            {
                // Agrandir légèrement la zone de touch
                characterImage.raycastPadding = new Vector4(10, 10, 10, 10);
            }
        }
    }

    public void Initialize(string name, Sprite sprite)
    {
        characterName = name;
        characterSprite = sprite;
        characterImage.sprite = characterSprite;
    }

    private void OnCardClicked()
    {
        if (!GameManager.Instance.isGameActive || UIManager.Instance.isRouletteRunning) return;

        if (GameManager.Instance.wantedCharacter == this)
        {
            // Cacher toutes les cartes immédiatement
            var gridManager = FindObjectOfType<GridManager>();
            if (gridManager != null)
            {
                foreach (var card in gridManager.cards)
                {
                    card.gameObject.SetActive(false);
                }
            }

            cardAnimation.PlayCorrectAnimation();
            AudioManager.Instance.PlayCorrect();
            GameManager.Instance.AddScore();
        }
        else
        {
            cardAnimation.PlayWrongAnimation();
            AudioManager.Instance.PlayWrong();
            GameManager.Instance.GameOver();
        }
    }

    private void OnMouseDown()
    {
        Debug.Log($"OnMouseDown sur {gameObject.name}");
    }
} 