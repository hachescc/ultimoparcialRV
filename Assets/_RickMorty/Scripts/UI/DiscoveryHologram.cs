using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RickMorty
{
    /// <summary>
    /// Holograma de Descubrimientos: cuadricula con todos los objetos del multiverso.
    /// Los no descubiertos salen como "???". Al apuntar con la mano derecha a uno descubierto
    /// se despliega su receta (asi no hay que memorizar las combinaciones).
    /// </summary>
    public class DiscoveryHologram : MonoBehaviour
    {
        public Transform head;
        public int columns = 7;

        Canvas m_Canvas;
        CanvasGroup m_Group;
        TextMeshProUGUI m_Detail, m_Header;
        readonly Dictionary<string, (Image bg, TextMeshProUGUI label)> m_Cells = new Dictionary<string, (Image, TextMeshProUGUI)>();
        float m_Alpha;
        bool m_Visible;
        int m_PointerCount;
        float m_LastPointerTime;

        public bool IsPointerOver => m_PointerCount > 0 || Time.time - m_LastPointerTime < 0.5f;

        void Awake()
        {
            Build();
        }

        void Build()
        {
            var items = new List<ItemDef>(GameDatabase.Discoverables);
            int rows = Mathf.CeilToInt(items.Count / (float)columns);
            float cellW = 118, cellH = 58, pad = 6;
            float w = columns * (cellW + pad) + pad + 20;
            float gridH = rows * (cellH + pad) + pad;
            float h = gridH + 70 + 110;

            m_Canvas = UIKit.WorldCanvas("HologramCanvas", transform, new Vector2(w, h), 0.00048f, true);
            m_Group = m_Canvas.gameObject.AddComponent<CanvasGroup>();
            var root = m_Canvas.transform;
            UIKit.Fill("Marco", root, new Color(0.35f, 0.85f, 1f, 0.9f));
            var bg = UIKit.Panel("Bg", root, new Color(0.03f, 0.07f, 0.13f, 0.94f), Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            bg.raycastTarget = true; // para mantenerlo visible mientras se apunta
            UIKit.OnHover(bg.gameObject, () => { m_PointerCount++; m_LastPointerTime = Time.time; }, () => { m_PointerCount = Mathf.Max(0, m_PointerCount - 1); m_LastPointerTime = Time.time; });

            m_Header = UIKit.Text("Header", root, "HOLOGRAMA DE DESCUBRIMIENTOS", 30, UIKit.Cyan, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -60), new Vector2(-10, -8));
            UIKit.AsTitle(m_Header);

            var grid = UIKit.Rect("Grid", root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -60 - gridH), new Vector2(-10, -60));
            for (int i = 0; i < items.Count; i++)
            {
                var def = items[i];
                int cx = i % columns, cy = i / columns;
                var cellBg = UIKit.Panel("Cell_" + def.id, grid, new Color(0.08f, 0.12f, 0.2f, 0.95f), new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(pad + cx * (cellW + pad), -(pad + cy * (cellH + pad)) - cellH),
                    new Vector2(pad + cx * (cellW + pad) + cellW, -(pad + cy * (cellH + pad))));
                cellBg.raycastTarget = true;
                var label = UIKit.Text("Label", cellBg.transform, "???", 15, new Color(0.55f, 0.62f, 0.72f), TextAlignmentOptions.Center,
                    Vector2.zero, Vector2.one, new Vector2(4, 2), new Vector2(-4, -2));
                label.enableAutoSizing = true; label.fontSizeMin = 10; label.fontSizeMax = 15; label.overflowMode = TextOverflowModes.Ellipsis;
                m_Cells[def.id] = (cellBg, label);
                var id = def.id;
                UIKit.OnHover(cellBg.gameObject, () => { m_PointerCount++; m_LastPointerTime = Time.time; ShowDetail(id); },
                    () => { m_PointerCount = Mathf.Max(0, m_PointerCount - 1); m_LastPointerTime = Time.time; });
            }

            var detailBg = UIKit.Panel("DetailBg", root, new Color(0.01f, 0.03f, 0.06f, 0.95f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(10, 10), new Vector2(-10, 110));
            m_Detail = UIKit.Text("Detail", detailBg.transform, "Apunta con la mano derecha a un objeto descubierto para ver su receta.", 21, new Color(0.92f, 0.97f, 1f),
                TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.one, new Vector2(12, 6), new Vector2(-12, -6));

            m_Canvas.gameObject.SetActive(false);
        }

        void ShowDetail(string id)
        {
            var def = GameDatabase.GetItem(id);
            var score = ScoreManager.Instance;
            bool known = score != null && score.IsDiscovered(id);
            if (!known)
            {
                m_Detail.text = "<b>???</b>  Todavía no descubres este objeto. Experimenta, Morty.";
                return;
            }
            m_Detail.text = $"<b>{def.name}</b>  =  {GameDatabase.RecipeText(id)}\n<size=82%><color=#9FD8FF>{def.description}</color></size>";
        }

        void Refresh()
        {
            var score = ScoreManager.Instance;
            int count = 0;
            foreach (var kv in m_Cells)
            {
                bool known = score != null && score.IsDiscovered(kv.Key);
                bool everKnown = SaveSystem.Data.collection.Contains(kv.Key);
                if (known) count++;
                kv.Value.label.text = known ? GameDatabase.NameOf(kv.Key) : (everKnown ? "?? (ya visto)" : "???");
                kv.Value.label.color = known ? Color.white : new Color(0.55f, 0.62f, 0.72f);
                kv.Value.bg.color = known ? new Color(0.1f, 0.42f, 0.24f, 0.97f) : new Color(0.08f, 0.12f, 0.2f, 0.95f);
            }
            m_Header.text = $"HOLOGRAMA DE DESCUBRIMIENTOS  {count}/{m_Cells.Count}";
        }

        public void SetVisible(bool visible)
        {
            if (visible && !m_Visible) Refresh();
            m_Visible = visible;
        }

        void LateUpdate()
        {
            m_Alpha = Mathf.MoveTowards(m_Alpha, m_Visible ? 1f : 0f, Time.deltaTime * 5f);
            bool active = m_Alpha > 0.01f;
            if (m_Canvas.gameObject.activeSelf != active) m_Canvas.gameObject.SetActive(active);
            if (!active) { m_PointerCount = 0; return; }
            m_Group.alpha = m_Alpha;
            m_Canvas.transform.localScale = Vector3.one * 0.00048f * Mathf.Lerp(0.6f, 1f, m_Alpha);

            // Flota ~20 cm sobre la muneca, mirando a la cabeza
            if (head != null)
            {
                var anchor = transform.parent != null ? transform.parent.position : transform.position;
                var toHead = head.position - anchor;
                toHead.y = 0;
                var pos = anchor + Vector3.up * 0.22f + toHead.normalized * -0.05f;
                m_Canvas.transform.position = pos;
                var dir = pos - head.position;
                if (dir.sqrMagnitude > 0.0001f) m_Canvas.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }
    }
}
