using System.Collections;
using UnityEngine;

namespace RickMorty
{
    public class MeeseeksNPC : CreatureMover
    {
        public bool contraband;
        public float lifetime = 40f;

        MixableItem m_Carrying;
        MixableItem m_Target;
        bool m_Done;
        float m_Born;

        public static GameObject Spawn(Vector3 feet, bool contraband)
        {
            var go = new GameObject(contraband ? "Meeseeks de Contrabando" : "Mr. Meeseeks");
            go.transform.position = feet + Vector3.up * 0.55f;
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            var color = contraband ? new Color(0.65f, 0.25f, 0.25f) : new Color(0.35f, 0.7f, 1f);
            var reg = ItemVisualRegistry.Get();
            var modelEntry = reg != null ? reg.Find(contraband ? "meeseeks_kirkland" : "meeseeks_npc") : null;
            if (modelEntry != null)
            {
                var model = Instantiate(modelEntry.prefab, visual);
                float scale = contraband ? 1.05f / 0.32f : 1f;
                model.transform.localScale = Vector3.one * scale;
                model.transform.localPosition = contraband ? Vector3.zero : new Vector3(0, -0.55f, 0);
            }
            else BuildPrimitiveMeeseeks(visual, color);

            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.1f;
            col.radius = 0.18f;
            col.center = new Vector3(0, 0.05f, 0);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 30f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var cr = go.AddComponent<Creature>();
            cr.displayName = go.name;
            cr.splatColor = new Color(0.3f, 0.6f, 1f);
            cr.killLineId = "kill_meeseeks";

            var npc = go.AddComponent<MeeseeksNPC>();
            npc.contraband = contraband;
            npc.speed = 0.9f;
            npc.hop = true;

            FX.Burst(go.transform.position, new Color(0.4f, 0.8f, 1f), 30, 2f, 0.04f, 0.6f, 0f, true);
            AudioFX.Play("meeseeks", go.transform.position, 1f);
            RickComms.Instance?.Say(contraband ? "meeseeks_contraband_spawn" : "meeseeks_spawn");
            return go;
        }

        static void BuildPrimitiveMeeseeks(Transform visual, Color color)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(visual, false);
            body.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
            body.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(color);
            var headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(headObj.GetComponent<Collider>());
            headObj.transform.SetParent(visual, false);
            headObj.transform.localPosition = new Vector3(0, 0.55f, 0);
            headObj.transform.localScale = Vector3.one * 0.32f;
            headObj.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(color);
            var eyesRoot = new GameObject("Eyes").transform;
            eyesRoot.SetParent(headObj.transform, false);
            eyesRoot.localScale = Vector3.one * 3f;
            ItemFactory.AddEyes(eyesRoot, 0.3f);
        }

        protected override void Awake()
        {
            base.Awake();
            m_Born = Time.time;
        }

        protected override Vector3? CurrentGoal()
        {
            if (m_Done) return null;
            var gm = GameManager.Instance;
            if (contraband)
            {
                if (m_Target == null || m_Target.Unavailable)
                    m_Target = PickRandomBase();
                return m_Target != null ? m_Target.transform.position : (Vector3?)null;
            }

            if (m_Carrying != null)
                return gm != null && gm.trashHole != null ? gm.trashHole.transform.position : (Vector3?)null;
            if (m_Target == null || m_Target.Unavailable || m_Target.IsHeld)
                m_Target = FindJunk();
            return m_Target != null ? m_Target.transform.position : (Vector3?)null;
        }

        static MixableItem PickRandomBase()
        {
            var list = new System.Collections.Generic.List<MixableItem>();
            foreach (var m in MixableItem.All) if (m != null && m.IsBase && !m.Unavailable && !m.IsHeld) list.Add(m);
            return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
        }

        MixableItem FindJunk()
        {
            MixableItem best = null;
            float bestD = 36f;
            foreach (var m in MixableItem.All)
            {
                if (m == null || m.IsBase || m.Unavailable || m.IsHeld || m.InSocket) continue;
                if (m.GetComponent<Creature>() != null) continue;
                if (m.itemId == "pistola_portales" || m.itemId == "portal_carton" || m.itemId == "aguardiente") continue;
                if (m.transform.position.y > 0.4f) continue;
                float d = (m.transform.position - transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = m; }
            }
            return best;
        }

        protected override void Update()
        {
            base.Update();
            if (m_Done) return;
            if (Time.time - m_Born > lifetime) { StartCoroutine(CoPoof("¡Ya terminé! ¡Adiós!")); return; }

            if (contraband)
            {
                bool close = m_Target != null && FlatDelta(m_Target.transform.position).sqrMagnitude < 0.8f;
                bool tired = m_Target != null && Time.time - m_Born > 12f;
                if (close || tired)
                {
                    if (m_Target.Base != null) m_Target.Base.Steal(15f);
                    StartCoroutine(CoPoof("*cof cof* Je, je... me llevo esto. *cof*"));
                }
                return;
            }

            if (m_Carrying == null && m_Target != null && FlatDelta(m_Target.transform.position).sqrMagnitude < 0.25f)
            {
                m_Carrying = m_Target;
                m_Carrying.Unavailable = true;
                m_Carrying.Body.isKinematic = true;
                foreach (var c in m_Carrying.GetComponentsInChildren<Collider>()) c.enabled = false;
                m_Carrying.transform.SetParent(transform, true);
                m_Carrying.transform.localPosition = new Vector3(0, 0.9f, 0.1f);
                RickComms.Instance?.SayText("¡Ooooh, sí se puede!", "Sr. Meeseeks");
            }

            var gm = GameManager.Instance;
            if (m_Carrying != null && gm != null && gm.trashHole != null &&
                FlatDelta(gm.trashHole.transform.position).sqrMagnitude < 0.4f)
            {
                var junk = m_Carrying;
                m_Carrying = null;
                junk.transform.SetParent(null, true);
                gm.trashHole.Swallow(junk);
                StartCoroutine(CoPoof("¡Listo! ¡Ya puedo dejar de existir!"));
            }
        }

        IEnumerator CoPoof(string line)
        {
            m_Done = true;
            FX.FloatingText(transform.position + Vector3.up * 0.9f, line, Color.white, 0.5f, 2f, 0.1f);
            if (m_Carrying != null)
            {
                m_Carrying.transform.SetParent(null, true);
                foreach (var c in m_Carrying.GetComponentsInChildren<Collider>()) c.enabled = true;
                m_Carrying.Body.isKinematic = false;
                m_Carrying.Unavailable = false;
                m_Carrying = null;
            }
            yield return new WaitForSeconds(1.2f);
            FX.Burst(transform.position, new Color(0.4f, 0.7f, 1f), 50, 2.5f, 0.05f, 0.8f, 0.3f);
            AudioFX.Play("splat", transform.position, 0.7f, 1.4f);
            Destroy(gameObject);
        }
    }
}
