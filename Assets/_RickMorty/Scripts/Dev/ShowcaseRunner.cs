#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace RickMorty.Dev
{
    /// <summary>
    /// SOLO EDITOR. Juega solo de principio a fin y saca capturas para la documentacion del equipo
    /// (menu "Rick y Morty/4. Recorrido con capturas"). Guarda JPGs + manifest.json en la carpeta de salida.
    /// </summary>
    public class ShowcaseRunner : MonoBehaviour
    {
        public string outDir;
        public Action onFinished;

        [Serializable] class ShotInfo { public string file, section, title, caption; }
        [Serializable] class Manifest { public List<ShotInfo> shots = new List<ShotInfo>(); public List<string> errors = new List<string>(); }

        readonly Manifest m_Manifest = new Manifest();
        string m_Section = "";
        int m_Index;
        GameManager GM => GameManager.Instance;

        // ---- Modo pelicula: en vez de JPGs, graba un MP4 continuo (ver FilmRecorder)
        public bool filmMode;
        public string filmPath;
        public float filmHold = 2.8f;
        public int filmTestParts;   // >0: solo graba los primeros N tramos (prueba rapida)
        FilmRecorder m_Film;

        void Start()
        {
            DontDestroyOnLoad(gameObject);
            Application.logMessageReceived += OnLog;
            ForceControllers();
            if (filmMode)
            {
                m_Film = gameObject.AddComponent<FilmRecorder>();
                m_Film.fxLayer = FxLayer;
                m_Film.Begin(filmPath);
            }
            StartCoroutine(Run());
        }

        /// <summary>Ejecuta una corrutina "aplanando" las anidadas, para que en modo pelicula cada plano
        /// encolado se reproduzca (vuelo + pausa) justo despues del paso que lo pidio.</summary>
        IEnumerator Drive(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var top = stack.Peek();
                bool moved;
                try { moved = top.MoveNext(); }
                catch (Exception e) { m_Manifest.errors.Add("Recorrido: " + e); stack.Pop(); continue; }
                while (m_Film != null && m_Film.HasQueued) yield return m_Film.PlayNext();
                if (!moved) { stack.Pop(); continue; }
                var cur = top.Current;
                if (cur is IEnumerator nested) { stack.Push(nested); continue; }
                yield return cur;
            }
            while (m_Film != null && m_Film.HasQueued) yield return m_Film.PlayNext();
        }

        bool FilmShot(Vector3 pos, Quaternion rot, float fov, bool useMain, string title)
        {
            if (!filmMode || m_Film == null) return false;
            m_Film.Queue(new FilmRecorder.FilmShot { pos = pos, rot = rot, fov = fov, useMain = useMain, section = m_Section, title = title, hold = filmHold });
            return true;
        }

        void OnDestroy() => Application.logMessageReceived -= OnLog;

        void OnLog(string msg, string stack, LogType type)
        {
            if ((type == LogType.Exception || type == LogType.Error) && !msg.Contains("Licens") && !msg.Contains("OpenXR"))
                m_Manifest.errors.Add(msg + "\n" + stack);
        }

        // ================================================================== recorrido

        IEnumerator Run()
        {
            Directory.CreateDirectory(outDir);
            var parts = new List<IEnumerator>
            {
                Intro(), Onboarding(), Tutorial(), RickLeaves(), FreePlayUI(), Mixing(), Interactions(),
                Creatures(), Events(), EasterEggs(), Exterior(), Gallery(), Finale(), GameOver()
            };
            if (filmMode) parts.RemoveAt(11);   // la galeria de objetos es de estudio, no del juego
            if (filmTestParts > 0) parts.RemoveRange(filmTestParts, parts.Count - filmTestParts);
            for (int i = 0; i < parts.Count; i++)
            {
                if (filmMode) Debug.Log($"[Showcase] parte {i + 1}/{parts.Count} (video {m_Film.Frames / 30} s)");
                yield return Drive(parts[i]);
            }
            if (m_Film != null) { yield return new WaitForSeconds(1.5f); m_Film.End(); }
            if (!filmMode) File.WriteAllText(Path.Combine(outDir, "manifest.json"), JsonUtility.ToJson(m_Manifest, true));
            Debug.Log($"[Showcase] {m_Manifest.shots.Count} capturas, {m_Manifest.errors.Count} errores → {outDir}");
            onFinished?.Invoke();
        }

        IEnumerator Safe(IEnumerator e) { return e; }

        IEnumerator Intro()
        {
            m_Section = "1. Inicio";
            yield return new WaitForSeconds(0.6f);
            ShotMain("Pantalla en negro", "El juego arranca en negro: se escucha a Rick revolviendo cosas y eructando."); yield return null; // film
            yield return new WaitForSeconds(4f);
            Shot("Morty despierta", "Morty despierta sobre la mesa del garaje. Rick golpea la mesa y empieza a darle instrucciones (subtitulos en el monitor y en el reloj).",
                new Vector3(-0.3f, 1.6f, 0.4f), new Vector3(1.2f, 1.3f, 1.6f)); yield return null; // film
            Shot("Monitor del garaje", "Monitor colgante: objetivo actual y subtitulos de Rick. Todo lo que dice Rick también sale aquí.",
                new Vector3(0, 1.95f, 1.95f), new Vector3(0, 2.22f, 2.95f), 60); yield return null; // film
            Shot("Vista general del garaje", "Garaje: mesa de mezclas al centro, estanteria con utileria, tablero de corcho, lavanderia a la izquierda, capibara en el rincon derecho.",
                new Vector3(-3.3f, 2.4f, -2.7f), new Vector3(0.5f, 0.8f, 2.5f), 78); yield return null; // film
            Comms()?.StopAll();
            yield return WaitState(GameState.Onboarding, 90f);
        }

        IEnumerator Onboarding()
        {
            m_Section = "2. Onboarding VR";
            yield return new WaitForSeconds(1.5f);
            var watch = WristWatch.Instance;
            float oldDot = watch.lookDot, oldDist = watch.lookMaxDistance;
            watch.lookDot = -2f; watch.gazeDot = -2f; watch.lookMaxDistance = 100f;   // simula mirar la muneca
            yield return new WaitForSeconds(1.2f);
            ShotWatch("Reloj Multiversal", "Paso 1: levantar la muñeca izquierda y mirar el Reloj Multiversal (temporizador, puntaje, subtitulos y objetivo)."); yield return null; // film
            ShotHologram("Holograma de descubrimientos (vacio)", "Al mirar el reloj se proyecta el holograma con la colección: lo no descubierto aparece como ???."); yield return null; // film
            watch.lookDot = 2f; watch.lookMaxDistance = oldDist;
            Comms()?.StopAll();
            yield return new WaitForSeconds(1.5f);
            var marker = GM.onboardingMarker;
            Shot("Caminar al círculo verde", "Paso 2: caminar con el joystick izquierdo (movimiento continuo) hasta el círculo verde.",
                new Vector3(0.6f, 1.7f, 1.6f), marker.position + Vector3.up * 0.3f); yield return null; // film
            GM.rig.TeleportTo(marker.position, Vector3.forward);
            Comms()?.StopAll();
            yield return new WaitForSeconds(2f);
            for (int i = 0; i < 4; i++)
            {
                GM.rig.TeleportTo(marker.position, Quaternion.Euler(0, 60 * (i + 1), 0) * Vector3.forward);
                yield return new WaitForSeconds(0.2f);
            }
            Comms()?.StopAll();
            yield return new WaitForSeconds(1.5f);
            GM.rig.TeleportTo(GM.playerStart.position, Vector3.forward);
            Shot("Agarrar un frasco", "Paso 4: agarrar un frasco con el grip. Liquidos a la izquierda, solidos a la derecha, cada uno en su soporte (socket XR).",
                new Vector3(0, 1.45f, 1.15f), new Vector3(0, 0.95f, 2.05f), 65); yield return null; // film
            {
                var mid = GM.rig.leftHand.position * 0.5f + GM.rig.rightHand.position * 0.5f;
                Shot("Manos del jugador", "Las manos reemplazan al modelo del control: abiertas normalmente y en puno al apretar el Grip o sostener algo.",
                    GM.rig.head.position + new Vector3(0, -0.1f, -0.15f), mid, 50); yield return null; // film
            }
            GameEvents.RaiseItemGrabbed("acido");
            Comms()?.StopAll();
            yield return WaitState(GameState.Tutorial, 30f);
        }

        IEnumerator Tutorial()
        {
            m_Section = "3. Tutorial de Rick (3 mezclas)";
            Comms()?.StopAll();
            yield return Mix(new[] { "mega_semilla", "meeseeks" }, "Misión 1: en el tazón", "Rick: 'Agarra el Extracto de Mega-Semilla y mezclalo con el polvo de Meeseeks'. Se meten al tazón y se presiona el botón rojo MEZCLAR.");
            yield return WaitItem("bola_meeseeks", 6f);
            yield return new WaitForSeconds(0.6f);
            ShotDesk("Resultado: Caja de Meeseeks", "Mezcla correcta: portal verde, burbujas, puntos y el objeto aparece en la bandeja de salida."); yield return null; // film
            ShotWatchBanner("Nuevo descubrimiento", "Cada objeto nuevo dispara un cartel de neon en la muñeca + sonido arcade + vibracion."); yield return null; // film
            Comms()?.StopAll();
            yield return new WaitForSeconds(1.5f);
            Comms()?.StopAll();
            var box = Item("bola_meeseeks");
            var meeseeks = MeeseeksNPC.Spawn(box.transform.position + new Vector3(0.6f, -0.9f, -0.5f), false);
            GameEvents.RaiseItemActivated("bola_meeseeks");
            yield return new WaitForSeconds(1f);
            Shot("Usar la caja (gatillo)", "Onboarding del gatillo: con la caja en la mano se aprieta el gatillo y aparece un Mr. Meeseeks que limpia la basura del piso.",
                new Vector3(-0.2f, 1.5f, -0.6f), meeseeks.transform.position + Vector3.up * 0.4f); yield return null; // film
            yield return new WaitForSeconds(3.5f);
            Comms()?.StopAll();
            if (box != null) box.gameObject.SetActive(false);   // el jugador se la llevo: que no estorbe en la mesa
            yield return Mix(new[] { "fleeb", "plumbus_polvo", "esporas" }, "Misión 2: tres ingredientes", "Rick: 'Agarra Polvo de Plumbus, Jugo de Fleeb y esas Esporas Parasitarias y juntalos'.");
            yield return WaitItem("plumbus", 6f);
            yield return new WaitForSeconds(0.6f);
            ShotDesk("Resultado: Plumbus", "Todo el mundo sabe para que sirve un Plumbus."); yield return null; // film
            Comms()?.StopAll();
            yield return new WaitForSeconds(1f);
            yield return Mix(new[] { "mega_semilla", "kalaxianos" }, null, null);
            yield return new WaitForSeconds(0.55f);
            ShotDesk("Advertencia: el tazón se pone rojo", "Si la mezcla va a fallar, el tazón se pone rojo y los controles vibran fuerte: ultima oportunidad para cubrirse."); yield return null; // film
            yield return new WaitForSeconds(0.25f);
            ShotMain("Explosión (vista del jugador)", "Mezcla invalida: destello blanco, explosión, humo negro, zumbido en los oidos y todo suena amortiguado 3 s. En la misión 3 Rick 'arrastra tu consciencia a otra dimension' (reapareces)."); yield return null; // film
            yield return new WaitForSeconds(0.4f);
            ShotDesk("Explosión (vista externa)", "La explosión lanza los objetos sueltos por el garaje; los frascos base regresan solos por un micro-portal."); yield return null; // film
            yield return new WaitForSeconds(4f);
            Comms()?.StopAll();
            yield return Mix(new[] { "plumbus", "kalaxianos" }, "Misión 3: Plumbus + Cristales", "Rick: 'Toma ese Plumbus que acabas de hacer y combinalo con los Cristales Kalaxianos'.");
            yield return WaitItem("espada_zord", 8f);
            yield return new WaitForSeconds(0.8f);
            ShotDesk("Resultado: Espada de Zord", "Espada negra y pesadisima: hay que moverla despacio o se queda atras (fisica de velocidad)."); yield return null; // film
            Comms()?.StopAll();
            yield return WaitState(GameState.RickLeaving, 20f);
        }

        IEnumerator RickLeaves()
        {
            m_Section = "4. Rick se va";
            yield return new WaitForSeconds(1.5f);
            Shot("Rick explica la misión", "Rick: 'Rompi mi pistola de portales... tienes que fabricarme una. Deje la puerta del garaje abierta. Si pisas fuera de la cinta, los francotiradores te volaran la cabeza'.",
                new Vector3(0.2f, 1.6f, 0.2f), GM.rickNPC.transform.position + Vector3.up * 1.4f, 60); yield return null; // film
            yield return new WaitForSeconds(4f);
            Shot("Se abre el garaje", "La puerta del garaje bascula hacia adentro y se ve la calle.",
                new Vector3(0.5f, 1.7f, -1.5f), new Vector3(4f, 1.2f, 0.6f)); yield return null; // film
            Comms()?.StopAll();
            yield return new WaitForSeconds(2.2f);
            Shot("Rick se va por un portal", "Rick camina hacia la puerta y desaparece por un portal verde.",
                new Vector3(1.5f, 1.6f, -1.5f), new Vector3(3.6f, 1f, 0.5f)); yield return null; // film
            yield return WaitState(GameState.FreePlay, 30f);
            Comms()?.StopAll();
        }

        IEnumerator FreePlayUI()
        {
            m_Section = "5. Juego libre: UI";
            yield return new WaitForSeconds(1.5f);
            Comms()?.StopAll();
            ShotWatch("Temporizador del FBI", "Arranca la cuenta regresiva del FBI (15 min). El reloj muestra tiempo, puntaje, objetivo y descubrimientos."); yield return null; // film
            Shot("Objetivo en el monitor", "Objetivo: fabricar la Pistola de Portales antes de que entre el FBI.",
                new Vector3(0, 1.95f, 1.95f), new Vector3(0, 2.22f, 2.95f), 60); yield return null; // film
            Shot("Tablero de corcho", "Notas de Rick: reglas basicas y pistas que se van revelando cuando no hay progreso.",
                new Vector3(-2.95f, 1.6f, 2.9f), new Vector3(-2.95f, 1.6f, 3.97f), 60); yield return null; // film
            PauseMenu.Instance.Open();
            yield return null;
            ShotMain("Menú de pausa", "Botón Menú del control izquierdo: pausa (congela el juego), ver controles, viñeta de confort, giro suave/por pasos, reiniciar y salir."); yield return null; // film
            PauseMenu.Instance.Close();
            ControlsPanel.Instance.Open();
            yield return null;
            ShotMain("Pantalla de controles", "Botón Y del control izquierdo (o tecla B en el PC) en cualquier momento: explica los controles del visor y del simulador de PC."); yield return null; // film
            ControlsPanel.Instance.Close();
        }

        IEnumerator Mixing()
        {
            m_Section = "6. Mezclas y sistemas";
            yield return Mix(new[] { "acido", "plumbus_polvo" }, "Mezcla libre", "En juego libre se experimenta: 36 recetas en total (tabla al final).");
            yield return WaitItem("pistola_laser", 6f);
            yield return new WaitForSeconds(0.6f);
            ShotDesk("Pistola Láser descubierta", "Las pistas de Rick y las notas del corcho guian hacia la receta final."); yield return null; // film
            Comms()?.StopAll();
            var watch = WristWatch.Instance;
            watch.lookDot = -2f; watch.gazeDot = -2f; watch.lookMaxDistance = 100f;
            yield return new WaitForSeconds(1.2f);
            ShotHologram("Holograma con descubrimientos", "La colección se llena; al apuntar a un objeto descubierto con la mano derecha se muestra su receta (no hay que memorizarlas)."); yield return null; // film
            watch.lookDot = 2f; watch.lookMaxDistance = 0.75f;
            // Anti-estupidez: un frasco cae al piso y vuelve por un portal
            var flask = Item("fleeb");
            ReleaseBase(flask);
            yield return null;
            flask.transform.position = new Vector3(-0.8f, 1.6f, 1.2f);
            flask.Body.position = flask.transform.position;
            yield return new WaitForSeconds(1.05f);
            Shot("Frasco al piso: micro-portal", "Sistema 'anti-estupidez': si un frasco base toca el piso, un micro-portal se lo traga...",
                new Vector3(-0.3f, 1.2f, 0.4f), new Vector3(-0.8f, 0.1f, 1.2f), 60); yield return null; // film
            yield return new WaitForSeconds(0.7f);
            Shot("...y vuelve a su soporte", "...y otro portal lo devuelve a su soporte en la mesa. Rick: 'Sabia que tus manos de mantequilla los tirarian al suelo'.",
                new Vector3(-0.8f, 1.4f, 1.3f), new Vector3(-0.8f, 0.95f, 2.0f), 60); yield return null; // film
            EnableSockets();
            // Agujero negro de basura
            var junk = ItemFactory.Spawn("vhs_balistica", GM.trashHole.transform.position + Vector3.up * 1.0f, Quaternion.identity);
            yield return new WaitForSeconds(0.55f);
            Shot("Agujero negro de basura", "Bote con un agujero negro: lo que se tira adentro se estira como espagueti y desaparece (mantiene el rendimiento).",
                GM.trashHole.transform.position + new Vector3(0.7f, 1.0f, -0.7f), GM.trashHole.transform.position + Vector3.up * 0.3f, 60); yield return null; // film
            Comms()?.StopAll();
            yield return new WaitForSeconds(1f);
        }

        IEnumerator Interactions()
        {
            m_Section = "7. Interacciones con objetos";
            Vector3 stage = new Vector3(7.5f, 1.0f, 0.5f);
            Vector3 camPos = stage + new Vector3(-0.5f, 0.25f, -0.55f);

            var laser = Hold("pistola_laser", stage + new Vector3(0, 0, -0.4f), Quaternion.LookRotation(new Vector3(1, 0, 1)));
            Call(laser.GetComponent<LaserGun>(), "Fire");
            Shot("Pistola laser", "Gatillo: rayo que rebota hasta 3 veces en las paredes. Le pega a criaturas, quema la marihuana cosmica...",
                camPos, stage + new Vector3(0.6f, 0, 0.3f)); yield return null; // film
            Destroy(laser.gameObject);

            var scar = Hold("scar", stage, Quaternion.LookRotation(new Vector3(1, 0, 1)));
            Call(scar.GetComponent<ScarRifle>(), "OnActivate");
            yield return new WaitForSeconds(0.6f);
            Shot("SCAR del Fortnite", "Rafaga rapida con dispersion horrible y numeros de dano amarillos flotando (9, 22, 9...).",
                camPos, stage + new Vector3(1.2f, 0, 1.2f)); yield return null; // film
            Call(scar.GetComponent<ScarRifle>(), "OnDeactivate");
            Destroy(scar.gameObject);

            var saber = Hold("sable_luz", stage, Quaternion.Euler(-50, 30, 0));
            Call(saber.GetComponent<Lightsaber>(), "OnActivate");
            yield return null;
            Shot("Sable de luz", "Gatillo: despliega la hoja azul con zumbido. Corta criaturas de un golpe y deja quemaduras en paredes y mesa.",
                camPos, stage + Vector3.up * 0.3f); yield return null; // film
            Destroy(saber.gameObject);

            var bang = Hold("portal_carton", stage, Quaternion.LookRotation(new Vector3(1, 0, 1)));
            Call(bang.GetComponent<CardboardPortalGun>(), "OnActivate");
            yield return new WaitForSeconds(0.25f);
            Shot("Portal Gun de cartón", "La version de cartón es una burla: saca una banderita 'BANG!' y mancha de verde. Es la base de la receta final.",
                stage + new Vector3(-0.35f, 0.15f, -0.35f), stage + new Vector3(0.1f, 0.05f, 0.1f), 55); yield return null; // film
            Comms()?.StopAll();
            Destroy(bang.gameObject);

            for (int i = 0; i < 4; i++)
                Hold("bloque_minecraft", new Vector3(6.5f + i * 0.32f, 0.3f + i * 0.32f, 2.0f), Quaternion.identity);
            Hold("token_fortnite", new Vector3(8.2f, 1.6f, 2.0f), Quaternion.identity);
            yield return new WaitForSeconds(0.3f);
            Shot("Bloques de Minecraft y Token", "El bloque ignora la gravedad: donde lo sueltas se queda (sirve para hacer escaleras). El token V-Bucks flota girando.",
                new Vector3(6.4f, 1.4f, 0.6f), new Vector3(7.3f, 0.9f, 2.0f)); yield return null; // film

            // Cuchillo clavado en el corcho
            var cork = CorkBoard.Instance.transform.position;
            var knife = ItemFactory.Spawn("kunai", cork + new Vector3(0.3f, 0.1f, -0.25f), Quaternion.LookRotation(Vector3.forward));
            knife.Body.linearVelocity = Vector3.forward * 6f;
            yield return new WaitForSeconds(0.4f);
            Shot("Cuchillos clavados", "Los cuchillos (CS:GO y kunai) lanzados con fuerza se clavan en la madera o en el corcho: dardos contra el tablero de Rick (+25 pts).",
                cork + new Vector3(0.5f, 0.1f, -0.9f), cork + new Vector3(0.2f, 0.05f, 0)); yield return null; // film

            // Guantelete
            var g = Hold("guantelete", new Vector3(0.3f, 1.25f, 1.45f), Quaternion.identity);
            for (int i = 0; i < 4; i++) ItemFactory.Spawn("papa_rellena", new Vector3(-0.5f + i * 0.25f, 1.0f, 1.62f), Quaternion.identity);
            yield return new WaitForSeconds(1f);
            Call(g.GetComponent<InfinityGauntlet>(), "OnActivate");
            yield return new WaitForSeconds(0.9f);
            Shot("Guantelete del Infinito", "Chasquido: exactamente la mitad de los objetos sueltos se hace polvo (los ingredientes base vuelven solos).",
                new Vector3(0.2f, 1.6f, 0.6f), new Vector3(0, 1.0f, 1.8f)); yield return null; // film
            Comms()?.StopAll();
            yield return new WaitForSeconds(0.8f);
            Destroy(g.gameObject);

            // Comida: efectos en la vision del jugador
            var fx = ScreenFX.Instance;
            fx.VHS(4f);
            Comms()?.Say("eat_szechuan", true);
            yield return new WaitForSeconds(1.2f);
            ShotMain("Salsa Szechuan (VHS)", "Comer = acercar a la boca. La salsa Szechuan pone un filtro VHS y Rick llora de felicidad."); yield return null; // film
            yield return new WaitForSeconds(3.5f);
            fx.Drunk(4f);
            Comms()?.Say("eat_aguardiente", true);
            yield return new WaitForSeconds(1.5f);
            ShotMain("Aguardiente / Papa rellena", "Borrachera: distorsion y aberracion cromatica."); yield return null; // film
            yield return new WaitForSeconds(3.5f);
            fx.PurpleHaze(4f);
            Comms()?.Say("weed_burn", true);
            yield return new WaitForSeconds(1.2f);
            ShotMain("Marihuana cosmica quemada", "Si se quema con el laser: humo morado y la gravedad baja a la mitad por 30 s."); yield return null; // film
            yield return new WaitForSeconds(3.5f);
            var back = GM.rig.FeetPosition;
            var spot = GM.quantumSpots[0];
            GM.rig.TeleportTo(spot.position, spot.forward);
            yield return new WaitForSeconds(0.2f);
            ShotMain("Empanada cuantica", "Morderla te teletransporta 3 s a otro punto del garaje (aquí: parado encima de la lavadora) y luego vuelves."); yield return null; // film
            GM.rig.TeleportTo(back, Vector3.forward);
            Comms()?.EnableDogMode(10f);
            Comms()?.Say("snowball_on", true);
            yield return new WaitForSeconds(0.5f);
            Shot("Casco de Snowball", "Con el casco puesto todo suena grave y los insultos de Rick por la radio se convierten en ladridos.",
                new Vector3(0, 1.95f, 1.95f), new Vector3(0, 2.22f, 2.95f), 60); yield return null; // film
            Comms()?.StopAll();
            Comms()?.EnableDogMode(0f);
            var cereal = ItemFactory.Spawn("cereal_eyeholes", new Vector3(3.2f, 1.5f, 2.9f), Quaternion.identity);
            var co = typeof(Consumable).GetMethod("CoEyeholes", BindingFlags.NonPublic | BindingFlags.Static);
            GM.StartCoroutine((IEnumerator)co.Invoke(null, new object[] { cereal.GetComponent<Consumable>() }));
            yield return new WaitForSeconds(0.4f);
            Shot("Cereal Eyeholes", "Si te lo acercas a la boca: alarma, el Hombre Agujero de Ojo aparece en la ventana, golpe con vibracion y te arrebata la caja.",
                new Vector3(3.3f, 1.55f, 2.55f), new Vector3(4.8f, 1.45f, 3.1f), 60); yield return null; // film
            Comms()?.StopAll();
            yield return new WaitForSeconds(2f);
        }

        IEnumerator Creatures()
        {
            m_Section = "8. Criaturas";
            GM.rig.TeleportTo(GM.playerStart.position, Vector3.back);
            GM.eventDirector.SteveInvasion();
            var steve = UnityEngine.Object.FindObjectsByType<FakeUncleSteve>(FindObjectsSortMode.None).FirstOrDefault();
            if (steve != null) steve.transform.position = new Vector3(0.5f, 0.3f, -1.4f);
            yield return new WaitForSeconds(1.5f);
            if (steve != null) steve.transform.position = new Vector3(0.3f, 0.25f, -0.6f);
            Shot("Falso Tío Steve", "Evento: un parasito de memoria entra al garaje y camina hacia ti pidiendo un abrazo. Hay que matarlo o bloquearlo con un escudo (dakimakura/peluche).",
                new Vector3(-0.2f, 1.1f, -0.2f), steve != null ? steve.transform.position + Vector3.up * 0.15f : new Vector3(0, 0.3f, -1f), 50); yield return null; // film
            Comms()?.StopAll();
            ScreenFX.Instance.Blind(3f);
            yield return new WaitForSeconds(1f);
            ShotMain("Abrazo del Tío Steve", "Si te toca: 5 s de 'falsos recuerdos hermosos' (pantalla rosa con corazones) y penalizacion."); yield return null; // film
            if (steve != null) Destroy(steve.gameObject);
            yield return new WaitForSeconds(2.5f);

            GM.eventDirector.ContrabandRaid();
            yield return new WaitForSeconds(1.2f);
            Shot("Meeseeks de contrabando", "Evento: un Meeseeks pirata (rojo, Kirkland) entra, se roba un ingrediente al azar, tose y explota.",
                new Vector3(3.9f, 1.3f, 0.3f), new Vector3(5.3f, 0.6f, 1.5f), 55); yield return null; // film
            Comms()?.StopAll();

            var caleno = Hold("rick_caleno", new Vector3(-0.62f, 0.985f, 1.62f), Quaternion.Euler(0, 200, 0));

            FX.FloatingText(caleno.transform.position + Vector3.up * 0.25f, "¡Vamos Ameeeerica!", new Color(1f, 0.2f, 0.2f), 0.4f, 3f, 0.05f);
            Shot("Rick Caleño + camiseta del América", "Rick Orientoso (caleno) rechaza el Token de Fortnite ('necesito lucas de verdad, ve') y con la camiseta del América se pone a cantar barras.",
                new Vector3(-0.5f, 1.18f, 1.25f), new Vector3(-0.62f, 1.05f, 1.62f), 45); yield return null; // film

            var pickle = Hold("pepino_mutante", new Vector3(4.6f, 1.85f, 9.85f), Quaternion.Euler(0, 180, 0));
            var win = UnityEngine.Object.FindObjectsByType<NeighborWindow>(FindObjectsSortMode.None).OrderBy(w => (w.transform.position - pickle.transform.position).sqrMagnitude).First();
            win.Hit(pickle.GetComponent<StickyThrowable>());
            yield return new WaitForSeconds(0.3f);
            Shot("Criaturas a la ventana del vecino", "El Pepino mutante y el Mini-Cronenberg se pegan a las ventanas del vecino y se escuchan gritos de terror.",
                new Vector3(4.2f, 1.8f, 7.6f), new Vector3(4.6f, 1.8f, 9.9f), 60); yield return null; // film

            var capy = UnityEngine.Object.FindObjectsByType<Capybara>(FindObjectsSortMode.None).FirstOrDefault();
            if (capy != null)
            {
                capy.GetComponent<Creature>().OnLaserBounce();
                Shot("Capibara cuántico", "Easter egg: si le disparas el laser le rebota en la cabeza. 'No molestes al roedor cuántico, Morty'.",
                    capy.transform.position + new Vector3(-1.1f, 0.7f, -0.6f), capy.transform.position + Vector3.up * 0.25f, 55); yield return null; // film
            }
            Comms()?.StopAll();

            var cron = Hold("mini_cronenberg", new Vector3(7.5f, 0.6f, 0.5f), Quaternion.identity);
            cron.Body.isKinematic = true;
            cron.GetComponent<Creature>().Kill(DamageType.Firearm);
            yield return new WaitForSeconds(0.55f);
            Shot("Muerte comica (se infla)", "Todas las criaturas mueren de un golpe: con armas de fuego se inflan como globo...",
                new Vector3(6.9f, 1.0f, -0.1f), new Vector3(7.5f, 0.6f, 0.5f), 55); yield return null; // film
            yield return new WaitForSeconds(0.35f);
            Shot("...y explotan", "...y explotan en particulas y un charco del color de su sangre (Rick comenta cada muerte).",
                new Vector3(6.75f, 1.05f, -0.35f), new Vector3(7.5f, 0.55f, 0.5f), 62); yield return null; // film
            Comms()?.StopAll();
            yield return new WaitForSeconds(1f);
        }

        IEnumerator Events()
        {
            m_Section = "9. Eventos inesperados y fallos";
            GM.rig.TeleportTo(GM.playerStart.position, Vector3.forward);
            var co = GM.eventDirector.CoBlackout();
            GM.StartCoroutine(co);
            yield return new WaitForSeconds(1f);
            Shot("Apagon", "Evento: el FBI corta la luz. Solo brillan los cristales y el tazón.",
                new Vector3(-3.3f, 2.4f, -2.7f), new Vector3(0.5f, 0.8f, 2.5f), 78); yield return null; // film
            GM.StopCoroutine(co);
            foreach (var l in GM.eventDirector.lights) l.SetBlackout(false);
            Comms()?.StopAll();
            GM.rig.TeleportTo(new Vector3(9f, 0, -2.0f), Vector3.back);
            yield return new WaitForSeconds(0.6f);
            ShotWatchBanner("Cerca de la cinta del FBI", "Al acercarse al perímetro el reloj avisa y Rick advierte de los francotiradores."); yield return null; // film
            Comms()?.StopAll();
            GM.rig.TeleportTo(new Vector3(9f, 0, -6f), Vector3.back);
            yield return new WaitForSeconds(0.25f);
            ShotMain("Francotirador del FBI", "Si sales de la cinta: disparo, pantalla roja, -100 puntos, -30 s y reapareces en el garaje."); yield return null; // film
            Comms()?.StopAll();
            yield return new WaitForSeconds(2f);
        }

        IEnumerator EasterEggs()
        {
            m_Section = "10. Easter eggs";
            (string obj, string name, string cap)[] eggs =
            {
                ("Casco Iron Man (easter egg)", "Casco de Iron Man", "Bajo la mesa, entre chatarra: le falta un ojo y tiene soldaduras."),
                ("Pokebola (easter egg)", "Pokébola desarmada", "Junto a los calcetines de la lavadora, con cables saliendo."),
                ("Calcomania Colombia (easter egg)", "Calcomanía de Colombia", "Desgastada, en el frente de la lavadora."),
                ("Cubo Prison Realm (easter egg)", "Cubo Prison Realm", "Usado como pisapapeles al lado de la caja TIME TRAVEL STUFF."),
                ("Esfera del Dragon (easter egg)", "Esfera del Dragón", "Flotando en un frasco de formol en el estante superior."),
                ("Figura Miku (easter egg)", "Figura de Miku", "Con parche cibernetico y tubos de ensayo en el pelo."),
                ("Llama Fortnite (easter egg)", "Llama de Fortnite", "Asomandose de una caja de plastico."),
                ("Capibara (easter egg)", "Capibara con gafas VR", "Durmiendo en un saco de dormir. Si le tiras algo mueve una oreja."),
            };
            foreach (var e in eggs)
            {
                var go = GameObject.Find(e.obj);
                if (go == null) { m_Manifest.errors.Add("No se encontro " + e.obj); continue; }
                var bounds = new Bounds(go.transform.position, Vector3.zero);
                foreach (var r in go.GetComponentsInChildren<Renderer>()) if (r.enabled) bounds.Encapsulate(r.bounds);
                var toRoom = new Vector3(0, 1.3f, 1.0f) - bounds.center; toRoom.y = 0; toRoom.Normalize();
                float dist = Mathf.Max(0.35f, bounds.extents.magnitude * 3.2f);
                var cam = bounds.center + toRoom * dist + Vector3.up * dist * 0.35f;
                Shot(e.name, e.cap + " Encontrarlo (agarrarlo/seleccionarlo) da +50 puntos.", cam, bounds.center, 50); yield return null; // film
            }
            yield return null;
        }

        IEnumerator Exterior()
        {
            m_Section = "11. Exterior";
            Shot("Casa Smith y garaje", "Al abrirse el garaje: casa de los Smith, entrada de concreto, aro de baloncesto.",
                new Vector3(12f, 3f, -6f), new Vector3(2f, 1.5f, 1.5f), 70); yield return null; // film
            Shot("Casa del vecino", "Casa rosada del vecino con valla blanca: sus ventanas son diana para lanzar criaturas.",
                new Vector3(8f, 1.8f, 2.5f), new Vector3(7.5f, 1.8f, 10f), 70); yield return null; // film
            Shot("Calle y perímetro del FBI", "La calle es la zona de pruebas; en los extremos barricadas, conos, camionetas del FBI y cinta policial.",
                new Vector3(18f, 3.5f, 20f), new Vector3(18f, 0.5f, 40f), 65); yield return null; // film
            Shot("Vista aerea", "Vista aerea del area jugable.",
                new Vector3(4f, 22f, -14f), new Vector3(6f, 0f, 6f), 70); yield return null; // film
            yield return null;
        }

        IEnumerator Gallery()
        {
            m_Section = "12. Galeria de objetos";
            var studio = new GameObject("Estudio");
            studio.transform.position = new Vector3(0, -800, 0);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(studio.transform, false);
            floor.transform.localScale = new Vector3(6, 0.1f, 6);
            floor.transform.localPosition = new Vector3(0, -0.6f, 0);
            floor.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(0.42f, 0.44f, 0.48f));
            foreach (var (p, i) in new[] { (new Vector3(-2, 2.5f, -2.5f), 2.2f), (new Vector3(2.5f, 1.5f, -1f), 1.2f), (new Vector3(0, 2f, 2.5f), 1.4f) })
            {
                var l = new GameObject("Luz").AddComponent<Light>();
                l.transform.SetParent(studio.transform, false);
                l.transform.localPosition = p; l.type = LightType.Point; l.range = 12; l.intensity = i; l.shadows = LightShadows.None;
            }
            var center = studio.transform.position;
            foreach (var def in GameDatabase.Items)
            {
                bool longShape = def.shape == "gun" || def.shape == "blade" || def.shape == "knife" || def.shape == "hilt";
                var m = ItemFactory.Spawn(def.id, center, Quaternion.Euler(0, longShape ? 119 : 210, 0));
                if (m == null) continue;
                m.Body.isKinematic = true;
                foreach (var b in m.GetComponents<MonoBehaviour>()) if (b is CreatureMover || b is Talker) b.enabled = false;
                yield return null;
                var bounds = new Bounds(m.transform.position, Vector3.zero);
                foreach (var r in m.GetComponentsInChildren<Renderer>()) if (r.enabled && !(r is ParticleSystemRenderer) && !(r is LineRenderer) && r.name != "Label") bounds.Encapsulate(r.bounds);
                float size = Mathf.Max(bounds.extents.magnitude, 0.03f);
                var dir = new Vector3(-0.55f, 0.35f, -1f).normalized;
                var camPos = bounds.center + dir * size * 2.6f;
                ShotRaw("item_" + def.id, def.name, GameDatabase.RecipeText(def.id), camPos, bounds.center, 40, 520, 520, new Color(0.3f, 0.32f, 0.36f)); yield return null; // film
                Destroy(m.gameObject);
                yield return null;
            }
            Destroy(studio);
        }

        IEnumerator Finale()
        {
            m_Section = "13. Final: la Pistola de Portales";
            Comms()?.StopAll();
            GM.rig.TeleportTo(GM.playerStart.position, Vector3.forward);
            var st = GM.finalStation;
            var gun = ItemFactory.Spawn("portal_carton", st.socket.transform.position, st.socket.transform.rotation);
            yield return new WaitForSeconds(1.2f);
            Shot("Portal Gun de cartón en el soporte", "Receta final: la Portal Gun de cartón se asegura en el Soporte de Ensamblaje (socket XR)...",
                new Vector3(0.1f, 1.4f, 1.15f), st.socket.transform.position, 50); yield return null; // film
            Comms()?.StopAll();
            yield return WaitItem("kalaxianos", 30f);   // un evento aleatorio (Meeseeks pirata) pudo habérselos llevado unos segundos
            var crystals = Item("kalaxianos");
            ReleaseBase(crystals);
            yield return null;
            crystals.transform.position = st.socket.transform.position + new Vector3(0.17f, -0.05f, 0.12f);
            crystals.Body.position = crystals.transform.position;
            var guaro = ItemFactory.Spawn("aguardiente", st.socket.transform.position + new Vector3(-0.17f, 0f, 0.12f), Quaternion.identity);
            yield return new WaitForSeconds(0.3f);
            Shot("Cristales + Aguardiente", "...se le acercan los Cristales Kalaxianos (energia) y el Aguardiente (estabilizador cuántico)...",
                new Vector3(0.1f, 1.4f, 1.15f), st.socket.transform.position, 50); yield return null; // film
            yield return new WaitForSeconds(1.2f);
            Shot("Transmutacion", "...el cartón arde con chispas verdes y levita...",
                new Vector3(0.1f, 1.4f, 1.0f), st.socket.transform.position + Vector3.up * 0.25f, 50); yield return null; // film
            yield return new WaitForSeconds(0.85f);
            ShotMain("Destello", "...destello cegador..."); yield return null; // film
            yield return new WaitForSeconds(1.5f);
            Shot("Pistola de Portales real", "...y aparece la Pistola de Portales real (+1000 puntos).",
                new Vector3(0.1f, 1.45f, 1.1f), st.socket.transform.position + Vector3.up * 0.35f, 45); yield return null; // film
            EnableSockets();
            Comms()?.StopAll();
            GM.OnRealPortalGunGrabbed(null);
            yield return new WaitForSeconds(2f);
            Shot("Holograma de Rick", "Al agarrarla, Rick aparece como holograma desde el brazo robotico: 'Rompiste al menos cinco leyes de la termodinamica... ¡Salta, Morty!'",
                new Vector3(0.3f, 1.4f, 1.0f), new Vector3(0, 1.1f, 2.45f), 55); yield return null; // film
            GM.ShootPortal(new Vector3(-3.99f, 1.2f, 1.6f), Vector3.right);
            yield return new WaitForSeconds(1.5f);
            Shot("Portal abierto", "Se dispara contra una pared y se abre el portal verde...",
                new Vector3(-1.5f, 1.6f, 1.0f), new Vector3(-3.99f, 1.2f, 1.6f), 65); yield return null; // film
            Comms()?.StopAll();
            GM.EnterFinalPortal();
            yield return new WaitForSeconds(0.9f);
            ShotMain("Cruzar el portal", "...se cruza caminando: fundido a blanco y suena el acorde final."); yield return null; // film
            yield return new WaitForSeconds(5.5f);
            var spot = GM.endScreen.playerSpot;
            ShotCanvas("ResultsCanvas", 1.25f, "Resultados (victoria)", "Cierre: puntaje, rango, tiempo, descubrimientos, explosiones, criaturas, easter eggs y las últimas 3 partidas (el historial se guarda en JSON por dentro; el jugador no ve ese detalle). Botones para jugar de nuevo o salir."); yield return null; // film
            ShotCanvas("CreditsCanvas", 2.6f, "Creditos", "Creditos flotando lentamente en el vacio del espacio."); yield return null; // film
        }

        IEnumerator GameOver()
        {
            m_Section = "14. Otro final: se acaba el tiempo";
            if (m_Film != null) m_Film.Paused = true;   // la recarga de escena es tiempo muerto: no se graba
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            yield return new WaitForSeconds(0.5f);
            ForceControllers();
            yield return new WaitForSeconds(1.5f);
            GM.DebugSkipTutorial();
            float t = 0;
            while (GM.State != GameState.FreePlay && t < 90f) { Comms()?.StopAll(); t += 0.5f; yield return new WaitForSeconds(0.5f); }
            GM.DebugGameOver();
            if (m_Film != null) { m_Film.FollowPlayer(); m_Film.Paused = false; }
            yield return new WaitForSeconds(2.5f);
            ShotMain("Atrapado por el FBI", "Esto es OTRA partida, no la continuación de la anterior: si el temporizador del FBI llega a 0 antes de fabricar la Pistola de Portales, suenan las sirenas, Rick se burla y la pantalla se pone roja."); yield return null; // film
            yield return new WaitForSeconds(4.5f);
            var spot = GM.endScreen.playerSpot;
            ShotCanvas("ResultsCanvas", 1.25f, "Resultados (derrota)", "Pantalla de resultados de esa partida perdida: ATRAPADO POR EL FBI, con el puntaje y rango obtenidos."); yield return null; // film
        }

        // ================================================================== helpers de juego

        const int FxLayer = 4;
        static RickComms Comms() => RickComms.Instance;

        /// <summary>Sin controles conectados el XR Input Modality Manager apaga los controles (y el reloj). Para las capturas los forzamos.</summary>
        static void ForceControllers()
        {
            var rig = PlayerRig.Instance;
            if (rig == null) return;
            foreach (var mb in rig.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                var tn = mb.GetType().Name;
                if (tn == "XRInputModalityManager") mb.enabled = false;
                // capturas limpias: sin lineas ni reticulas de los rayos (dejaban discos verdes sobre la mesa)
                if (tn == "XRInteractorLineVisual" || tn == "XRInteractorReticleVisual" || tn == "CurveVisualController") mb.enabled = false;
            }
            foreach (var lr in rig.GetComponentsInChildren<LineRenderer>(true)) lr.enabled = false;
            foreach (var h in new[] { rig.leftHand, rig.rightHand })
            {
                if (h == null) continue;
                h.gameObject.SetActive(true);
                h.localPosition = h == rig.leftHand ? new Vector3(-0.2f, -0.75f, 0.1f) : new Vector3(0.2f, -0.75f, 0.1f);
                h.localRotation = Quaternion.Euler(20, 0, 0);
            }
            var cam = Camera.main;
            if (cam != null)
                foreach (Transform t in cam.transform)
                    if (t.name == "ScreenFade" || t.name == "ScreenOverlay" || t.name == "OverlayText" || t.name == "ComfortVignette")
                        foreach (var ch in t.GetComponentsInChildren<Transform>(true)) ch.gameObject.layer = FxLayer;
            if (WristWatch.Instance != null) WristWatch.Instance.lookDot = 2f;   // holograma solo cuando lo pedimos
        }

        IEnumerator WaitState(GameState s, float timeout)
        {
            float t = 0;
            while (GM.State != s && t < timeout) { t += Time.deltaTime; yield return null; }
            if (GM.State != s) m_Manifest.errors.Add($"Timeout esperando {s} (estado {GM.State})");
        }

        IEnumerator WaitItem(string id, float timeout)
        {
            float t = 0;
            while (MixableItem.FindById(id) == null && t < timeout) { t += Time.deltaTime; yield return null; }
        }

        static MixableItem Item(string id) => MixableItem.FindById(id);

        static readonly List<IngredientSocket> s_Off = new List<IngredientSocket>();

        static void ReleaseBase(MixableItem m)
        {
            if (m == null) return;
            if (m.Base != null && m.Base.homeSocket != null) { m.Base.homeSocket.socketActive = false; s_Off.Add(m.Base.homeSocket); }
            m.ForceRelease();
        }

        static void EnableSockets()
        {
            foreach (var s in s_Off) if (s != null) s.socketActive = true;
            s_Off.Clear();
        }

        IEnumerator Mix(string[] ids, string title, string caption)
        {
            var items = ids.Select(Item).ToList();
            foreach (var m in items) ReleaseBase(m);
            yield return new WaitForSeconds(0.4f);
            var bowl = MixingBowl.Instance;
            var c = bowl.transform.TransformPoint(bowl.innerCenter);
            for (int k = 0; k < items.Count; k++)
            {
                var m = items[k];
                if (m == null) continue;
                m.Body.isKinematic = false;
                var p = c + new Vector3((k - (items.Count - 1) / 2f) * 0.14f, 0.06f, 0);
                m.transform.position = p; m.Body.position = p; m.Body.linearVelocity = Vector3.zero;
            }
            yield return new WaitForSeconds(0.9f);
            if (title != null) ShotDesk(title, caption); yield return null; // film
            UnityEngine.Object.FindAnyObjectByType<MixButton>().Press(null);
            yield return new WaitForSeconds(0.1f);
            yield return new WaitForSeconds(1.6f);
            EnableSockets();
        }

        static MixableItem Hold(string id, Vector3 pos, Quaternion rot)
        {
            var m = ItemFactory.Spawn(id, pos, rot);
            if (m != null) { m.Body.isKinematic = true; }
            return m;
        }

        static void Call(object target, string method)
        {
            if (target == null) return;
            var mi = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            mi?.Invoke(target, null);
        }

        // ================================================================== capturas

        void ShotCanvas(string name, float dist, string title, string caption)
        {
            var c = GM.endScreen.transform.Find(name);
            if (c == null) { m_Manifest.errors.Add("No hay " + name); return; }
            ShotRot(title, caption, c.position - c.forward * dist, c.rotation, 60);
        }

        void ShotDesk(string title, string caption) =>
            Shot(title, caption, new Vector3(0, 1.5f, 1.05f), new Vector3(0, 0.92f, 1.95f), 55);

        void ShotWatch(string title, string caption)
        {
            var w = WristWatch.Instance.transform;
            var pos = w.position + w.up * 0.14f - w.forward * 0.03f;
            ShotRot(title, caption, pos, Quaternion.LookRotation(-w.up, w.forward), 50);
        }

        void ShotWatchBanner(string title, string caption)
        {
            var banner = WristWatch.Instance.transform.Find("NeonBanner");
            if (banner == null || !banner.gameObject.activeSelf) { ShotWatch(title, caption); return; }
            ShotRot(title, caption, banner.position - banner.forward * 0.45f + Vector3.down * 0.08f, Quaternion.LookRotation(banner.forward), 55);
        }

        void ShotHologram(string title, string caption)
        {
            var canvas = WristWatch.Instance.hologram.transform.Find("HologramCanvas");
            if (canvas == null || !canvas.gameObject.activeInHierarchy) { ShotWatch(title, caption); return; }
            ShotRot(title, caption, canvas.position - canvas.forward * 0.62f, canvas.rotation, 55);
        }

        void Shot(string title, string caption, Vector3 pos, Vector3 lookAt, float fov = 70f) =>
            ShotRot(title, caption, pos, Quaternion.LookRotation(lookAt - pos), fov);

        void ShotRot(string title, string caption, Vector3 pos, Quaternion rot, float fov)
        {
            if (FilmShot(pos, rot, fov, false, title)) return;
            var file = $"{++m_Index:000}";
            Render(file, pos, rot, fov, 1280, 720, null, false);
            m_Manifest.shots.Add(new ShotInfo { file = file + ".jpg", section = m_Section, title = title, caption = caption });
        }

        void ShotRaw(string file, string title, string caption, Vector3 pos, Vector3 lookAt, float fov, int w, int h, Color bg)
        {
            Render(file, pos, Quaternion.LookRotation(lookAt - pos), fov, w, h, bg, false);
            m_Manifest.shots.Add(new ShotInfo { file = file + ".jpg", section = m_Section, title = title, caption = caption });
        }

        /// <summary>Vista exacta del jugador (incluye fundidos, overlays y post-proceso de la camara XR).</summary>
        void ShotMain(string title, string caption)
        {
            if (FilmShot(Vector3.zero, Quaternion.identity, 80f, true, title)) return;
            var file = $"{++m_Index:000}";
            Render(file, Vector3.zero, Quaternion.identity, 0, 1280, 720, null, true);
            m_Manifest.shots.Add(new ShotInfo { file = file + ".jpg", section = m_Section, title = title, caption = caption });
        }

        void Render(string file, Vector3 pos, Quaternion rot, float fov, int w, int h, Color? bg, bool useMain)
        {
            Camera cam;
            GameObject temp = null;
            if (useMain)
            {
                cam = Camera.main;
            }
            else
            {
                temp = new GameObject("ShowcaseCam");
                cam = temp.AddComponent<Camera>();
                temp.transform.SetPositionAndRotation(pos, rot);
                cam.fieldOfView = fov;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 400f;
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                if (bg.HasValue) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = bg.Value; }
                cam.cullingMask = ~(1 << FxLayer);
            }
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            try
            {
                float oldFov = cam.fieldOfView;
                if (useMain) cam.fieldOfView = 80f;
                var req = new RenderPipeline.StandardRequest { destination = rt };
                if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
                else { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
                if (useMain) cam.fieldOfView = oldFov;
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(outDir, file + ".jpg"), tex.EncodeToJPG(88));
                Destroy(tex);
            }
            catch (Exception e) { m_Manifest.errors.Add("Captura " + file + ": " + e.Message); }
            finally
            {
                rt.Release();
                Destroy(rt);
                if (temp != null) Destroy(temp);
            }
        }
    }
}
#endif
