using UnityEngine;
using DG.Tweening;

public class CardAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float spawnDuration = 0.3f;
    [SerializeField] private float correctAnimationDuration = 0.3f;
    [SerializeField] private float wrongAnimationDuration = 0.3f;
    
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = transform.localScale;
        originalPosition = rectTransform.anchoredPosition;
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
        // Annuler les animations en cours
        DOTween.Kill(transform);
        
        // Animation de pulsation pour la bonne réponse
        transform.DOScale(originalScale * 1.2f, correctAnimationDuration / 2)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                transform.DOScale(originalScale, correctAnimationDuration / 2)
                    .SetEase(Ease.InQuad);
            })
            .SetUpdate(true);
    }

    public void PlayWrongAnimation()
    {
        // Annuler les animations en cours
        DOTween.Kill(transform);
        
        // Animation de secousse pour la mauvaise réponse
        rectTransform.DOShakePosition(wrongAnimationDuration, 10f, 10, 90f)
            .SetUpdate(true)
            .OnComplete(() => {
                // Remettre à la position d'origine
                rectTransform.anchoredPosition = originalPosition;
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