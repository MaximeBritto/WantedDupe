using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CardAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float spawnDuration = 0.3f;
    [SerializeField] private float correctAnimationDuration = 0.3f;
    [SerializeField] private float wrongAnimationDuration = 0.3f;
    
    [Header("Mobile Settings")]
    [SerializeField] private float mobileAnimationScale = 0.8f;
    
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = transform.localScale;
        originalPosition = rectTransform.anchoredPosition;
    }

    private void Start()
    {
        if (Application.isMobilePlatform)
        {
            // Réduire l'amplitude des animations
            correctAnimationDuration *= 0.8f;
            wrongAnimationDuration *= 0.8f;
            originalScale *= mobileAnimationScale;
        }
    }

    private void OnEnable()
    {
        PlaySpawnAnimation();
    }

    public void PlaySpawnAnimation()
    {
        // Réinitialiser la position et l'échelle
        transform.localScale = Vector3.zero;
        rectTransform.anchoredPosition = originalPosition;

        // Animation d'apparition avec rebond
        transform.DOScale(originalScale, spawnDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    public void PlayCorrectAnimation()
    {
        // Animation quand on trouve le bon Wanted
        transform.DOScale(Vector3.one * 1.2f, correctAnimationDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
                transform.DOScale(Vector3.one, correctAnimationDuration);
            });
    }

    public void PlayWrongAnimation()
    {
        // Créer un effet visuel d'erreur sans affecter le mouvement continu
        
        // Obtenir le composant CharacterCard parent
        CharacterCard card = GetComponent<CharacterCard>();
        if (card != null && card.characterImage != null)
        {
            Color originalColor = card.characterImage.color;
            Sequence errorSeq = DOTween.Sequence();
            
            // Ajouter une légère animation de mise à l'échelle (pulsation) qui n'affecte pas le mouvement
            Vector3 originalScale = transform.localScale;
            Vector3 pulseScale = originalScale * 1.1f; // Légèrement plus grand
            
            errorSeq.Append(transform.DOScale(pulseScale, 0.1f))
                   .Append(transform.DOScale(originalScale, 0.1f))
                   .Append(transform.DOScale(pulseScale, 0.1f))
                   .Append(transform.DOScale(originalScale, 0.1f));
                   
            // Clignotement rouge pour indiquer l'erreur
            errorSeq.Join(card.characterImage.DOColor(new Color(1f, 0.5f, 0.5f, 1f), 0.1f))
                   .Append(card.characterImage.DOColor(originalColor, 0.1f))
                   .Append(card.characterImage.DOColor(new Color(1f, 0.5f, 0.5f, 1f), 0.1f))
                   .Append(card.characterImage.DOColor(originalColor, 0.1f));
                   
            // S'assurer que tout s'exécute sans interférer avec les autres animations
            errorSeq.SetUpdate(true);
        }
    }

    public void PlayShuffleAnimation(Vector2 targetPosition)
    {
        // Animation de mélange des cartes
        rectTransform.DOAnchorPos(targetPosition, wrongAnimationDuration)
            .SetEase(Ease.OutBack);
        
        transform.DORotate(new Vector3(0, 0, Random.Range(-360, 360)), wrongAnimationDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                transform.DORotate(Vector3.zero, 0.2f);
            });
    }

    public void PlayHighlightAnimation()
    {
        // Animation de highlight rapide
        transform.DOScale(Vector3.one * 1.2f, 0.1f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
                transform.DOScale(Vector3.one, 0.1f);
            });
    }

    private void OnDisable()
    {
        // Nettoyer les animations en cours
        DOTween.Kill(transform);
        
        // Réinitialiser la position et l'échelle
        transform.localScale = originalScale;
        rectTransform.anchoredPosition = originalPosition;
    }
} 