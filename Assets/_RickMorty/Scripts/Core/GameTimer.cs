using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Cuenta regresiva del FBI. Cuando llega a 0 los agentes entran al garaje (fallo).
    /// </summary>
    public class GameTimer : MonoBehaviour
    {
        public static GameTimer Instance { get; private set; }

        [Tooltip("Segundos de juego libre antes de que el FBI entre")]
        public float duration = 600f;   // 10 min (15 se hacia largo segun las pruebas del equipo)

        public float Remaining { get; private set; }
        public bool Running { get; private set; }
        public bool Paused { get; set; }
        public float Elapsed { get; private set; }

        public event System.Action Expired;
        public event System.Action<int> MinuteWarning; // minutos restantes

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
            m_LastWarnedMinute = Mathf.CeilToInt(duration / 60f);
        }

        public void Stop() => Running = false;

        public void AddTime(float seconds)
        {
            Remaining = Mathf.Max(0f, Remaining + seconds);
        }

        void Update()
        {
            if (!Running || Paused) return;
            Elapsed += Time.deltaTime;
            Remaining -= Time.deltaTime;

            int minute = Mathf.CeilToInt(Remaining / 60f);
            if (minute < m_LastWarnedMinute)
            {
                m_LastWarnedMinute = minute;
                if (minute == 5 || minute == 3 || minute == 1) MinuteWarning?.Invoke(minute);
            }

            // ultimos 10 segundos: un "tic" por segundo (cada vez mas agudo) para que se sienta el final
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
