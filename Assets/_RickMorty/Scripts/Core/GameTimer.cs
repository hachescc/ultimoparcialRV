using UnityEngine;

namespace RickMorty
{
    public class GameTimer : MonoBehaviour
    {
        public static GameTimer Instance { get; private set; }

        public float duration = 480f;

        public float Remaining { get; private set; }
        public bool Running { get; private set; }
        public bool Paused { get; set; }

        public event System.Action Expired;
        public event System.Action<int> MinuteWarning;

        int m_LastWarnedMinute = -1;
        int m_LastTick = -1;

        void Awake()
        {
            Instance = this;
            Remaining = duration;
        }

        public void StartTimer()
        {
            Remaining = duration;
            Running = true;
            m_LastWarnedMinute = Mathf.CeilToInt(duration / 60f) + 1;
        }

        public void Stop() => Running = false;

        public void AddTime(float seconds)
        {
            Remaining = Mathf.Max(0f, Remaining + seconds);
        }

        void Update()
        {
            if (!Running || Paused) return;
            Remaining -= Time.deltaTime;

            int minute = Mathf.CeilToInt(Remaining / 60f);
            if (minute < m_LastWarnedMinute)
            {
                m_LastWarnedMinute = minute;
                if (minute == 5 || minute == 3 || minute == 1) MinuteWarning?.Invoke(minute);
            }

            if (Remaining > 0f && Remaining <= 10f)
            {
                int sec = Mathf.CeilToInt(Remaining);
                if (sec != m_LastTick) { m_LastTick = sec; AudioFX.Play2D("tick", 0.7f, 1f + (10 - sec) * 0.06f); }
            }

            if (Remaining <= 0f)
            {
                Remaining = 0f;
                Running = false;
                Expired?.Invoke();
            }
        }

        public static string Format(float seconds)
        {
            seconds = Mathf.Max(0, seconds);
            int m = (int)(seconds / 60f);
            int s = (int)(seconds % 60f);
            return $"{m:00}:{s:00}";
        }
    }
}
