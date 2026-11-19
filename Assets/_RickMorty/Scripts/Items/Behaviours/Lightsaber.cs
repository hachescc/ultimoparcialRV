using UnityEngine;

namespace RickMorty
{
    /// <summary>Sable de luz: el gatillo despliega la hoja azul con zumbido. Corta criaturas y quema superficies.</summary>
    public class Lightsaber : ItemBehaviour
    {
        public float bladeLength = 0.9f;
        public Color bladeColor = new Color(0.3f, 0.6f, 1f);

        GameObject m_Blade;
        AudioSource m_Hum;
        bool m_On;
        float m_NextMark;

        protected override void Awake()
        {
            base.Awake();
            m_Blade = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            m_Blade.name = "Blade";
            m_Blade.transform.SetParent(transform, false);
            m_Blade.transform.localRotation = Quaternion.Euler(90, 0, 0);
            m_Blade.transform.localPosition = new Vector3(0, 0, 0.12f + bladeLength * 0.5f);
            m_Blade.transform.localScale = new Vector3(0.035f, bladeLength * 0.5f, 0.035f);
            m_Blade.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(bladeColor, 4f);
            var col = m_Blade.GetComponent<Collider>();
            col.isTrigger = true;
            var light = m_Blade.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = bladeColor;
            light.range = 1.5f;
            light.intensity = 1.5f;
            light.shadows = LightShadows.None;
            m_Hum = AudioFX.AttachLoop(m_Blade, "hum", 0.25f, 4f, 1.6f);
            m_Blade.SetActive(false);
        }

        protected override void OnActivate()
        {
            m_On = !m_On;
            m_Blade.SetActive(m_On);
            AudioFX.Play("saber_on", transform.position, 0.7f, m_On ? 1f : 0.7f);
            Item.Haptic(0.5f, 0.1f);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!m_On) return;
            var creature = other.GetComponentInParent<Creature>();
            if (creature != null && creature.gameObject != gameObject)
            {
                creature.Kill(DamageType.Melee);
                Item.Haptic(0.8f, 0.12f);
            }
        }

        void Update()
        {
            if (!m_On || Time.time < m_NextMark) return;
            // Quemaduras en paredes/mesa: rayo a lo largo de la hoja
            var start = transform.position + transform.forward * 0.12f;
            if (Physics.Raycast(start, transform.forward, out var hit, bladeLength, ~0, QueryTriggerInteraction.Ignore) && hit.rigidbody == null)
            {
                m_NextMark = Time.time + 0.08f;
                FX.Decal(hit.point, hit.normal, new Color(0.1f, 0.05f, 0.02f, 0.85f), 0.05f, 40f);
                FX.Burst(hit.point, new Color(1f, 0.7f, 0.3f), 4, 1f, 0.012f, 0.25f, 1f, true);
                Item.Haptic(0.3f, 0.05f);
            }
        }
    }
}
