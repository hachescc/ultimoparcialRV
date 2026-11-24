using System;
using System.Collections.Generic;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Puntaje y estadisticas verificables de la partida. Escucha el bus de eventos (GameEvents).
    /// Lo usan: GameManager (resultado y pistas), tazon, soporte final, reloj, holograma, monitor y pantalla final.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Puntos")]
        public int pointsPerDiscovery = 100;
        public int pointsPerRepeatMix = 10;
        public int pointsPerEasterEgg = 50;
        public int explosionPenalty = 50;

        public int Score { get; private set; }
        public int Explosions { get; private set; }
        public int Kills { get; private set; }
        public readonly HashSet<string> Discovered = new HashSet<string>();
        public readonly HashSet<string> EasterEggs = new HashSet<string>();

        public event Action Changed;

        void Awake()
        {
            Instance = this;
        }

        void OnEnable()
        {
            GameEvents.MixSucceeded += OnMixSucceeded;
            GameEvents.MixFailed += OnMixFailed;
            GameEvents.CreatureKilled += OnKilled;
            GameEvents.Penalty += OnPenalty;
            GameEvents.Bonus += OnBonus;
            GameEvents.EasterEggFound += OnEgg;
        }

        void OnDisable()
        {
            GameEvents.MixSucceeded -= OnMixSucceeded;
            GameEvents.MixFailed -= OnMixFailed;
            GameEvents.CreatureKilled -= OnKilled;
            GameEvents.Penalty -= OnPenalty;
            GameEvents.Bonus -= OnBonus;
            GameEvents.EasterEggFound -= OnEgg;
        }

        public bool IsDiscovered(string id) => Discovered.Contains(id);

        /// <summary>Registra un objeto como descubierto. Devuelve true si es nuevo.</summary>
        public bool RegisterDiscovery(string id)
        {
            if (!Discovered.Add(id)) return false;
            SaveSystem.AddToCollection(id);
            var def = GameDatabase.GetItem(id);
            if (def != null) GameEvents.RaiseItemDiscovered(def);
            return true;
        }

        /// <summary>Suma (o resta) puntos. reason describe el motivo para quien lea el codigo.</summary>
        void Add(int pts, string reason)
        {
            Score = Mathf.Max(0, Score + pts);
            Changed?.Invoke();
            Popup(pts);
        }

        /// <summary>Numero flotante (+100 / -50) para que el jugador VEA por que cambio su puntaje.</summary>
        static void Popup(int pts)
        {
            if (pts == 0) return;
            var gm = GameManager.Instance;
            if (gm != null && gm.IsFinished) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 pos;
            var bowl = gm != null ? gm.mixingBowl : null;
            if (bowl != null && (bowl.transform.position - cam.transform.position).sqrMagnitude < 2.5f * 2.5f)
                pos = bowl.transform.position + Vector3.up * 0.5f;
            else
            {
                var f = cam.transform.forward; f.y = 0;
                if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
                pos = cam.transform.position + f.normalized * 1.0f + Vector3.down * 0.12f;
            }
            FX.FloatingText(pos, (pts > 0 ? "+" : "") + pts + " pts", pts > 0 ? new Color(0.45f, 1f, 0.45f) : new Color(1f, 0.35f, 0.3f), 0.9f, 1.8f, 0.25f);
        }

        // Repetir la misma mezcla puntua como maximo 3 veces (antes daba +10 sin limite: se podia "farmear").
        readonly Dictionary<string, int> m_RepeatCount = new Dictionary<string, int>();

        void OnMixSucceeded(RecipeDef r, bool first)
        {
            if (!first)
            {
                m_RepeatCount.TryGetValue(r.result ?? "", out int n);
                m_RepeatCount[r.result ?? ""] = n + 1;
                if (n >= MaxSameBonus) { Changed?.Invoke(); return; }
            }
            Add(first ? pointsPerDiscovery : pointsPerRepeatMix, (first ? "Descubrimiento: " : "Mezcla: ") + GameDatabase.NameOf(r.result));
        }

        void OnMixFailed(string reason)
        {
            Explosions++;
            Add(-explosionPenalty, "Explosión: " + reason);
        }

        void OnKilled(Creature c, bool accidental)
        {
            Kills++;
            if (c != null && c.moralPenalty > 0) Add(-c.moralPenalty, "Penalización moral: " + c.displayName);
            else Changed?.Invoke();
        }

        void OnPenalty(string reason, int pts)
        {
            Add(-Mathf.Abs(pts), reason);
        }

        // Cada bono puntua como maximo 3 veces por partida (antes se podia "farmear" puntaje sin limite
        // clavando el cuchillo en el corcho, tirando cosas a la ventana del vecino, repitiendo mezclas...)
        const int MaxSameBonus = 3;
        readonly Dictionary<string, int> m_BonusCount = new Dictionary<string, int>();

        void OnBonus(string reason, int pts)
        {
            string key = reason ?? "";
            m_BonusCount.TryGetValue(key, out int n);
            if (n >= MaxSameBonus) return;
            m_BonusCount[key] = n + 1;
            Add(Mathf.Abs(pts), reason);
        }

        void OnEgg(string id)
        {
            if (EasterEggs.Add(id)) Add(pointsPerEasterEgg, "Easter egg: " + id);
        }

        public static string RankFor(int score, bool victory)
        {
            if (!victory) return score >= 800 ? "C" : "D";
            if (score >= 3500) return "S";
            if (score >= 2500) return "A";
            if (score >= 1600) return "B";
            return "C";
        }
    }
}
