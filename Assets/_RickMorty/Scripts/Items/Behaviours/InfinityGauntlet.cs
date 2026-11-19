using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Guantelete del Infinito: gatillo = chasquido. Exactamente la mitad de los objetos
    /// sueltos desaparece en polvo (los ingredientes base regresan por el anti-estupidez).
    /// </summary>
    public class InfinityGauntlet : ItemBehaviour
    {
        float m_Next;

        protected override void Start()
        {
            base.Start();
            // Gemas de colores
            var colors = new[] { Color.red, Color.blue, Color.green, Color.yellow, new Color(1f, 0.5f, 0f), new Color(0.6f, 0.2f, 1f) };
            for (int i = 0; i < colors.Length; i++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(g.GetComponent<Collider>());
                g.transform.SetParent(transform, false);
                g.transform.localScale = Vector3.one * 0.025f;
                g.transform.localPosition = new Vector3(-0.06f + i * 0.024f, 0.09f, 0.04f);
                g.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(colors[i], 3f);
            }
        }

        protected override void OnActivate()
        {
            if (Time.time < m_Next) return;
            m_Next = Time.time + 5f;
            StartCoroutine(CoSnap());
        }

        IEnumerator CoSnap()
        {
            AudioFX.Play("snap", transform.position, 1f);
            Item.Haptic(1f, 0.3f);
            ScreenFX.Instance?.Flash(new Color(1f, 0.9f, 0.6f), 0.05f, 1f);
            yield return new WaitForSeconds(0.6f);

            var candidates = new List<MixableItem>();
            foreach (var m in MixableItem.All)
            {
                if (m == null || m == Item || m.Unavailable || m.IsHeld) continue;
                if (m.itemId == "pistola_portales") continue;
                candidates.Add(m);
            }
            // Mezcla aleatoria y elimina la mitad exacta
            for (int i = 0; i < candidates.Count; i++)
            {
                int j = Random.Range(i, candidates.Count);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }
            int half = candidates.Count / 2;
            for (int i = 0; i < half; i++)
            {
                var m = candidates[i];
                FX.Burst(m.transform.position, new Color(0.45f, 0.35f, 0.25f), 25, 0.6f, 0.03f, 1.5f, -0.1f);
                AudioFX.Play("dust", m.transform.position, 0.5f);
                m.Consume(1.5f, false);
                yield return new WaitForSeconds(0.05f);
            }
            RickComms.Instance?.Say("gauntlet_snap");
        }
    }
}
