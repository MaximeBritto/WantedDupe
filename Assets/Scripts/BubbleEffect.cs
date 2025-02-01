using UnityEngine;
using DG.Tweening;

public class BubbleEffect : MonoBehaviour
{
    public float bounceScale = 1.1f;
    public float bounceDuration = 0.2f;

    private void Start()
    {
        // Effet de pulsation douce continue
        transform.DOScale(bounceScale, bounceDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
} 