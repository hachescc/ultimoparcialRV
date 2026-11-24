using System.Collections;
using TMPro;
using UnityEngine;

namespace RickMorty
{
    public class CardboardPortalGun : ItemBehaviour
    {
        static bool s_Commented;
        public static void ResetStatics() => s_Commented = false;
        float m_Next;

        protected override void OnActivate()
        {
            if (Time.time < m_Next) return;
            m_Next = Time.time + 1.2f;
            StartCoroutine(CoBang());
            if (Physics.Raycast(Muzzle(0.2f), transform.forward, out var hit, 15f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                FX.Decal(hit.point, hit.normal, new Color(0.2f, 0.8f, 0.2f, 0.9f), 0.2f, 40f);
                FX.Burst(hit.point, new Color(0.3f, 0.9f, 0.3f), 15, 1.5f, 0.03f, 0.5f, 1f);
            }
            AudioFX.Play("splat", transform.position, 0.6f, 1.4f);
            Item.Haptic(0.3f, 0.1f);
            if (!s_Commented)
            {
                s_Commented = true;
                RickComms.Instance?.Say("cardboard_bang");
            }
        }

        IEnumerator CoBang()
        {
            var flag = new GameObject("BangFlag");
            flag.transform.SetParent(transform, false);
            flag.transform.localPosition = new Vector3(0, 0.05f, 0.2f);
            var stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(stick.GetComponent<Collider>());
            stick.transform.SetParent(flag.transform, false);
            stick.transform.localRotation = Quaternion.Euler(90, 0, 0);
            stick.transform.localScale = new Vector3(0.005f, 0.06f, 0.005f);
            stick.transform.localPosition = new Vector3(0, 0, 0.06f);
            stick.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.6f, 0.45f, 0.3f));
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(board.GetComponent<Collider>());
            board.transform.SetParent(flag.transform, false);
            board.transform.localPosition = new Vector3(0, 0.04f, 0.12f);
            board.transform.localScale = new Vector3(0.002f, 0.06f, 0.11f);
            board.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(Color.white);
            var txt = new GameObject("Text").AddComponent<TextMeshPro>();
            txt.transform.SetParent(flag.transform, false);
            txt.transform.localPosition = new Vector3(0.0015f, 0.04f, 0.12f);
            txt.transform.localRotation = Quaternion.Euler(0, -90, 0);
            txt.text = "¡BANG!";
            txt.fontSize = 0.35f;
            txt.color = Color.red;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.rectTransform.sizeDelta = new Vector2(0.11f, 0.06f);
            float t = 0;
            while (t < 1.3f)
            {
                t += Time.deltaTime;
                float s = t < 0.15f ? Mathf.Lerp(0.1f, 1.2f, t / 0.15f) : 1f + 0.1f * Mathf.Sin(t * 30f) * Mathf.Exp(-t * 4f);
                flag.transform.localScale = Vector3.one * s;
                yield return null;
            }
            Destroy(flag);
        }
    }
}
