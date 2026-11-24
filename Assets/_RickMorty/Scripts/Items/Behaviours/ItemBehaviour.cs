using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RickMorty
{
    [RequireComponent(typeof(MixableItem))]
    public abstract class ItemBehaviour : MonoBehaviour
    {
        protected MixableItem Item { get; private set; }
        protected Rigidbody Body => Item != null ? Item.Body : null;

        protected virtual void Awake()
        {
            Item = GetComponent<MixableItem>();
        }

        protected virtual void Start()
        {
            var g = GetComponent<XRGrabInteractable>();
            if (g == null) return;
            g.activated.AddListener(HandleActivated);
            g.deactivated.AddListener(HandleDeactivated);
            g.selectEntered.AddListener(HandleSelectEntered);
            g.selectExited.AddListener(HandleSelectExited);
        }

        void HandleActivated(ActivateEventArgs args) => OnActivate();
        void HandleDeactivated(DeactivateEventArgs args) => OnDeactivate();

        void HandleSelectEntered(SelectEnterEventArgs args)
        {
            if (!(args.interactorObject is XRSocketInteractor)) OnGrabbed();
        }

        void HandleSelectExited(SelectExitEventArgs args)
        {
            if (!(args.interactorObject is XRSocketInteractor)) OnReleased();
        }

        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }
        protected virtual void OnGrabbed() { }
        protected virtual void OnReleased() { }

        protected Vector3 Muzzle(float forward = 0.15f) => transform.position + transform.forward * forward;

        protected static void ApplyProjectileHit(RaycastHit hit, Vector3 dir, float force, bool laser)
        {
            var creature = hit.collider.GetComponentInParent<Creature>();
            if (creature != null)
            {
                if (laser && creature.invulnerableToLaser)
                {
                    creature.OnLaserBounce();
                    return;
                }
                creature.Kill(DamageType.Firearm);
                return;
            }
            var burn = hit.collider.GetComponentInParent<Burnable>();
            if (burn != null && laser) { burn.Burn(); return; }

            var rb = hit.rigidbody;
            if (rb != null && !rb.isKinematic) rb.AddForceAtPosition(dir * force, hit.point, ForceMode.Impulse);
        }
    }
}
