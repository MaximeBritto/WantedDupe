using UnityEngine;
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
        // Animation quand on se trompe
        transform.DOShakePosition(0.5f, 10, 20, 90, false, true);
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