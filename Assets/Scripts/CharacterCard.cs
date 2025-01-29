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
        if (cardButton == null) cardButton = GetComponent<Button>();
        if (characterImage == null) characterImage = GetComponent<Image>();
        
        cardButton.onClick.AddListener(OnCardClicked);
        cardAnimation = GetComponent<CardAnimation>();
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
            cardAnimation.PlayCorrectAnimation();
            GameManager.Instance.AddScore();
        }
        else
        {
            cardAnimation.PlayWrongAnimation();
            GameManager.Instance.GameOver();
        }
    }
} 