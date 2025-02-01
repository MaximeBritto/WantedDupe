using UnityEngine;

public class WantedPanelPosition : MonoBehaviour
{
    [Header("Position Settings")]
    [Range(0f, 0.5f)] public float topOffset = 0.1f;    // % de l'écran depuis le haut
    [Range(0f, 1f)] public float heightRatio = 0.25f;   // % de l'écran pour la hauteur
    [Range(0f, 1f)] public float widthRatio = 0.8f;     // % de l'écran pour la largeur

    private RectTransform rectTransform;
    private Vector2 screenSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        UpdatePosition();
    }

    private void Update()
    {
        // Vérifier si la taille de l'écran a changé
        if (screenSize.x != Screen.width || screenSize.y != Screen.height)
        {
            UpdatePosition();
            screenSize = new Vector2(Screen.width, Screen.height);
        }
    }

    public void UpdatePosition()
    {
        if (rectTransform == null) return;

        // Calculer la taille en fonction de l'écran
        float panelHeight = Screen.height * heightRatio;
        float panelWidth = Screen.width * widthRatio;
        
        // Calculer la position Y (depuis le haut)
        float topPosition = Screen.height * topOffset;
        
        // Mettre à jour la position et la taille
        rectTransform.sizeDelta = new Vector2(panelWidth, panelHeight);
        rectTransform.anchoredPosition = new Vector2(0, -topPosition);

        Debug.Log($"Screen: {Screen.width}x{Screen.height}, Panel: {panelWidth}x{panelHeight}, Y: {-topPosition}");
    }
} 