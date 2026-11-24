using System.Collections;
using UnityEngine;

namespace RickMorty
{
    public class Creature : MonoBehaviour
    {
        public string displayName = "Criatura";
        public Color splatColor = Color.red;
        public int moralPenalty;
        public string killLineId;
        public bool invulnerableToLaser;

        public bool IsDead { get; private set; }

        float m_LastLaserLine = -100f;

        public virtual void OnLaserBounce()
        {
            AudioFX.Play("impact_metal", transform.position, 0.6f, 1.5f);
            FX.Burst(transform.position + Vector3.up * 0.2f, new Color(0.2f, 1f, 0.9f), 10, 1.5f, 0.02f, 0.3f, 0f, true);
            if (Time.time - m_LastLaserLine > 10f)
            {
                m_LastLaserLine = Time.time;
                RickComms.Instance?.Say("laser_capybara");
            }
        }

        void SpawnChunks(Vector3 pos, int n)
        {
            var mat = MatLib.Lit(Color.Lerp(splatColor, new Color(1f, 0.55f, 0.6f), 0.4f), 0.15f, 0.85f);
            for (int i = 0; i < n; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                c.name = "Pedazo";
                c.transform.position = pos + Random.insideUnitSphere * 0.08f;
                var s = Random.Range(0.035f, 0.065f);
                c.transform.localScale = new Vector3(s * Random.Range(1f, 1.6f), s * Random.Range(0.6f, 1f), s * Random.Range(1f, 1.4f));
                c.transform.rotation = Random.rotation;
                c.GetComponent<Renderer>().sharedMaterial = mat;
                var rb = c.AddComponent<Rigidbody>();
                rb.mass = 0.05f;
                rb.AddForce((Random.insideUnitSphere + Vector3.up * 1.2f) * Random.Range(2f, 3.5f), ForceMode.VelocityChange);
                Destroy(c, Random.Range(3f, 5f));
            }
        }

        public void Kill(DamageType type, bool accidental = false)
        {
            if (IsDead) return;
            IsDead = true;
            StartCoroutine(CoDie(type, accidental));
        }

        IEnumerator CoDie(DamageType type, bool accidental)
        {
            var item = GetComponent<MixableItem>();
            if (item != null) { item.Unavailable = true; item.ForceRelease(); }
            var rb = GetComponent<Rigidbody>();
            foreach (var mb in GetComponents<CreatureMover>()) mb.enabled = false;

            if (rb != null) { rb.isKinematic = true; }
            yield return new WaitForSeconds(0.2f);

            var start = transform.localScale;
            if (type == DamageType.Firearm)
            {
                AudioFX.Play("whoosh", transform.position, 0.5f, 1.8f);
                float t = 0;
                while (t < 0.5f)
                {
                    t += Time.deltaTime;
                    transform.localScale = start * (1f + t * 1.2f) + Vector3.one * Mathf.Sin(t * 60f) * 0.01f;
                    yield return null;
                }
            }
            else if (type == DamageType.Melee)
            {
                float t = 0;
                while (t < 0.6f)
                {
                    t += Time.deltaTime;
                    transform.localScale = new Vector3(start.x * (1f + t), start.y * (1f - t * 0.8f), start.z);
                    yield return null;
                }
            }

            var pos = transform.position;
            var dark = Color.Lerp(splatColor, Color.black, 0.45f);
            var light = Color.Lerp(splatColor, Color.white, 0.55f);
            FX.Burst(pos, light, 18, 1.2f, 0.35f, 0.25f, 0f, true);
            FX.Burst(pos, Color.Lerp(splatColor, light, 0.3f), 40, 3.5f, 0.16f, 1.1f, 2.5f);
            FX.Burst(pos, splatColor, 25, 2f, 0.1f, 0.9f, 2.5f);
            FX.FlashLight(pos, splatColor, 3f, 3f, 0.3f);
            AudioFX.Play("splat", pos, 1f, Random.Range(0.8f, 1.1f));
            AudioFX.Play("explosion", pos, 0.35f, 1.8f);
            FX.FloatingText(pos + Vector3.up * 0.35f, UIKit.TitleTag("¡SPLAT!"), new Color(1f, 0.85f, 0.15f), 1.4f, 1.4f, 0.5f);
            SpawnChunks(pos, 7);
            if (Physics.Raycast(pos + Vector3.up * 0.1f, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                FX.Decal(hit.point, hit.normal, new Color(splatColor.r, splatColor.g, splatColor.b, 0.95f), 0.8f, 60f);
                for (int i = 0; i < 4; i++)
                {
                    var off = Random.insideUnitCircle * 0.45f;
                    FX.Decal(hit.point + new Vector3(off.x, 0, off.y), hit.normal, new Color(dark.r, dark.g, dark.b, 0.9f), Random.Range(0.15f, 0.3f), 60f);
                }
            }

            if (!string.IsNullOrEmpty(killLineId)) RickComms.Instance?.Say(killLineId);
            GameEvents.RaiseCreatureKilled(this, accidental);
            Destroy(gameObject);
        }
    }
}
