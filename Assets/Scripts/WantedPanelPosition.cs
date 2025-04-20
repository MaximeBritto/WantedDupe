using UnityEngine;

public class WantedPanelPosition : MonoBehaviour
{
    [Header("Position Settings")]
    [Range(0f, 0.5f)] public float topOffset = 0.1f;    // % de l'écran depuis le haut
    [Range(0f, 1f)] public float heightRatio = 0.25f;   // % de l'écran pour la hauteur
    [Range(0f, 1f)] public float widthRatio = 0.8f;     // % de l'écran pour la largeur
    
    [Header("Tablet Settings")]
    [Range(0f, 0.5f)] public float tabletTopOffset = 0.05f;  // % plus petit pour tablettes
    [Range(0f, 1f)] public float tabletHeightRatio = 0.2f;   // % plus petit pour tablettes
    [Range(0f, 1f)] public float tabletWidthRatio = 0.6f;    // % plus petit pour tablettes

    private RectTransform rectTransform;
    private Vector2 screenSize;
    private bool isTablet = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        isTablet = IsTablet();
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

    // Méthode pour détecter les tablettes basée sur la taille d'écran
    private bool IsTablet()
    {
        // Vérifier si c'est un appareil mobile d'abord
        if (!Application.isMobilePlatform)
            return false;
            
        // Résolution minimum d'une tablette (en général 1280x720 ou plus)
        float minTabletDiagonal = 1500f; // Valeur approximative pour identifier une tablette
        
        // Calculer la diagonale en pixels
        float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
        
        // Log pour le débogage
        Debug.Log($"WantedPanelPosition - Détection tablette: Diagonale écran = {screenDiagonal}px, Width = {Screen.width}, Height = {Screen.height}");
        
        return screenDiagonal >= minTabletDiagonal;
    }

    public void UpdatePosition()
    {
        if (rectTransform == null) return;

        // Utiliser les paramètres appropriés en fonction du type d'appareil
        float useTopOffset = isTablet ? tabletTopOffset : topOffset;
        float useHeightRatio = isTablet ? tabletHeightRatio : heightRatio;
        float useWidthRatio = isTablet ? tabletWidthRatio : widthRatio;

        // Calculer la taille en fonction de l'écran
        float panelHeight = Screen.height * useHeightRatio;
        float panelWidth = Screen.width * useWidthRatio;
        
        // Calculer la position Y (depuis le haut)
        float topPosition = Screen.height * useTopOffset;
        
        // Mettre à jour la position et la taille
        rectTransform.sizeDelta = new Vector2(panelWidth, panelHeight);
        rectTransform.anchoredPosition = new Vector2(0, -topPosition);

        Debug.Log($"WantedPanel: Screen: {Screen.width}x{Screen.height}, Panel: {panelWidth}x{panelHeight}, Y: {-topPosition}, IsTablet: {isTablet}");
    }
}