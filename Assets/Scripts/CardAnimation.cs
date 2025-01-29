using UnityEngine;
using DG.Tween; // Nous utiliserons DOTween pour les animations

public class CardAnimation : MonoBehaviour
{
    private RectTransform rectTransform;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void PlaySpawnAnimation()
    {
        // Animation d'apparition
        transform.localScale = Vector3.zero;
        transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    public void PlayCorrectAnimation()
    {
        // Animation de bonne réponse
        transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 1, 0.5f);
    }

    public void PlayWrongAnimation()
    {
        // Animation de mauvaise réponse
        transform.DOShakePosition(0.3f, 10f, 10, 90f);
    }
} 