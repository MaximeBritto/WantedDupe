using UnityEngine;
using UnityEngine.UI;

public class ComboSlider : MonoBehaviour
{
    public Slider comboSlider;
    public float fillSpeed = 2f;  // Vitesse de remplissage du slider
    
    private float targetValue = 0f;
    
    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onComboChanged.AddListener(UpdateComboSlider);
        }
        
        // Initialiser le slider
        comboSlider.value = 0;
        comboSlider.maxValue = 1;
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
        targetValue = comboProgress;
        // Forcer la mise à jour immédiate si c'est une réinitialisation
        if (comboProgress == 0f)
        {
            comboSlider.value = 0f;
        }
    }
} 