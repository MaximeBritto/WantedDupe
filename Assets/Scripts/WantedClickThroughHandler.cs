using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class WantedClickThroughHandler : MonoBehaviour, IPointerDownHandler
{
    // Référence au GridManager (à assigner dans l'inspecteur)
    public GridManager gridManager;

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
