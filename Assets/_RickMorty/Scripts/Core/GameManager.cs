using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Flujo principal del juego (maquina de estados):
    /// Intro → Onboarding (controles) → Tutorial (3 mezclas de Rick) → Rick se va →
    /// Juego libre con temporizador del FBI y eventos inesperados → Ensamblaje de la Pistola de
    /// Portales → Portal final → Resultados y creditos.  Fallo: el FBI entra si se acaba el tiempo.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Sistemas")]
        public PlayerRig rig;
        public RickComms comms;
        public RickMonitor monitor;
        public CorkBoard corkBoard;
        public MixingBowl mixingBowl;
        public FinalAssemblyStation finalStation;
        public BlackHoleTrash trashHole;
        public GameTimer timer;
        public ScoreManager score;
        public EventDirector eventDirector;
        public EndScreen endScreen;

        [Header("Escena")]
        public GarageDoor garageDoor;
        public RickNPC rickNPC;
        public RickHologram rickHologram;
        public Transform playerStart;
        public Transform onboardingMarker;
        public Transform ceilingPortalPoint;
        public Transform eyeholesSpot;
        public Transform[] quantumSpots;
        public AudioSource music;

        [Header("Pruebas")]
        [Tooltip("Saltar intro y tutorial (directo al juego libre)")]
        public bool skipTutorial;

        public GameState State { get; private set; } = GameState.Intro;

        class Mission
        {
            public string target, objective, failLine, successLine, ensureItem;
            public bool respawnOnFail;
        }

        readonly List<Mission> m_Missions = new List<Mission>
        {
            new Mission { target = "bola_meeseeks", objective = "Mete <b>Extracto de Mega-Semilla</b> + <b>Esencia Meeseeks</b> en el tazón y presiona el botón rojo <b>MEZCLAR</b>.", failLine = "mission1_fail", successLine = "mission1_success" },
            new Mission { target = "plumbus", objective = "Mezcla <b>Polvo de Plumbus</b> + <b>Jugo de Fleeb</b> + <b>Esporas Parasitarias</b>.", failLine = "mission2_fail", successLine = "mission2_success" },
            new Mission { target = "espada_zord", objective = "Combina el <b>Plumbus</b> que hiciste con los <b>Cristales Kalaxianos</b>.", failLine = "mission3_fail", successLine = "mission3_success", ensureItem = "plumbus", respawnOnFail = true },
        };

        int m_MissionIndex = -1;
        int m_MissionResult; // 0 = esperando, 1 = exito, -1 = fallo
        bool m_SkipRequested;
        bool m_GunCreated;
        GameObject m_EndPortal;
        float m_LastProgressTime;
        float m_NextHintTime;
        int m_LastDiscoveryCount;
        bool m_Finished;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Burnable.ResetGravity();
            GameDatabase.Load();
            SaveSystem.Load();
        }

        void OnEnable()
        {
            if (timer != null)
            {
                timer.Expired += OnTimerExpired;
                timer.MinuteWarning += OnMinuteWarning;
            }
        }

        void OnDisable()
        {
            if (timer != null)
            {
                timer.Expired -= OnTimerExpired;
                timer.MinuteWarning -= OnMinuteWarning;
            }
        }

        void Start()
        {
            if (music != null && music.clip == null) music.clip = Resources.Load<AudioClip>("Audio/Music/bgm");
            StartCoroutine(CoMain());
        }

        void SetState(GameState s)
        {
            // una vez terminado (victoria o FBI) nada puede devolver el juego a otro estado
            if (m_Finished && s != GameState.Ending && s != GameState.GameOver) return;
            State = s;
            GameEvents.RaiseStateChanged(s);
        }

        void SetObjective(string text) => monitor?.SetObjective(text);

        // =====================================================================
        //  Flujo principal
        // =====================================================================

        IEnumerator CoMain()
        {
            yield return null;
            if (rig != null && playerStart != null) rig.TeleportTo(playerStart.position, playerStart.forward);

            if (!skipTutorial)
            {
                yield return CoIntro();
                if (!m_SkipRequested) yield return CoOnboarding();
                if (!m_SkipRequested) yield return CoTutorial();
            }
            yield return CoRickLeaves();
            yield return CoFreePlay();
        }

        IEnumerator CoIntro()
        {
            SetState(GameState.Intro);
            SetObjective("...");
            var fx = ScreenFX.Instance;
            fx?.SetFade(Color.black, 1f);
            if (comms != null && rickNPC != null) comms.SpeakerOverride = rickNPC.mouth;
            AudioFX.Play("rummage", rickNPC != null ? rickNPC.transform.position : Vector3.zero, 1f);
            yield return new WaitForSeconds(1.5f);
            AudioFX.Play("burp", rickNPC != null ? rickNPC.transform.position : Vector3.zero, 0.8f);
            yield return new WaitForSeconds(0.8f);
            rickNPC?.SlamTable();
            fx?.Fade(Color.black, 1f, 0f, 2.5f);
            comms?.Say("intro_wake");
            yield return Wait(comms.WaitUntilIdle());
        }

        IEnumerator CoOnboarding()
        {
            SetState(GameState.Onboarding);

            // 1. Mirar el reloj
            comms?.Say("onb_watch");
            SetObjective("Levanta tu <b>muñeca izquierda</b> y mira el Reloj Multiversal.");
            yield return WaitUntilOrSkip(() => WristWatch.Instance != null && WristWatch.Instance.LookingDuration > 0.8f, 45f);
            if (m_SkipRequested) yield break;
            Good();

            // 2. Caminar
            comms?.Say("onb_move");
            SetObjective("Usa el <b>joystick izquierdo</b> para caminar hasta el <b>círculo verde</b>.");
            if (onboardingMarker != null) onboardingMarker.gameObject.SetActive(true);
            yield return WaitUntilOrSkip(() =>
            {
                if (onboardingMarker == null || rig == null) return true;
                var d = rig.HeadPosition - onboardingMarker.position; d.y = 0;
                return d.magnitude < 0.55f;
            }, 60f);
            if (onboardingMarker != null) onboardingMarker.gameObject.SetActive(false);
            if (m_SkipRequested) yield break;
            Good();

            // 3. Girar
            comms?.Say("onb_turn");
            SetObjective("Usa el <b>joystick derecho</b> para girar. Mira a tu alrededor.");
            float accumulated = 0f;
            float lastYaw = rig != null && rig.head != null ? rig.head.eulerAngles.y : 0f;
            yield return WaitUntilOrSkip(() =>
            {
                if (rig == null || rig.head == null) return true;
                float yaw = rig.head.eulerAngles.y;
                accumulated += Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw));
                lastYaw = yaw;
                return accumulated > 120f;
            }, 45f);
            if (m_SkipRequested) yield break;
            Good();

            // 4. Agarrar
            comms?.Say("onb_grab");
            SetObjective("Vuelve a la mesa y agarra un frasco con el botón <b>Grip</b> (gatillo del dedo medio).");
            bool grabbed = false;
            Action<string> onGrab = id => { var d = GameDatabase.GetItem(id); if (d != null && d.IsBase) grabbed = true; };
            GameEvents.ItemGrabbed += onGrab;
            yield return WaitUntilOrSkip(() => grabbed, 90f);
            GameEvents.ItemGrabbed -= onGrab;
            if (m_SkipRequested) yield break;
            Good();
            yield return Wait(comms.WaitUntilIdle());
        }

        IEnumerator CoTutorial()
        {
            SetState(GameState.Tutorial);
            comms?.Say("mission1_intro");

            for (m_MissionIndex = 0; m_MissionIndex < m_Missions.Count; m_MissionIndex++)
            {
                var mission = m_Missions[m_MissionIndex];
                SetObjective(mission.objective);
                while (true)
                {
                    EnsureMissionItem(mission);
                    m_MissionResult = 0;
                    // revisa cada 2 s que el objeto de la mision siga existiendo (si lo botan a la basura
                    // o se lo lleva un Meeseeks, la mision 3 quedaba imposible)
                    float nextEnsure = Time.time + 2f;
                    yield return WaitUntilOrSkip(() =>
                    {
                        if (Time.time > nextEnsure) { nextEnsure = Time.time + 2f; EnsureMissionItem(mission); }
                        return m_MissionResult != 0;
                    });
                    if (m_SkipRequested) yield break;
                    if (m_MissionResult > 0) break;

                    comms?.Say(mission.failLine, true);
                    if (mission.respawnOnFail)
                    {
                        // "Te arrastre a una dimension identica": pantalla blanca y reaparicion
                        ScreenFX.Instance?.SetFade(Color.white, 1f);
                        yield return new WaitForSeconds(1.2f);
                        if (rig != null && playerStart != null) rig.TeleportTo(playerStart.position, playerStart.forward);
                        ScreenFX.Instance?.Fade(Color.white, 1f, 0f, 2f);
                    }
                    yield return new WaitForSeconds(1.5f);
                }

                GameEvents.RaiseBonus("Misión de Rick completada", 150);
                comms?.Say(mission.successLine, true);

                if (m_MissionIndex == 0)
                {
                    // Onboarding del gatillo: usar la caja de Meeseeks recien creada
                    yield return Wait(comms.WaitUntilIdle());
                    comms?.Say("onb_trigger");
                    SetObjective("Agarra la <b>Caja de Meeseeks</b> y presiona el <b>gatillo</b> (índice) para usarla.");
                    bool used = false;
                    Action<string> onAct = id => { if (id == "bola_meeseeks") used = true; };
                    GameEvents.ItemActivated += onAct;
                    float timeout = Time.time + 60f;
                    yield return WaitUntilOrSkip(() => used || Time.time > timeout);
                    GameEvents.ItemActivated -= onAct;
                    if (m_SkipRequested) yield break;
                    yield return new WaitForSeconds(2.5f);
                    comms?.Say("mission2_reminder");
                }
            }
            yield return Wait(comms.WaitUntilIdle());
        }

        IEnumerator CoRickLeaves()
        {
            SetState(GameState.RickLeaving);
            ScreenFX.Instance?.SetFade(Color.black, 0f);
            m_SkipRequested = false;
            comms?.StopAll();
            comms?.Say("rick_leaving");
            SetObjective("Escucha a Rick...");
            yield return new WaitForSeconds(4f);
            if (garageDoor != null) garageDoor.Open();
            yield return Wait(comms.WaitUntilIdle());
            if (rickNPC != null && rickNPC.gameObject.activeSelf) yield return rickNPC.CoLeave();
            if (comms != null) comms.SpeakerOverride = null;
        }

        IEnumerator CoFreePlay()
        {
            SetState(GameState.FreePlay);
            SetObjective("Fabrica la <b>PISTOLA DE PORTALES</b> antes de que el FBI entre. Experimenta combinaciones. Mira tu muñeca para ver tus descubrimientos.");
            corkBoard?.AddNote("META: Pistola de Portales\n= Portal Gun de cartón\n+ Cristales + ???");
            timer?.StartTimer();
            eventDirector?.Begin();
            m_LastProgressTime = Time.time;
            m_NextHintTime = Time.time + 90f;
            m_LastDiscoveryCount = score != null ? score.Discovered.Count : 0;
            if (music != null && music.clip != null && !music.isPlaying) music.Play();

            while (State == GameState.FreePlay || State == GameState.PortalGunReady)
            {
                if (score != null && score.Discovered.Count != m_LastDiscoveryCount)
                {
                    m_LastDiscoveryCount = score.Discovered.Count;
                    m_LastProgressTime = Time.time;
                    m_NextHintTime = Mathf.Max(m_NextHintTime, Time.time + 60f);
                }
                if (State == GameState.FreePlay && Time.time > m_NextHintTime)
                {
                    m_NextHintTime = Time.time + 100f;
                    GiveHint();
                }
                yield return new WaitForSeconds(1f);
            }
        }

        // =====================================================================
        //  Mezclas
        // =====================================================================

        /// <summary>El tazon pregunta si la mezcla esta permitida (en el tutorial solo vale la pedida).</summary>
        public bool EvaluateMix(RecipeDef recipe, List<string> ids)
        {
            if (State == GameState.Tutorial && m_MissionIndex >= 0 && m_MissionIndex < m_Missions.Count)
                return recipe != null && recipe.result == m_Missions[m_MissionIndex].target;
            return true;
        }

        public void OnMixResolved(RecipeDef recipe, MixableItem result)
        {
            if (State == GameState.Tutorial && m_MissionIndex >= 0 && m_MissionIndex < m_Missions.Count)
            {
                m_MissionResult = recipe != null && recipe.result == m_Missions[m_MissionIndex].target ? 1 : -1;
                return;
            }
            if (recipe == null)
            {
                comms?.Say("explosion_random", true);
                return;
            }
            // Comentario de Rick si existe una linea "found_<id>"
            var line = GameDatabase.GetLine("found_" + recipe.result);
            if (line != null) comms?.Say("found_" + recipe.result);
            RevealProgressNotes(recipe.result);
        }

        void RevealProgressNotes(string id)
        {
            if (corkBoard == null) return;
            if (id == "pistola_laser") corkBoard.AddNote("Pistola laser +\nPolvo de Plumbus = ?");
            if (id == "portal_carton") corkBoard.AddNote("Carton + Cristales +\nun liquido que ENFRIE\n(¿alcohol?)");
            if (id == "aguardiente") corkBoard.AddNote("Aguardiente:\nestabilizador cuántico");
        }

        void EnsureMissionItem(Mission m)
        {
            if (string.IsNullOrEmpty(m.ensureItem)) return;
            if (MixableItem.FindById(m.ensureItem) != null) return;
            var p = mixingBowl != null && mixingBowl.outputPoint != null ? mixingBowl.outputPoint.position : Vector3.up;
            FX.Portal(p + Vector3.up * 0.3f, Quaternion.LookRotation(Vector3.down), 0.15f, 1f);
            ItemFactory.Spawn(m.ensureItem, p + Vector3.up * 0.15f, Quaternion.identity);
        }

        // =====================================================================
        //  Pistas
        // =====================================================================

        public string CurrentHintText()
        {
            var s = score;
            if (s == null) return null;
            if (!s.IsDiscovered("pistola_laser")) return "La Pistola Láser sale de Ácido Galaxiano + Polvo de Plumbus.";
            if (!s.IsDiscovered("portal_carton")) return "Pistola Láser + Polvo de Plumbus = una Portal Gun de cartón.";
            if (!s.IsDiscovered("aguardiente")) return "Necesitas un liquido estabilizador: Jugo de Fleeb + Ácido Galaxiano.";
            return "Pon la Portal Gun de cartón en el soporte de ensamblaje y acercale los Cristales Kalaxianos y el Aguardiente.";
        }

        void GiveHint()
        {
            var hint = CurrentHintText();
            if (string.IsNullOrEmpty(hint)) return;
            comms?.Say("hint_intro");
            comms?.SayText(hint);
            corkBoard?.AddNote("PISTA: " + hint);
        }

        // =====================================================================
        //  Final
        // =====================================================================

        public void StopMusic()
        {
            if (music != null) music.Stop();
        }

        public void OnPortalGunCreated(MixableItem gun)
        {
            if (m_GunCreated || m_Finished) return;   // si el FBI ya entro durante la transmutacion, no revive la partida
            m_GunCreated = true;
            SetState(GameState.PortalGunReady);
            GameEvents.RaiseBonus("¡Pistola de Portales creada!", 1000);
            comms?.Say("final_created", true);
            SetObjective("¡La lograste! <b>Agarra la Pistola de Portales</b>.");
        }

        public void OnRealPortalGunGrabbed(RealPortalGun gun)
        {
            StartCoroutine(CoFinalDialogue());
        }

        IEnumerator CoFinalDialogue()
        {
            StopMusic();
            AudioFX.Play("static", rig != null ? rig.HeadPosition : Vector3.zero, 0.8f);
            if (rickHologram != null)
            {
                rickHologram.Show();
                if (comms != null) comms.SpeakerOverride = rickHologram.transform;
            }
            SetObjective("Dispara la pistola (gatillo) contra una <b>pared</b> y <b>cruza el portal</b>.");
            comms?.Say("final_1", true);
            comms?.Say("final_2");
            comms?.Say("final_3");
            yield return Wait(comms.WaitUntilIdle());
            if (rickHologram != null) rickHologram.Hide();
            if (comms != null) comms.SpeakerOverride = null;
        }

        public void ShootPortal(Vector3 point, Vector3 normal)
        {
            if (State != GameState.PortalGunReady)
            {
                FX.Portal(point + normal * 0.03f, Quaternion.LookRotation(-normal), 0.3f, 2f);
                return;
            }
            if (m_EndPortal != null) Destroy(m_EndPortal);

            bool wall = Mathf.Abs(normal.y) < 0.5f;
            var pos = point + normal * 0.05f;
            if (wall)
            {
                float floor = rig != null && rig.origin != null ? rig.origin.transform.position.y : 0f;
                pos.y = Mathf.Clamp(pos.y, floor + 1.0f, floor + 1.4f);
            }
            var rot = wall ? Quaternion.LookRotation(-normal, Vector3.up) : Quaternion.LookRotation(-normal);
            var pv = FX.Portal(pos, rot, 0.9f, 0f);
            m_EndPortal = pv.gameObject;
            var trigger = m_EndPortal.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.6f, 2.2f, 0.8f);
            m_EndPortal.AddComponent<EndingPortal>();
            AudioFX.AttachLoop(m_EndPortal, "hum", 0.5f, 8f, 0.5f);
            PlayerRig.Instance?.Haptic(Hand.Both, 1f, 0.6f);
            comms?.Say("portal_open");
        }

        public void EnterFinalPortal()
        {
            if (m_Finished) return;
            m_Finished = true;
            StartCoroutine(CoEnding(true));
        }

        void OnTimerExpired()
        {
            if (m_Finished) return;
            m_Finished = true;
            StartCoroutine(CoEnding(false));
        }

        void OnMinuteWarning(int minutes)
        {
            AudioFX.Play2D("siren", 0.5f);
            comms?.Say(minutes == 5 ? "fbi_5min" : minutes == 3 ? "fbi_3min" : "fbi_1min");
            WristWatch.Instance?.Notify($"¡QUEDAN {minutes} MIN ANTES DE QUE ENTRE EL FBI!", UIKit.Danger, 4f);
        }

        IEnumerator CoEnding(bool victory)
        {
            SetState(victory ? GameState.Ending : GameState.GameOver);
            timer?.Stop();
            eventDirector?.Stop();
            PauseMenu.Instance?.Close();
            // fuera criaturas y Meeseeks: el Tio Steve podia abrazarte encima de la pantalla de resultados
            foreach (var c in FindObjectsByType<CreatureMover>(FindObjectsSortMode.None))
                if (c != null && c.GetComponent<RickCaleno>() == null) Destroy(c.gameObject);
            foreach (var m in FindObjectsByType<MeeseeksNPC>(FindObjectsSortMode.None))
                if (m != null) Destroy(m.gameObject);
            StopMusic();
            var fx = ScreenFX.Instance;

            if (victory)
            {
                fx?.Fade(Color.white, 0f, 1f, 0.8f);
                AudioFX.Play2D("chord", 0.9f);
            }
            else
            {
                AudioFX.Play2D("siren", 0.8f);
                comms?.Say("fbi_caught", true);
                fx?.Fade(new Color(0.3f, 0f, 0f), 0f, 1f, 3f);
                yield return new WaitForSeconds(2f);
            }
            yield return new WaitForSeconds(1.2f);
            comms?.StopAll();

            var run = BuildRunRecord(victory);
            SaveSystem.RecordRun(run);

            if (endScreen != null && rig != null)
            {
                var spot = endScreen.playerSpot != null ? endScreen.playerSpot : endScreen.transform;
                rig.TeleportTo(spot.position, spot.forward);
                endScreen.Show(victory, run, "");
            }
            Burnable.ResetGravity();
            yield return new WaitForSeconds(0.5f);
            fx?.Fade(victory ? Color.white : new Color(0.3f, 0f, 0f), 1f, 0f, 2.5f);
            comms?.Say(victory ? "ending_victory" : "ending_defeat");
        }

        RunRecord BuildRunRecord(bool victory)
        {
            int finalScore = score != null ? score.Score : 0;
            if (victory && timer != null) finalScore += Mathf.RoundToInt(timer.Remaining * 2f); // bono por tiempo restante
            return new RunRecord
            {
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                result = victory ? "Victoria" : "Atrapado por el FBI",
                score = finalScore,
                rank = ScoreManager.RankFor(finalScore, victory),
                seconds = Time.timeSinceLevelLoad,
                discoveries = score != null ? score.Discovered.Count : 0,
                explosions = score != null ? score.Explosions : 0,
                kills = score != null ? score.Kills : 0,
                easterEggs = score != null ? score.EasterEggs.Count : 0,
            };
        }

        // =====================================================================
        //  Utilidades / pruebas
        // =====================================================================

        void Good()
        {
            AudioFX.Play2D("arcade_win", 0.5f, 1.2f);
            rig?.Haptic(Hand.Both, 0.4f, 0.15f);
            comms?.Say("onb_good");
        }

        IEnumerator WaitUntilOrSkip(Func<bool> condition, float timeout = 0f)
        {
            float end = timeout > 0f ? Time.time + timeout : float.MaxValue;
            while (!m_SkipRequested && !condition())
            {
                if (Time.time > end)
                {
                    comms?.Say("onb_timeout");
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator Wait(IEnumerator inner)
        {
            while (!m_SkipRequested && inner.MoveNext()) yield return inner.Current;
        }

        public void DebugSkipTutorial()
        {
            if (State == GameState.Intro || State == GameState.Onboarding || State == GameState.Tutorial)
            {
                m_SkipRequested = true;
                ScreenFX.Instance?.SetFade(Color.black, 0f);
                if (onboardingMarker != null) onboardingMarker.gameObject.SetActive(false);
                Debug.Log("[RickMorty] Tutorial saltado (F1)");
            }
        }

        public void DebugSpawnFinalKit()
        {
            var p = mixingBowl != null && mixingBowl.outputPoint != null ? mixingBowl.outputPoint.position : Vector3.up;
            ItemFactory.Spawn("portal_carton", p + Vector3.up * 0.2f, Quaternion.identity);
            ItemFactory.Spawn("aguardiente", p + Vector3.up * 0.4f + Vector3.right * 0.1f, Quaternion.identity);
            ScoreManager.Instance?.RegisterDiscovery("portal_carton");
            ScoreManager.Instance?.RegisterDiscovery("aguardiente");
        }

        public void DebugGameOver()
        {
            if (timer != null) { timer.StartTimer(); timer.AddTime(-timer.duration + 1f); }
        }
    }
}
