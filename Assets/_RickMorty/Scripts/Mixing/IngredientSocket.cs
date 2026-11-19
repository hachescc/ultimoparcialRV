using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RickMorty
{
    /// <summary>
    /// Socket XR que solo acepta objetos con ciertos ids (el soporte de cada frasco base,
    /// o el soporte de ensamblaje final que solo acepta la Portal Gun de carton).
    /// </summary>
    public class IngredientSocket : XRSocketInteractor
    {
        public List<string> acceptedIds = new List<string>();

        bool Accepts(IXRInteractable interactable)
        {
            if (acceptedIds == null || acceptedIds.Count == 0) return true;
            var item = interactable != null ? interactable.transform.GetComponent<MixableItem>() : null;
            return item != null && !item.Unavailable && acceptedIds.Contains(item.itemId);
        }

        public override bool CanHover(IXRHoverInteractable interactable)
        {
            return base.CanHover(interactable) && Accepts(interactable);
        }

        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) && Accepts(interactable);
        }

        public MixableItem HeldItem
        {
            get
            {
                if (!hasSelection) return null;
                return interactablesSelected[0].transform.GetComponent<MixableItem>();
            }
        }
    }
}
