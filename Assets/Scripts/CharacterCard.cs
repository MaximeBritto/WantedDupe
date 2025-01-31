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
        Debug.Log($"Awake de la carte {gameObject.name}");
        
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
        cardButton.onClick.AddListener(() => {
            Debug.Log("Click détecté via listener!");
            OnCardClicked();
        });
        
        cardAnimation = GetComponent<CardAnimation>();
        
        Debug.Log($"Card setup complete - Button: {cardButton != null}, Image: {characterImage != null}");
    }

    public void Initialize(string name, Sprite sprite)
    {
        characterName = name;
        characterSprite = sprite;
        characterImage.sprite = characterSprite;
        Debug.Log($"Carte initialisée: {name} - Est Wanted: {name == "Wanted"}");
    }

    private void OnCardClicked()
    {
        Debug.Log($"Carte cliquée: {characterName}");
        
        if (!GameManager.Instance.isGameActive)
        {
            Debug.Log("Jeu non actif");
            return;
        }

        // Ajout de logs détaillés pour la comparaison
        Debug.Log($"Cette carte: {GetInstanceID()}");
        Debug.Log($"Wanted card: {GameManager.Instance.wantedCharacter?.GetInstanceID()}");
        Debug.Log($"Cette carte est Wanted? {characterName == "Wanted"}");
        Debug.Log($"Sprite actuel: {characterSprite.name}");
        Debug.Log($"Sprite wanted: {GameManager.Instance.wantedCharacter?.characterSprite.name}");

        bool isWanted = (GameManager.Instance.wantedCharacter == this);
        Debug.Log($"Comparaison directe: {isWanted}");

        if (isWanted)
        {
            Debug.Log("C'est la bonne carte!");
            cardAnimation.PlayCorrectAnimation();
            GameManager.Instance.AddScore();
        }
        else
        {
            Debug.Log("Mauvaise carte");
            cardAnimation.PlayWrongAnimation();
            GameManager.Instance.GameOver();
        }
    }

    private void OnMouseDown()
    {
        Debug.Log($"OnMouseDown sur {gameObject.name}");
    }
} 