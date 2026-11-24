using System.Collections.Generic;
using UnityEngine;

namespace RickMorty
{
    public class Talker : ItemBehaviour
    {
        static readonly List<Talker> s_All = new List<Talker>();
        public string lineId = "toallin_idle";
        public float interval = 45f;
        float m_Next;

        void OnEnable() => s_All.Add(this);
        void OnDisable() => s_All.Remove(this);

        protected override void Start()
        {
            base.Start();
            m_Next = Time.time + 3f;
        }

        void Update()
        {
            if (Time.time < m_Next) return;
            var rig = PlayerRig.Instance;
            if (rig == null || (rig.HeadPosition - transform.position).sqrMagnitude > 9f) { m_Next = Time.time + 2f; return; }
            m_Next = Time.time + interval;
            Speak(lineId);
        }

        void Speak(string id)
        {
            var line = GameDatabase.GetLine(id);
            if (line == null) return;
            RickComms.Instance?.SayText(line.PickText(), line.speaker);
            FX.FloatingText(transform.position + Vector3.up * 0.2f, "!", Color.white, 0.6f, 1f, 0.2f);
        }

        public static void SayAll(string id)
        {
            if (s_All.Count > 0) s_All[0].Speak(id);
        }
    }
}
