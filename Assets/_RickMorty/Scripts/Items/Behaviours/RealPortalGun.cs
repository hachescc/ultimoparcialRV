using System.Collections;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// La Pistola de Portales real. Al agarrarla por primera vez se dispara la escena final.
    /// Gatillo: abre un portal donde apuntes. Si apuntas a un objeto suelto lo absorbe y lo
    /// escupe por un portal en el techo (ciclo infinito de objetos cayendo).
    /// </summary>
    public class RealPortalGun : ItemBehaviour
    {
        public float range = 30f;
        bool m_GrabbedOnce;
        float m_Next;
        Light m_Glow;

        protected override void Start()
        {
            base.Start();
            AudioFX.AttachLoop(gameObject, "hum", 0.25f, 4f, 1f);
            m_Glow = gameObject.AddComponent<Light>();
            m_Glow.type = LightType.Point;
            m_Glow.color = new Color(0.3f, 1f, 0.3f);
            m_Glow.range = 1.2f;
            m_Glow.intensity = 1.5f;
            m_Glow.shadows = LightShadows.None;
            var tube = transform.Find("Visual/Tube");
            if (tube != null) FX.Attach(tube, new Color(0.5f, 1f, 0.4f), 15f, 0.012f, 0.05f, -0.2f, 0.01f);
        }

        void Update()
        {
            if (m_Glow != null) m_Glow.intensity = 1.3f + 0.4f * Mathf.Sin(Time.time * 5f);
        }

        protected override void OnGrabbed()
        {
            Item.Haptic(0.4f, 0.3f);
            if (m_GrabbedOnce) return;
            m_GrabbedOnce = true;
            GameManager.Instance?.OnRealPortalGunGrabbed(this);
        }

        protected override void OnActivate()
        {
            if (Time.time < m_Next) return;
            m_Next = Time.time + 0.8f;
            Item.Haptic(1f, 0.35f);
            AudioFX.Play("zap", transform.position, 0.8f);
            var origin = Muzzle(0.22f);
            if (!Physics.Raycast(origin, transform.forward, out var hit, range, ~0, QueryTriggerInteraction.Ignore)) return;

            var other = hit.collider.GetComponentInParent<MixableItem>();
            if (other != null && other.gameObject != gameObject && !other.IsHeld && !other.IsBase)
            {
                StartCoroutine(CoAbsorb(other));
                return;
            }
            if (hit.rigidbody == null)
                GameManager.Instance?.ShootPortal(hit.point, hit.normal);
        }

        IEnumerator CoAbsorb(MixableItem target)
        {
            FX.Portal(target.transform.position, Quaternion.LookRotation(Vector3.up), 0.25f, 1f);
            yield return new WaitForSeconds(0.25f);
            if (target == null) yield break;
            var gm = GameManager.Instance;
            var ceiling = gm != null && gm.ceilingPortalPoint != null ? gm.ceilingPortalPoint.position : target.transform.position + Vector3.up * 2f;
            FX.Portal(ceiling, Quaternion.LookRotation(Vector3.down), 0.3f, 1.5f);
            target.Body.linearVelocity = Vector3.zero;
            target.transform.position = ceiling + Vector3.down * 0.15f;
            target.Body.position = target.transform.position;
        }
    }
}
