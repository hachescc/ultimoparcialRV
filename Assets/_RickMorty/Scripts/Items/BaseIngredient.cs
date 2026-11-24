using System.Collections;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Sistema "Anti-Estupidez": los 8 frascos base nunca se pierden. Si tocan el suelo,
    /// caen fuera del mapa, se usan en una mezcla o se los roban, un micro-portal se los
    /// traga y otro los devuelve a su soporte (socket) en la mesa.
    /// Lo usan: MixableItem (lo consulta al consumir o robar) y los Meeseeks; el builder lo pone en cada frasco base.
    /// </summary>
    [RequireComponent(typeof(MixableItem))]
    public class BaseIngredient : MonoBehaviour
    {
        public IngredientSocket homeSocket;
        public Vector3 homePosition;
        public Quaternion homeRotation = Quaternion.identity;

        static bool s_FirstFloorRespawn = true;

        MixableItem m_Item;
        Renderer[] m_Renderers;
        Collider[] m_Colliders;
        bool m_Returning;
        float m_AwayTime, m_LooseTime;

        bool NearWorkSpot()
        {
            var gm = GameManager.Instance;
            if (gm == null) return false;
            var p = transform.position;
            if (gm.mixingBowl != null && (gm.mixingBowl.transform.position - p).sqrMagnitude < 0.38f * 0.38f) return true;
            if (gm.finalStation != null && (gm.finalStation.transform.position - p).sqrMagnitude < 0.45f * 0.45f) return true;
            return false;
        }

        void Awake()
        {
            m_Item = GetComponent<MixableItem>();
            m_Renderers = GetComponentsInChildren<Renderer>(true);
            m_Colliders = GetComponentsInChildren<Collider>(true);
            if (homePosition == Vector3.zero)
            {
                homePosition = transform.position;
                homeRotation = transform.rotation;
            }
            s_FirstFloorRespawn = true;
        }

        void Start()
        {
            // Detalles "vivos" de cada ingrediente segun el documento de diseno
            switch (m_Item.itemId)
            {
                case "acido": // burbujea violentamente y suelta humo toxico verde
                    FX.Attach(transform, new Color(0.5f, 1f, 0.4f, 0.8f), 10f, 0.025f, 0.15f, -0.05f, 0.02f);
                    AudioFX.AttachLoop(gameObject, "bubbles", 0.12f, 2f);
                    break;
                case "kalaxianos": // zumbido electrico grave
                    AudioFX.AttachLoop(gameObject, "hum", 0.1f, 2f, 0.7f);
                    FX.Attach(transform, new Color(0.7f, 0.3f, 1f), 3f, 0.015f, 0.05f, 0f, 0.03f);
                    break;
                case "mega_semilla": // resplandor bioluminiscente
                    var l = gameObject.AddComponent<Light>();
                    l.type = LightType.Point; l.range = 0.5f; l.intensity = 0.6f; l.color = new Color(1f, 0.8f, 0.2f); l.shadows = LightShadows.None;
                    break;
                case "plumbus_polvo": // destellos minusculos
                    FX.Attach(transform, new Color(1f, 0.8f, 0.8f), 4f, 0.01f, 0.03f, -0.02f, 0.03f);
                    break;
            }
        }

        void OnCollisionEnter(Collision c)
        {
            if (m_Returning || m_Item.IsHeld || m_Item.InSocket) return;
            var surface = c.collider.GetComponentInParent<SurfaceType>();
            if (surface != null && surface.kind == SurfaceKind.Ground)
                ReturnHome(true, 0.35f);
        }

        void Update()
        {
            if (m_Returning) return;
            if (transform.position.y < -3f || transform.position.y > 60f) ReturnHome(false, 0f);

            // Quieto y lejos de su soporte (encima de un techo, del estante alto, detras de una caja...):
            // a los 15 s vuelve solo. Sin esto se podia perder un ingrediente de la receta final para toda la partida.
            bool stray = !m_Item.IsHeld && !m_Item.InSocket && !m_Item.Unavailable
                && (transform.position - homePosition).sqrMagnitude > 1.3f * 1.3f
                && m_Item.Body != null && m_Item.Body.linearVelocity.sqrMagnitude < 0.02f;
            m_AwayTime = stray ? m_AwayTime + Time.deltaTime : 0f;
            if (m_AwayTime > 15f) { m_AwayTime = 0f; ReturnHome(false, 0f); }

            // Soltado sobre la mesa (acostado o parado fuera de su soporte): a los 6 s vuelve a su puesto.
            // No aplica dentro del tazon ni junto al soporte de ensamblaje, donde se dejan a proposito.
            bool loose = !m_Item.IsHeld && !m_Item.InSocket && !m_Item.Unavailable && !stray
                && m_Item.Body != null && m_Item.Body.linearVelocity.sqrMagnitude < 0.02f && !NearWorkSpot();
            m_LooseTime = loose ? m_LooseTime + Time.deltaTime : 0f;
            if (m_LooseTime > 6f) { m_LooseTime = 0f; ReturnHome(false, 0f); }
        }

        public void ReturnHome(bool fromFloor, float delay)
        {
            if (m_Returning || !isActiveAndEnabled) return;
            StartCoroutine(CoReturn(fromFloor, delay, 0f));
        }

        /// <summary>Lo roba un Meeseeks de contrabando: desaparece unos segundos.</summary>
        public void Steal(float seconds)
        {
            if (m_Returning) return;
            StartCoroutine(CoReturn(false, 0f, seconds));
        }

        IEnumerator CoReturn(bool fromFloor, float delay, float hiddenSeconds)
        {
            m_Returning = true;
            m_Item.Unavailable = true;
            m_Item.ForceRelease();
            if (delay > 0) yield return new WaitForSeconds(delay);

            // Portal que se lo traga
            FX.Portal(transform.position + Vector3.up * 0.01f, Quaternion.identity, 0.12f, 0.9f);
            float t = 0;
            var startScale = transform.localScale;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, startScale * 0.05f, t / 0.35f);
                yield return null;
            }
            SetVisible(false);
            transform.localScale = startScale;
            var rb = m_Item.Body;
            rb.isKinematic = true;
            yield return new WaitForSeconds(0.3f + hiddenSeconds);

            // Portal que lo escupe en su soporte
            var target = homeSocket != null ? homeSocket.transform.position : homePosition;
            var rot = homeSocket != null ? homeSocket.transform.rotation : homeRotation;
            FX.Portal(target + Vector3.up * 0.25f, Quaternion.Euler(180, 0, 0), 0.1f, 0.9f);
            transform.SetPositionAndRotation(target, rot);
            rb.position = target;
            rb.rotation = rot;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            SetVisible(true);
            m_Item.Unavailable = false;
            m_Returning = false;

            if (fromFloor && s_FirstFloorRespawn)
            {
                s_FirstFloorRespawn = false;
                RickComms.Instance?.Say("respawn_first");
            }
        }

        void SetVisible(bool v)
        {
            foreach (var r in m_Renderers) if (r != null) r.enabled = v;
            foreach (var c in m_Colliders) if (c != null) c.enabled = v;
        }
    }
}
