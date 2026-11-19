using System.Collections.Generic;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Puntaje y estadisticas verificables de la partida. Escucha el bus de eventos.
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
        public int SuccessfulMixes { get; private set; }
        public int Penalties { get; private set; }
        public readonly HashSet<string> Discovered = new HashSet<string>();
        public readonly HashSet<string> EasterEggs = new HashSet<string>();
        public readonly List<string> Log = new List<string>();

        public event System.Action Changed;

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

        void Add(int pts, string reason)
        {
            Score = Mathf.Max(0, Score + pts);
            Log.Add($"{(pts >= 0 ? "+" : "")}{pts}  {reason}");
            Changed?.Invoke();
        }

        void OnMixSucceeded(RecipeDef r, bool first)
        {
            SuccessfulMixes++;
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
            Penalties++;
            Add(-Mathf.Abs(pts), reason);
        }

        // Cada bono puntua como maximo 3 veces por partida (antes se podia "farmear" puntaje sin limite
        // clavando el cuchillo en el corcho, tirando cosas a la ventana del vecino, repitiendo mezclas...)
        const int MaxSameBonus = 3;
        readonly System.Collections.Generic.Dictionary<string, int> m_BonusCount = new System.Collections.Generic.Dictionary<string, int>();

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
