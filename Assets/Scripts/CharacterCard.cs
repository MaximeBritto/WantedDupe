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

    private void Awake()
    {
        if (cardButton == null) cardButton = GetComponent<Button>();
        if (characterImage == null) characterImage = GetComponent<Image>();
        
        cardButton.onClick.AddListener(OnCardClicked);
    }

    public void Initialize(string name, Sprite sprite)
    {
        characterName = name;
        characterSprite = sprite;
        characterImage.sprite = characterSprite;
    }

    private void OnCardClicked()
    {
        if (!GameManager.Instance.isGameActive) return;

        if (GameManager.Instance.wantedCharacter == this)
        {
            GameManager.Instance.AddScore();
            // Déclencher l'animation de succès ici
        }
        else
        {
            GameManager.Instance.GameOver();
            // Déclencher l'animation d'échec ici
        }
    }
} 