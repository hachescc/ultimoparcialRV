using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RickMorty
{
    public class ItemLabel : MonoBehaviour
    {
        public float showDistance = 1.1f;
        public float height = 0.12f;
        public float minSeparation = 9f;
        const int MaxVisible = 6;

        TextMeshPro m_Text;
        MixableItem m_Item;
        bool m_Visible;
        float m_Score;
        Vector3 m_Dir;

        static readonly List<ItemLabel> s_All = new List<ItemLabel>();
        static readonly List<ItemLabel> s_Candidates = new List<ItemLabel>();
        static readonly List<ItemLabel> s_Accepted = new List<ItemLabel>();
        static int s_Frame = -1;

        void OnEnable() { s_All.Add(this); }
        void OnDisable() { s_All.Remove(this); if (m_Text != null) m_Text.gameObject.SetActive(false); }

        void Start()
        {
            m_Item = GetComponent<MixableItem>();
            var go = new GameObject("Label");
            go.transform.SetParent(transform, false);
            m_Text = go.AddComponent<TextMeshPro>();
            var label = m_Item != null && m_Item.Def != null ? m_Item.Def.name : name;
            m_Text.text = "<mark=#07111DD9 padding=\"16,16,6,6\">" + label + "</mark>";
            m_Text.fontSize = 0.35f;
            m_Text.alignment = TextAlignmentOptions.Center;
            m_Text.color = new Color(1, 1, 1, 0.95f);
            m_Text.rectTransform.sizeDelta = new Vector2(0.6f, 0.1f);
            var col = GetComponent<Collider>();
            if (col != null) height = col.bounds.extents.y + 0.05f;
            m_Text.gameObject.SetActive(false);
        }

        static void Resolve(PlayerRig rig)
        {
            var head = rig.HeadPosition;
            var fwd = rig.head != null ? rig.head.forward : Vector3.forward;
            s_Candidates.Clear();
            foreach (var l in s_All)
            {
                if (l == null || l.m_Text == null) continue;
                l.m_Visible = false;
                if (l.m_Item != null && l.m_Item.Unavailable) continue;
                if (l.m_Item != null && l.m_Item.IsBase && l.m_Item.InSocket) continue;
                var p = l.transform.position + Vector3.up * l.height;
                var d = p - head;
                float dist = d.magnitude;
                if (dist > l.showDistance || dist < 0.01f) continue;
                l.m_Dir = d / dist;
                l.m_Score = Vector3.Angle(fwd, l.m_Dir) + dist * 10f;
                if (l.m_Item != null && l.m_Item.IsHeld) l.m_Score -= 1000f;
                s_Candidates.Add(l);
            }
            s_Candidates.Sort((a, b) => a.m_Score.CompareTo(b.m_Score));
            s_Accepted.Clear();
            foreach (var c in s_Candidates)
            {
                if (s_Accepted.Count >= MaxVisible) break;
                bool clash = false;
                foreach (var a in s_Accepted)
                    if (Vector3.Angle(a.m_Dir, c.m_Dir) < c.minSeparation) { clash = true; break; }
                if (clash) continue;
                c.m_Visible = true;
                s_Accepted.Add(c);
            }
        }

        void LateUpdate()
        {
            var rig = PlayerRig.Instance;
            if (m_Text == null || rig == null) return;
            if (s_Frame != Time.frameCount)
            {
                s_Frame = Time.frameCount;
                Resolve(rig);
            }
            if (m_Text.gameObject.activeSelf != m_Visible) m_Text.gameObject.SetActive(m_Visible);
            if (!m_Visible) return;
            var head = rig.HeadPosition;
            m_Text.transform.position = transform.position + Vector3.up * height;
            m_Text.transform.rotation = Quaternion.LookRotation(m_Text.transform.position - head, Vector3.up);
        }
    }
}
