using System.Collections;
using TMPro;
using UnityEngine;

namespace RickMorty
{
    public static class FX
    {
        static int s_DecalCount;
        const int MaxDecals = 40;

        public static ParticleSystem Burst(Vector3 pos, Color color, int count = 40, float speed = 3f, float size = 0.06f,
            float lifetime = 0.8f, float gravity = 1f, bool additive = false)
        {
            var go = new GameObject("FX_Burst");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.maxParticles = count + 10;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.05f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(color, 0), new GradientColorKey(color, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MatLib.Particles(Color.white, additive);
            ps.Play();
            Object.Destroy(go, lifetime + 0.5f);
            return ps;
        }

        public static ParticleSystem Smoke(Vector3 pos, Color color, float duration = 3f, float radius = 0.3f, float size = 0.5f, float rate = 25f)
        {
            var go = new GameObject("FX_Smoke");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.gravityModifier = -0.05f;
            main.maxParticles = 300;
            var em = ps.emission;
            em.rateOverTime = rate;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = radius;
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 2f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(color, 0), new GradientColorKey(color, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(color.a, 0.2f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = MatLib.Particles(Color.white);
            ps.Play();
            Object.Destroy(go, duration + 3.5f);
            return ps;
        }

        public static ParticleSystem Attach(Transform parent, Color color, float rate = 6f, float size = 0.02f, float speed = 0.1f, float gravity = -0.05f, float radius = 0.03f, bool additive = true)
        {
            var go = new GameObject("FX_Loop");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = rate;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = radius;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(color, 0), new GradientColorKey(color, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = grad;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = MatLib.Particles(Color.white, additive);
            ps.Play();
            return ps;
        }

        public static void FlashLight(Vector3 pos, Color color, float intensity = 6f, float range = 6f, float duration = 0.4f)
        {
            var go = new GameObject("FX_Flash");
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            var fader = go.AddComponent<LightFader>();
            fader.duration = duration;
        }

        static Material s_TextMat;

        public static TextMeshPro FloatingText(Vector3 pos, string text, Color color, float fontSize = 1.2f, float life = 1.5f, float rise = 0.4f)
        {
            var go = new GameObject("FX_Text");
            go.transform.position = pos;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            if (s_TextMat == null && tmp.font != null)
            {
                s_TextMat = new Material(tmp.font.material) { name = "Texto flotante (contorno)" };
                s_TextMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
                s_TextMat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
                s_TextMat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            }
            if (s_TextMat != null) tmp.fontSharedMaterial = s_TextMat;
            tmp.rectTransform.sizeDelta = new Vector2(3f, 1f);
            var ft = go.AddComponent<FloatingTextMotion>();
            ft.life = life;
            ft.rise = rise;
            return tmp;
        }

        public static GameObject Decal(Vector3 pos, Vector3 normal, Color color, float size = 0.12f, float life = 30f)
        {
            if (s_DecalCount >= MaxDecals) return null;
            s_DecalCount++;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "FX_Decal";
            go.transform.position = pos + normal * 0.004f;
            go.transform.rotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            go.transform.localScale = Vector3.one * size * Random.Range(0.7f, 1.3f);
            go.GetComponent<Renderer>().sharedMaterial = MatLib.Particles(color);
            go.AddComponent<DecalLife>().life = life;
            return go;
        }

        internal static void DecalRemoved() => s_DecalCount = Mathf.Max(0, s_DecalCount - 1);

        public static PortalVisual Portal(Vector3 pos, Quaternion rot, float radius = 0.4f, float life = 1.5f)
        {
            var go = new GameObject("FX_Portal");
            go.transform.SetPositionAndRotation(pos, rot);
            var pv = go.AddComponent<PortalVisual>();
            pv.Build(radius, life);
            return pv;
        }

        public static void Explosion(Vector3 pos, float radius, float force, bool killCreatures, Color smokeColor)
        {
            Burst(pos, new Color(1f, 0.6f, 0.1f), 80, 6f, 0.1f, 0.7f, 0.5f, true);
            Smoke(pos, smokeColor, 1.6f, 0.3f, 0.45f, 14f);
            FlashLight(pos, new Color(1f, 0.8f, 0.5f), 10f, 8f, 0.5f);
            AudioFX.Play("explosion", pos, 1f);

            var cols = Physics.OverlapSphere(pos, radius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in cols)
            {
                var rb = c.attachedRigidbody;
                if (rb != null && !rb.isKinematic && rb.GetComponent<EasterEgg>() == null)
                    rb.AddExplosionForce(Mathf.Min(force, 4.5f), pos, radius, 0.4f, ForceMode.VelocityChange);
                if (killCreatures && (c.bounds.ClosestPoint(pos) - pos).magnitude < radius * 0.5f)
                {
                    var cr = c.GetComponentInParent<Creature>();
                    if (cr != null) cr.Kill(DamageType.Explosion, true);
                }
            }
        }
    }

    public class LightFader : MonoBehaviour
    {
        public float duration = 0.4f;
        Light m_Light;
        float m_Start, m_T;

        void Start() { m_Light = GetComponent<Light>(); m_Start = m_Light.intensity; }

        void Update()
        {
            m_T += Time.deltaTime;
            m_Light.intensity = Mathf.Lerp(m_Start, 0, m_T / duration);
            if (m_T >= duration) Destroy(gameObject);
        }
    }

    public class FloatingTextMotion : MonoBehaviour
    {
        public float life = 1.5f, rise = 0.4f;
        float m_T;
        TextMeshPro m_Text;

        void Start() => m_Text = GetComponent<TextMeshPro>();

        void Update()
        {
            m_T += Time.deltaTime;
            transform.position += Vector3.up * rise * Time.deltaTime;
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            if (m_Text != null)
            {
                var c = m_Text.color;
                c.a = 1f - Mathf.Clamp01((m_T - life * 0.6f) / (life * 0.4f));
                m_Text.color = c;
            }
            if (m_T >= life) Destroy(gameObject);
        }
    }

    public class DecalLife : MonoBehaviour
    {
        public float life = 30f;
        void Start() => Destroy(gameObject, life);
        void OnDestroy() => FX.DecalRemoved();
    }

    public class PortalVisual : MonoBehaviour
    {
        public float radius = 0.4f;
        public float life = 1.5f;
        Transform m_Disc, m_Inner;
        float m_T, m_S;
        bool m_Closing;

        static Transform Layer(Transform parent, string name, Material mat, float z)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = name;
            q.transform.SetParent(parent, false);
            q.transform.localPosition = new Vector3(0, 0, z);
            q.transform.localScale = Vector3.zero;
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return q.transform;
        }

        public void Build(float r, float lifeSeconds)
        {
            radius = r;
            life = lifeSeconds;
            m_Disc = Layer(transform, "Disc", ProcTex.PortalMaterial, 0f);
            m_Inner = Layer(transform, "Core", ProcTex.PortalCoreMaterial, -0.004f);

            var swirl = FX.Attach(transform, new Color(0.4f, 1f, 0.4f), 40f, 0.05f, 0.4f, 0f, r * 0.8f);
            var sh = swirl.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.rotation = new Vector3(0, 0, 0);
            var vel = swirl.velocityOverLifetime;
            vel.enabled = true;
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(4f);
            vel.space = ParticleSystemSimulationSpace.Local;
            var main = swirl.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            if (r >= 0.2f)
            {
                var light = gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.3f, 1f, 0.3f);
                light.range = r * 6f;
                light.intensity = 1.6f;
                light.shadows = LightShadows.None;
            }

            AudioFX.Play("portal", transform.position, 0.8f);
        }

        void Update()
        {
            m_T += Time.deltaTime;
            float target = radius * 2f;
            float s;
            if (m_Closing || (life > 0 && m_T > life - 0.3f))
                s = Mathf.MoveTowards(m_S, 0f, Time.deltaTime * target * 4f);
            else
                s = Mathf.MoveTowards(m_S, target, Time.deltaTime * target * 5f);
            m_S = s;
            float pulse = 1f + 0.035f * Mathf.Sin(m_T * 7f);
            m_Disc.localScale = new Vector3(s * 1.12f * pulse, s * 1.12f * pulse, 1f);
            m_Disc.Rotate(0, 0, 150f * Time.deltaTime, Space.Self);
            if (m_Inner != null)
            {
                m_Inner.localScale = new Vector3(s * 0.74f, s * 0.74f, 1f);
                m_Inner.Rotate(0, 0, -260f * Time.deltaTime, Space.Self);
            }
            if ((life > 0 && m_T >= life) || (m_Closing && s <= 0.001f)) Destroy(gameObject);
        }

        public void Close() => m_Closing = true;
    }
}
