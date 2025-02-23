using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ComboSlider : MonoBehaviour
{
    public Slider comboSlider;
    public float fillSpeed = 2f;  // Vitesse de remplissage du slider
    
    [Header("Animation Settings")]
    public float pulseSpeed = 1f;
    public float pulseScale = 1.1f;
    public float glowIntensity = 1.2f;
    
    [Header("References")]
    public Image fillImage;        // L'image de remplissage du slider
    public Image handleImage;      // Le "handle" du slider (optionnel)
    public Image backgroundImage;  // L'image de fond du slider
    
    [Header("Reset Animation")]
    public float resetDuration = 0.5f;  // Durée de l'animation de reset
    public float scoreIncrementDelay = 0.1f;  // Délai entre chaque incrément de score
    
    private float targetValue = 0f;
    private Sequence pulseSequence;
    private bool isResetting = false;
    
    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onComboChanged.AddListener(UpdateComboSlider);
        }
        
        // Vérifier et obtenir les références si nécessaire
        if (comboSlider == null)
        {
            comboSlider = GetComponent<Slider>();
            Debug.LogWarning("ComboSlider: slider non assigné, tentative de le trouver automatiquement");
        }

        if (fillImage == null && comboSlider != null)
        {
            fillImage = comboSlider.fillRect?.GetComponent<Image>();
            Debug.LogWarning("ComboSlider: fillImage non assigné, tentative de le trouver automatiquement");
        }

        if (backgroundImage == null && comboSlider != null)
        {
            backgroundImage = comboSlider.GetComponentInChildren<Image>();
        }

        // Initialiser le slider
        if (comboSlider != null)
        {
            comboSlider.value = 0;
            comboSlider.maxValue = 1;
        }

        // Démarrer l'animation d'idle seulement si on a l'image de remplissage
        if (fillImage != null)
        {
            StartIdleAnimation();
        }
        else
        {
            Debug.LogWarning("ComboSlider: fillImage non trouvé, l'animation d'idle ne sera pas jouée");
        }
    }
    
    private void StartIdleAnimation()
    {
        // Vérification de sécurité
        if (fillImage == null) return;

        // Arrêter l'animation précédente si elle existe
        if (pulseSequence != null)
        {
            pulseSequence.Kill();
        }

        // Créer une nouvelle séquence d'animation
        pulseSequence = DOTween.Sequence()
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);

        // Animation de pulsation
        pulseSequence.Append(fillImage.transform.DOScale(pulseScale, pulseSpeed)
            .SetEase(Ease.InOutSine));
        
        // Animation de brillance
        Color baseColor = fillImage.color;
        Color glowColor = new Color(
            baseColor.r * glowIntensity,
            baseColor.g * glowIntensity,
            baseColor.b * glowIntensity,
            baseColor.a
        );
        
        pulseSequence.Join(fillImage.DOColor(glowColor, pulseSpeed)
            .SetEase(Ease.InOutSine));
    }
    
    private void Update()
    {
        // Animation fluide du slider
        if (comboSlider.value != targetValue)
        {
            comboSlider.value = Mathf.Lerp(comboSlider.value, targetValue, Time.deltaTime * fillSpeed);
        }
    }
    
    public void UpdateComboSlider(float comboProgress)
    {
        if (isResetting) return;

        targetValue = comboProgress;

        // Si on atteint 5 (1.0f), démarrer l'animation de reset
        if (comboProgress >= 1.0f)
        {
            StartResetAnimation();
        }
        // Sinon, mise à jour normale
        else if (comboProgress == 0f)
        {
            comboSlider.value = 0f;
        }
    }

    private void StartResetAnimation()
    {
        isResetting = true;
        
        // Créer une séquence pour l'animation
        Sequence resetSequence = DOTween.Sequence();

        // D'abord, s'assurer que le slider atteint bien 1 (5 points)
        resetSequence.Append(DOTween.To(() => comboSlider.value, x => comboSlider.value = x, 1f, 0.2f));

        // Petit délai pour voir le slider plein
        resetSequence.AppendInterval(0.2f);

        // Vider le slider progressivement
        resetSequence.Append(DOTween.To(() => comboSlider.value, x => comboSlider.value = x, 0f, resetDuration)
            .SetEase(Ease.InOutQuad));

        // Incrémenter le score pendant que le slider se vide
        float scoreIncrement = 0;
        resetSequence.OnUpdate(() => 
        {
            if (!isResetting) return;  // Sécurité supplémentaire
            
            float progress = 1f - (comboSlider.value);  // 0 à 1
            float targetScore = Mathf.Floor(progress * 5f);  // 0 à 5
            
            if (targetScore > scoreIncrement)
            {
                scoreIncrement = targetScore;
                GameManager.Instance?.IncrementDisplayedScore();
            }
        });

        // Réinitialiser à la fin
        resetSequence.OnComplete(() => 
        {
            GameManager.Instance?.ResetCombo();  // Important : réinitialiser le combo après l'animation
            isResetting = false;
            targetValue = 0f;
        });
    }

    private void OnDestroy()
    {
        if (pulseSequence != null)
        {
            pulseSequence.Kill();
        }
    }
} 