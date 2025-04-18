using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class WantedClickThroughHandler : MonoBehaviour, IPointerDownHandler
{
    // Référence au GridManager (à assigner dans l'inspecteur)
    public GridManager gridManager;
    
    // Intervalle pour vérifier si la carte recherchée est visible
    public float checkInterval = 0.5f;
    
    // Pourcentage minimum de visibilité avant d'ajuster la position
    public float minimumVisiblePercentage = 0.1f;
    
    private void Start()
    {
        // Démarrer la vérification régulière
        StartCoroutine(CheckWantedVisibility());
    }
    
    // Coroutine pour vérifier périodiquement si la carte recherchée est suffisamment visible
    private IEnumerator CheckWantedVisibility()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            
            // S'assurer que le jeu est actif et que la roulette n'est pas en cours
            if (gridManager == null || gridManager.wantedCard == null || 
                gridManager.IsRouletteActive || !GameManager.Instance.isGameActive)
            {
                continue;
            }
            
            // Vérifier si la carte est partiellement visible
            if (!gridManager.wantedCard.IsPartiallyVisible())
            {
                // Ajuster légèrement la position pour rendre la carte visible
                RectTransform rt = gridManager.wantedCard.GetComponent<RectTransform>();
                Vector2 newPosition = rt.anchoredPosition + new Vector2(Random.Range(-50f, 50f), Random.Range(-50f, 50f));
                
                // S'assurer que la carte reste dans les limites du plateau
                newPosition.x = Mathf.Clamp(newPosition.x, -gridManager.playAreaWidth/2 + 100, gridManager.playAreaWidth/2 - 100);
                newPosition.y = Mathf.Clamp(newPosition.y, -gridManager.playAreaHeight/2 + 100, gridManager.playAreaHeight/2 - 100);
                
                // Appliquer la nouvelle position
                rt.anchoredPosition = newPosition;
            }
        }
    }

    // Cette méthode est appelée lorsque l'utilisateur touche/click sur l'objet auquel ce script est attaché.
    public void OnPointerDown(PointerEventData eventData)
    {
        // S'assurer que le GridManager et la carte wanted existent
        if (gridManager == null || gridManager.wantedCard == null)
            return;

        // Effectuer un raycast sur tous les éléments UI sous la position du clic
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        bool wantedFound = false;
        // Parcourir les résultats pour voir si la carte wanted fait partie des éléments raycastés
        foreach (var result in results)
        {
            if (result.gameObject == gridManager.wantedCard.gameObject)
            {
                wantedFound = true;
                break;
            }
        }

        // Si la carte wanted n'a pas été détectée (par exemple si un autre élément est au-dessus),
        // on vérifie manuellement si le clic se situe dans les bornes du RectTransform du wanted.
        if (!wantedFound)
        {
            RectTransform rt = gridManager.wantedCard.GetComponent<RectTransform>();
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                if (rt.rect.Contains(localPoint))
                {
                    wantedFound = true;
                }
            }
        }

        // Si la position est dans la zone du wanted, déclencher son événement de clic
        if (wantedFound)
        {
            // Vous pouvez utiliser ExecuteEvents pour déclencher l'interface IPointerClickHandler sur la carte wanted.
            ExecuteEvents.Execute(gridManager.wantedCard.gameObject, eventData, ExecuteEvents.pointerClickHandler);
        }
    }
}
