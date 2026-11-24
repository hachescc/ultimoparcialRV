using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Tazon de mezclas. Detecta los objetos dentro, da retroalimentacion haptica
    /// (ronroneo suave si la mezcla va bien, vibracion agresiva + tazon rojo si va a explotar)
    /// y al presionar MEZCLAR crea el resultado o provoca la explosion catastrofica.
    /// Lo usan: MixButton (llama a Mix), GameManager (misiones y final) y el builder de escena.
    /// </summary>
    public class MixingBowl : MonoBehaviour
    {
        public static MixingBowl Instance { get; private set; }

        [Header("Volumen interior (local)")]
        public Vector3 innerCenter = new Vector3(0, 0.1f, 0);
        public float innerRadius = 0.22f;
        public float innerHeight = 0.3f;
        public float previewRadius = 0.45f;

        [Header("Referencias")]
        public Transform outputPoint;
        public Renderer liquid;
        public Renderer glow;
        public Light bowlLight;

        [Header("Explosión")]
        public float explosionRadius = 3f;
        public float explosionForce = 6f;

        public bool Busy { get; private set; }

        float m_NextHaptic;
        float m_NextScan;
        bool m_Danger;
        Color m_LiquidColor = new Color(0.3f, 0.8f, 0.4f);
        readonly List<MixableItem> m_Contents = new List<MixableItem>();

        void Awake() => Instance = this;

        public bool Contains(MixableItem m)
        {
            if (m == null || m.Unavailable) return false;
            var local = transform.InverseTransformPoint(m.transform.position) - innerCenter;
            if (Mathf.Abs(local.y) > innerHeight * 0.5f) return false;
            local.y = 0;
            return local.magnitude <= innerRadius;
        }

        public List<MixableItem> Contents()
        {
            m_Contents.Clear();
            foreach (var m in MixableItem.All)
                // la Pistola de Portales real nunca cuenta como ingrediente: destruirla haria imposible ganar
                if (m != null && !m.IsHeld && m.itemId != "pistola_portales" && Contains(m)) m_Contents.Add(m);
            return m_Contents;
        }

        void Update()
        {
            if (Busy) return;
            if (Time.time < m_NextScan) return;
            m_NextScan = Time.time + 0.1f;

            var contents = Contents();
            var ids = IdsOf(contents);
            if (contents.Count > m_LastCount && Time.timeSinceLevelLoad > 2f)
                AudioFX.Play("plop", transform.position, 0.55f, 1f + 0.12f * contents.Count);   // confirma que el ingrediente SI quedo dentro
            m_LastCount = contents.Count;

            // Color del liquido = promedio de lo que hay dentro
            Color target = new Color(0.3f, 0.8f, 0.4f, 0.6f);
            if (contents.Count > 0)
            {
                Color sum = Color.black;
                foreach (var c in contents) sum += c.Def != null ? c.Def.Color : Color.white;
                target = sum / contents.Count;
                target.a = 0.75f;
            }
            m_LiquidColor = Color.Lerp(m_LiquidColor, target, 0.3f);
            if (liquid != null)
            {
                MatLib.SetColor(liquid, m_LiquidColor);
                float fill = Mathf.Clamp01(contents.Count / 3f);
                liquid.transform.localScale = new Vector3(liquid.transform.localScale.x, Mathf.Lerp(0.005f, 0.04f, fill), liquid.transform.localScale.z);
            }

            // Retroalimentacion haptica al acercar ingredientes con la mano
            bool anyHeldNear = false;
            bool danger = contents.Count > 0 && !GameDatabase.IsPartialOfAnyRecipe(ids);
            foreach (var m in MixableItem.All)
            {
                if (m == null || !m.IsHeld) continue;
                if ((m.transform.position - transform.TransformPoint(innerCenter)).sqrMagnitude > previewRadius * previewRadius) continue;
                anyHeldNear = true;
                var preview = new List<string>(ids) { m.itemId };
                bool ok = GameDatabase.IsPartialOfAnyRecipe(preview);
                if (!ok) danger = true;
                if (Time.time >= m_NextHaptic)
                {
                    if (ok) m.Haptic(0.12f, 0.15f);
                    else PlayerRig.Instance?.HapticPattern(m.HoldingHand, 0.8f, 3, 0.05f, 0.05f);
                }
            }
            if (anyHeldNear && Time.time >= m_NextHaptic) m_NextHaptic = Time.time + 0.5f;

            // Rojo = esa combinacion va a explotar. Ahora tambien lo DICE (antes solo cambiaba el color y nadie lo explicaba).
            if (danger && !m_Danger && contents.Count >= 2 && Time.time > m_NextDangerText)
            {
                m_NextDangerText = Time.time + 4f;
                FX.FloatingText(transform.position + Vector3.up * 0.42f, "¡MEZCLA INESTABLE!", new Color(1f, 0.3f, 0.25f), 0.8f, 2.2f, 0.12f);
            }
            m_Danger = danger;
            // luz roja estable con un latido lento (antes parpadeaba irregular a 3-5 Hz)
            UpdateGlow(danger ? new Color(1f, 0.15f, 0.1f) : new Color(0.3f, 1f, 0.4f), danger ? 1.6f + 0.35f * Mathf.Sin(Time.time * 4f) : 0.3f);
        }

        void UpdateGlow(Color c, float intensity)
        {
            if (glow != null) MatLib.SetColor(glow, new Color(c.r, c.g, c.b, intensity > 1f ? 0.35f : 0.14f));
            if (bowlLight != null)
            {
                bowlLight.color = c;
                bowlLight.intensity = intensity;
            }
        }

        bool m_FirstBoom = true;   // la primera explosion de la partida aturde mas (hay un solo tazon por escena)
        int m_LastCount;
        float m_NextDangerText;

        /// <summary>Lista con los ids de los objetos (en el mismo orden).</summary>
        static List<string> IdsOf(List<MixableItem> items)
        {
            var ids = new List<string>(items.Count);
            foreach (var item in items) ids.Add(item.itemId);
            return ids;
        }

        /// <summary>Llamado por el boton MEZCLAR.</summary>
        public void Mix()
        {
            if (Busy) return;
            var st = GameManager.Instance != null ? GameManager.Instance.State : GameState.FreePlay;
            if (st == GameState.Intro || st == GameState.Onboarding || st == GameState.RickLeaving)
            {
                // antes del tutorial (o mientras Rick se va) no se mezcla: evita fabricar la pistola final fuera de orden
                RickComms.Instance?.SayText("¡Todavía no, Morty! Primero haz lo que te estoy diciendo.");
                AudioFX.Play("beep", transform.position, 0.6f, 0.6f);
                return;
            }
            if (GameManager.Instance != null && GameManager.Instance.MixBlockedNow)
            {
                // Rick esta esperando que uses la Caja de Meeseeks: adelantarse hacia explotar la mezcla "correcta" de la mision 2
                RickComms.Instance?.SayText("¡Todavía no, Morty! Primero usa la Caja de Meeseeks: agárrala y aprieta el gatillo.");
                AudioFX.Play("beep", transform.position, 0.6f, 0.6f);
                FX.FloatingText(transform.position + Vector3.up * 0.45f, "Primero usa la Caja de Meeseeks", new Color(1f, 0.85f, 0.3f), 0.7f, 2.5f, 0.15f);
                return;
            }
            var contents = new List<MixableItem>(Contents());
            var ids = IdsOf(contents);

            if (contents.Count < 2)
            {
                RickComms.Instance?.Say(contents.Count == 0 ? "mix_empty" : "mix_need_more");
                AudioFX.Play("beep", transform.position, 0.6f, 0.6f);
                FX.FloatingText(transform.position + Vector3.up * 0.45f, "Mínimo 2 ingredientes en el tazón", new Color(1f, 0.85f, 0.3f), 0.7f, 2.5f, 0.15f);
                return;
            }

            if (GameDatabase.IsFinalCombo(ids))
            {
                RickComms.Instance?.Say("final_use_station");
                AudioFX.Play("beep", transform.position, 0.6f, 0.6f);
                FX.FloatingText(transform.position + Vector3.up * 0.45f, "Eso va en el SOPORTE DE ENSAMBLAJE", new Color(0.5f, 1f, 0.5f), 0.7f, 3f, 0.15f);
                return;
            }

            var recipe = GameDatabase.FindRecipe(ids);
            var gm = GameManager.Instance;
            bool allowed = gm == null || gm.EvaluateMix(recipe, ids);
            if (recipe != null && allowed) StartCoroutine(CoSuccess(recipe, contents));
            else StartCoroutine(CoFail(contents, recipe == null ? "combinacion inexistente" : "mezcla equivocada"));
        }

        IEnumerator CoSuccess(RecipeDef recipe, List<MixableItem> contents)
        {
            Busy = true;
            var center = transform.TransformPoint(innerCenter);
            AudioFX.Play("bubbles", center, 1f);
            var swirl = FX.Attach(transform, m_LiquidColor, 60f, 0.04f, 0.6f, -0.3f, 0.15f);
            swirl.transform.localPosition = innerCenter;
            float t = 0;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                UpdateGlow(Color.Lerp(m_LiquidColor, Color.white, t / 1.2f), 1f + t * 3f);
                yield return null;
            }
            Destroy(swirl.gameObject, 0.1f);

            foreach (var c in contents) if (c != null) c.Consume(0.2f, false);

            var pos = outputPoint != null ? outputPoint.position : center + Vector3.up * 0.4f;
            FX.Portal(pos + Vector3.up * 0.25f, Quaternion.LookRotation(Vector3.down), 0.18f, 1.2f);
            FX.Burst(pos, new Color(0.5f, 1f, 0.5f), 40, 2f, 0.03f, 0.6f, 0.3f, true);
            FX.FlashLight(center, new Color(0.5f, 1f, 0.5f), 4f, 4f, 0.5f);
            var item = ItemFactory.Spawn(recipe.result, pos + Vector3.up * 0.15f, Quaternion.Euler(0, Random.Range(-30f, 30f), 0));

            bool first = ScoreManager.Instance != null && ScoreManager.Instance.RegisterDiscovery(recipe.result);
            GameEvents.RaiseMixSucceeded(recipe, first);
            PlayerRig.Instance?.Haptic(Hand.Both, 0.5f, 0.25f);
            GameManager.Instance?.OnMixResolved(recipe, item);
            yield return new WaitForSeconds(0.5f);
            Busy = false;
        }

        IEnumerator CoFail(List<MixableItem> contents, string reason)
        {
            Busy = true;
            var center = transform.TransformPoint(innerCenter);

            // Advertencia: tazon rojo 0.5 s + vibracion agresiva para que el jugador se cubra
            AudioFX.Play("alarm", center, 0.8f, 1.4f);
            PlayerRig.Instance?.HapticPattern(Hand.Both, 1f, 5, 0.06f, 0.04f);
            float t = 0;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                UpdateGlow(new Color(1f, 0.05f, 0.05f), 4f + Mathf.Sin(t * 50f) * 2f);
                yield return null;
            }

            // Explosion
            foreach (var c in contents) if (c != null) c.Consume(1.2f, false);
            FX.Explosion(center + Vector3.up * 0.1f, explosionRadius, explosionForce, true, new Color(0.05f, 0.05f, 0.05f, 0.85f));
            var rig = PlayerRig.Instance;
            if (rig != null && (rig.HeadPosition - center).magnitude < 5f)
            {
                // destello corto y tibio (antes: blanco total 2 s en CADA mezcla fallida; molesto y agresivo en el visor)
                ScreenFX.Instance?.Flash(new Color(1f, 0.95f, 0.8f, 0.6f), 0.08f, 0.7f);
                ScreenFX.Instance?.Muffle(m_FirstBoom ? 3f : 1.5f);
                AudioFX.Play2D("tinnitus", m_FirstBoom ? 0.45f : 0.15f);
                m_FirstBoom = false;
                rig.Haptic(Hand.Both, 1f, 0.5f);
            }
            GameEvents.RaiseMixFailed(reason);
            GameManager.Instance?.OnMixResolved(null, null);
            yield return new WaitForSeconds(1.5f);
            Busy = false;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(innerCenter, new Vector3(innerRadius * 2, innerHeight, innerRadius * 2));
        }
    }
}
