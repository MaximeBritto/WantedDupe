using UnityEngine;
using DG.Tweening;

public class UIAnimations : MonoBehaviour
{
    [Header("Animation Settings")]
    public float enterDelay = 0.3f;
    public float scaleDuration = 0.5f;

    private void Start()
    {
        // Animation d'entrée
        transform.localScale = Vector3.zero;
        transform.DOScale(1f, scaleDuration)
            .SetDelay(enterDelay)
            .SetEase(Ease.OutBack);
    }

    public void PlayPulseAnimation()
    {
        transform.DOScale(1.1f, 0.2f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
                transform.DOScale(1f, 0.1f);
            });
    }
} 