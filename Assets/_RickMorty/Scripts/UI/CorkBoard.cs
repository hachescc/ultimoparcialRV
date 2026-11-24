using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RickMorty
{
    public class CorkBoard : MonoBehaviour
    {
        public static CorkBoard Instance { get; private set; }

        public Vector2 sizeMeters = new Vector2(1.2f, 0.9f);
        [TextArea] public List<string> startingNotes = new List<string>
        {
            "Botón rojo = MEZCLAR\nMínimo 2 ingredientes",
            "Líquidos: IZQUIERDA\nSólidos: DERECHA",
            "La basura va al\nagujero negro\n(al lado de la mesa)",
            "NO tocar al capibara.\nEstá meditando."
        };

        const float Mpp = 0.001f;
        readonly List<string> m_Notes = new List<string>();
        readonly List<Vector2> m_Pins = new List<Vector2>();
        Transform m_Root, m_NotesLayer, m_StringsLayer, m_PinsLayer;
        static readonly Color[] s_NoteColors =
        {
            new Color(1f, 0.95f, 0.55f), new Color(0.65f, 1f, 0.65f), new Color(0.65f, 0.88f, 1f), new Color(1f, 0.74f, 0.82f)
        };

        void Awake()
        {
            Instance = this;
            var canvas = UIKit.WorldCanvas("CorkCanvas", transform, sizeMeters / Mpp, Mpp, false);
            m_Root = canvas.transform;
            UIKit.Fill("Cork", m_Root, new Color(0.62f, 0.45f, 0.28f), false);
            UIKit.Panel("Borde", m_Root, new Color(0.38f, 0.25f, 0.13f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, false).rectTransform.SetAsFirstSibling();
            UIKit.Panel("Corcho", m_Root, new Color(0.66f, 0.49f, 0.31f), Vector2.zero, Vector2.one, new Vector2(18, 18), new Vector2(-18, -18), false);
            var title = UIKit.Line("Title", m_Root, "IDEAS DE RICK (NO TOCAR)", 40, new Color(0.18f, 0.09f, 0.02f), TextAlignmentOptions.Center,
                new Vector2(0, 0.88f), new Vector2(1, 0.985f), 30, 0);
            UIKit.AsTitle(title);
            title.fontStyle = FontStyles.Bold;
            m_NotesLayer = UIKit.Rect("Notas", m_Root, Vector2.zero, Vector2.one);
            m_StringsLayer = UIKit.Rect("Hilos", m_Root, Vector2.zero, Vector2.one);
            m_PinsLayer = UIKit.Rect("Chinchetas", m_Root, Vector2.zero, Vector2.one);
            foreach (var n in startingNotes) AddNote(n);
        }

        public void AddNote(string text)
        {
            if (m_Notes.Contains(text)) return;
            int i = m_Notes.Count;
            if (i >= 12) return;
            m_Notes.Add(text);
            const int cols = 4;
            int cx = i % cols, cy = i / cols;
            float w = 1f / cols;
            var minA = new Vector2(cx * w, 0.86f - (cy + 1) * 0.285f);
            var maxA = new Vector2((cx + 1) * w, 0.86f - cy * 0.285f);
            var note = UIKit.Panel("Nota_" + i, m_NotesLayer, s_NoteColors[i % s_NoteColors.Length], minA, maxA, new Vector2(16, 12), new Vector2(-16, -14), false);
            note.rectTransform.localRotation = Quaternion.Euler(0, 0, ((i * 37) % 7 - 3) * 1.1f);
            var t = UIKit.Block("Texto", note.transform, text, 22, 31, new Color(0.12f, 0.1f, 0.08f), Vector2.zero, Vector2.one, 12);
            t.alignment = TextAlignmentOptions.Center;

            var size = sizeMeters / Mpp;
            var pin = new Vector2(((minA.x + maxA.x) * 0.5f - 0.5f) * size.x, (maxA.y - 0.5f) * size.y - 24f);
            if (m_Pins.Count > 0) AddString(m_Pins[m_Pins.Count - 1], pin);
            m_Pins.Add(pin);
            var p = UIKit.Rect("Chincheta_" + i, m_PinsLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            p.sizeDelta = new Vector2(20, 20);
            p.anchoredPosition = pin;
            var img = p.gameObject.AddComponent<Image>();
            img.sprite = UIKit.Rounded; img.type = Image.Type.Sliced; img.color = new Color(0.85f, 0.1f, 0.1f);
            img.raycastTarget = false;
        }

        void AddString(Vector2 a, Vector2 b)
        {
            var r = UIKit.Rect("Hilo", m_StringsLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var d = b - a;
            r.pivot = new Vector2(0, 0.5f);
            r.sizeDelta = new Vector2(d.magnitude, 3.5f);
            r.anchoredPosition = a;
            r.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var img = r.gameObject.AddComponent<Image>();
            img.color = new Color(0.78f, 0.06f, 0.06f, 0.9f);
            img.raycastTarget = false;
        }
    }
}
