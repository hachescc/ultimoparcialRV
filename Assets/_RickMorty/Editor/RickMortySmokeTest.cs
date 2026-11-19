using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RickMorty.EditorTools
{
    /// <summary>
    /// Prueba de humo automatica: entra a Play, recorre el flujo principal (mezclas validas e
    /// invalidas, todos los objetos, eventos inesperados, final) y guarda capturas + errores.
    /// Uso: menu "Rick y Morty/3. Prueba automatica" o en batch con -executeMethod ...BatchRun
    /// </summary>
    [InitializeOnLoad]
    public static class RickMortySmokeTest
    {
        const string KeyActive = "RM_Smoke_Active";
        const string KeyBatch = "RM_Smoke_Batch";
        const string KeyOut = "RM_Smoke_Out";

        static double s_Start;
        static int s_Step;
        static readonly List<string> s_Errors = new List<string>();
        static readonly List<string> s_Notes = new List<string>();

        static RickMortySmokeTest()
        {
            if (!SessionState.GetBool(KeyActive, false)) return;
            EditorApplication.playModeStateChanged += OnPlayMode;
            if (EditorApplication.isPlaying) Begin();
        }

        [MenuItem("Rick y Morty/3. Prueba automatica (smoke test)", priority = 3)]
        public static void RunFromMenu() => Run(false, Path.GetFullPath("Temp/RickMortySmoke"));

        public static void BatchRun()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-smokeOut");
            var outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.GetFullPath("Temp/RickMortySmoke");
            Run(true, outDir);
        }

        static void Run(bool batch, string outDir)
        {
            Directory.CreateDirectory(outDir);
            SessionState.SetBool(KeyActive, true);
            SessionState.SetBool(KeyBatch, batch);
            SessionState.SetString(KeyOut, outDir);
            EditorSceneManager.OpenScene(RickMortySceneBuilder.ScenePath);
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayMode(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode) Begin();
        }

        static void Begin()
        {
            s_Start = EditorApplication.timeSinceStartup;
            s_Step = 0;
            s_Errors.Clear();
            s_Notes.Clear();
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                if (msg.Contains("Licens") || msg.Contains("OpenXR") || msg.Contains("XR") && msg.Contains("runtime")) { s_Notes.Add("[ignorado] " + msg); return; }
                s_Errors.Add($"[{type}] {msg}\n{stack}");
            }
        }

        static double T => EditorApplication.timeSinceStartup - s_Start;

        static void Note(string s)
        {
            s_Notes.Add($"{T:0.0}s  {s}");
            Debug.Log("[Smoke] " + s);
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var gm = GameManager.Instance;
            try
            {
                switch (s_Step)
                {
                    case 0:
                        if (T < 4) return;
                        Note("Estado inicial: " + (gm != null ? gm.State.ToString() : "SIN GAMEMANAGER"));
                        Shot("01_intro_vista_jugador", null);
                        s_Step++;
                        break;
                    case 1:
                        if (T < 6) return;
                        Note("Onboarding/tutorial en curso: " + gm.State + " | objetivo: " + (RickMonitor.Instance != null ? RickMonitor.Instance.Objective : "-"));
                        gm.DebugSkipTutorial();
                        s_Step++;
                        break;
                    case 2:
                        if (gm.State != GameState.FreePlay) { if (T > 240) { Note("TIMEOUT esperando FreePlay: " + gm.State); s_Step = 99; } return; }
                        Note("FreePlay alcanzado en " + T.ToString("0.0") + "s. Timer: " + GameTimer.Instance.Remaining.ToString("0"));
                        Shot("02_garaje_desde_mesa", new Pose(new Vector3(0, 1.6f, 0.6f), Quaternion.Euler(15, 0, 0)));
                        Shot("03_garaje_panoramica", new Pose(new Vector3(-3.2f, 2.3f, -2.6f), Quaternion.Euler(25, 40, 0)));
                        Shot("04_exterior", new Pose(new Vector3(12f, 3f, -6f), Quaternion.Euler(15, -50, 0)));
                        s_Step++;
                        break;
                    case 3:
                        // Mezcla valida: Acido + Polvo de Plumbus = Pistola laser
                        Release("acido", "plumbus_polvo");
                        s_Wait = T + 0.5;
                        s_Step = 30;
                        break;
                    case 4:
                        if (T < s_Wait) return;
                        Note("Contenido del tazon: " + string.Join(", ", MixingBowl.Instance.Contents().Select(x => x.itemId)));
                        MixingBowl.Instance.Mix();
                        s_Wait = T + 3;
                        s_Step++;
                        break;
                    case 5:
                        if (T < s_Wait) return;
                        Note("Pistola laser descubierta: " + ScoreManager.Instance.IsDiscovered("pistola_laser") + " | existe: " + (MixableItem.FindById("pistola_laser") != null) + " | puntaje " + ScoreManager.Instance.Score);
                        Shot("05_mesa_resultado", new Pose(new Vector3(0, 1.45f, 1.0f), Quaternion.Euler(40, 0, 0)));
                        // Mezcla invalida: explosion
                        Release("mega_semilla", "kalaxianos");
                        s_Wait = T + 0.5;
                        s_Step = 31;
                        break;
                    case 6:
                        if (T < s_Wait) return;
                        Note("Contenido del tazon: " + string.Join(", ", MixingBowl.Instance.Contents().Select(x => x.itemId)));
                        MixingBowl.Instance.Mix();
                        s_Wait = T + 0.9;
                        s_Step++;
                        break;
                    case 7:
                        if (T < s_Wait) return;
                        Shot("06_explosion", new Pose(new Vector3(0, 1.5f, 0.9f), Quaternion.Euler(30, 0, 0)));
                        s_Wait = T + 3;
                        s_Step++;
                        break;
                    case 8:
                        if (T < s_Wait) return;
                        Note("Contenido del tazon antes de mezclar se registra arriba. Explosiones: " + ScoreManager.Instance.Explosions + " | frascos base disponibles: " + MixableItem.All.Count(m => m.IsBase && !m.Unavailable) + "/8");
                        foreach (var sk in s_Disabled) if (sk != null) sk.socketActive = true;
                        // Aparecer todos los objetos descubribles para ejecutar sus comportamientos
                        int i = 0;
                        foreach (var def in GameDatabase.Discoverables)
                        {
                            var pos = new Vector3(6f + (i % 8) * 0.8f, 1.0f, -0.8f + (i / 8) * 0.8f);
                            ItemFactory.Spawn(def.id, pos, Quaternion.identity);
                            i++;
                        }
                        Note("Objetos aparecidos: " + i);
                        s_Wait = T + 3;
                        s_Step++;
                        break;
                    case 9:
                        if (T < s_Wait) return;
                        Shot("07_todos_los_objetos", new Pose(new Vector3(8.8f, 2.4f, -3.2f), Quaternion.Euler(35, 0, 0)));
                        var d = gm.eventDirector;
                        d.SteveInvasion();
                        d.ContrabandRaid();
                        d.Quake();
                        gm.StartCoroutine(d.CoBlackout());
                        Note("Eventos inesperados disparados");
                        s_Wait = T + 4;
                        s_Step++;
                        break;
                    case 10:
                        if (T < s_Wait) return;
                        Shot("08_apagon", new Pose(new Vector3(0, 1.6f, 0.6f), Quaternion.Euler(15, 0, 0)));
                        // Matar criaturas con un "disparo"
                        int killed = 0;
                        foreach (var c in UnityEngine.Object.FindObjectsByType<Creature>(FindObjectsSortMode.None))
                            if (c.displayName != "Capibara") { c.Kill(DamageType.Firearm); killed++; }
                        Note("Criaturas eliminadas en prueba: " + killed);
                        // Final: crear la pistola, agarrarla, abrir portal
                        var gun = ItemFactory.Spawn("pistola_portales", new Vector3(0.4f, 1.1f, 1.66f), Quaternion.identity);
                        ScoreManager.Instance.RegisterDiscovery("pistola_portales");
                        gm.OnPortalGunCreated(gun);
                        gm.OnRealPortalGunGrabbed(gun.GetComponent<RealPortalGun>());
                        s_Wait = T + 2;
                        s_Step++;
                        break;
                    case 11:
                        if (T < s_Wait) return;
                        Note("Estado final previo: " + gm.State);
                        gm.ShootPortal(new Vector3(0, 1.2f, 3.99f), Vector3.back);
                        s_Wait = T + 1.5;
                        s_Step++;
                        break;
                    case 12:
                        if (T < s_Wait) return;
                        Shot("09_holograma_y_portal", new Pose(new Vector3(0, 1.6f, 0.4f), Quaternion.Euler(10, 0, 0)));
                        gm.EnterFinalPortal();
                        s_Wait = T + 6;
                        s_Step++;
                        break;
                    case 13:
                        if (T < s_Wait) return;
                        Note("Estado tras portal: " + gm.State + " | partidas guardadas: " + SaveSystem.Data.runs.Count + " | mejor: " + SaveSystem.Data.bestScore);
                        var end = gm.endScreen.playerSpot;
                        Shot("10_resultados", new Pose(end.position + Vector3.up * 1.6f, Quaternion.LookRotation(end.forward)));
                        s_Step = 99;
                        break;
                    case 30:
                        if (T < s_Wait) return;
                        PlaceInBowl("acido", "plumbus_polvo");
                        s_Wait = T + 1.0;
                        s_Step = 4;
                        break;
                    case 31:
                        if (T < s_Wait) return;
                        PlaceInBowl("mega_semilla", "kalaxianos");
                        s_Wait = T + 1.0;
                        s_Step = 6;
                        break;
                    case 99:
                        Finish();
                        break;
                }
            }
            catch (Exception e)
            {
                s_Errors.Add("[SmokeTest] Excepcion en paso " + s_Step + ": " + e);
                s_Step = 99;
            }
        }

        static double s_Wait;

        static readonly List<IngredientSocket> s_Disabled = new List<IngredientSocket>();

        static void Release(params string[] ids)
        {
            foreach (var id in ids)
            {
                var m = MixableItem.FindById(id);
                if (m == null) continue;
                if (m.Base != null && m.Base.homeSocket != null) { m.Base.homeSocket.socketActive = false; s_Disabled.Add(m.Base.homeSocket); }
                m.ForceRelease();
            }
        }

        static void PlaceInBowl(params string[] ids)
        {
            var bowl = MixingBowl.Instance;
            var c = bowl.transform.TransformPoint(bowl.innerCenter);
            int k = 0;
            foreach (var id in ids)
            {
                var m = MixableItem.FindById(id);
                if (m == null) { Note("No se encontro " + id); continue; }
                m.Body.isKinematic = false;
                var p = c + new Vector3((k - 0.5f) * 0.2f, 0.06f, 0);
                m.transform.position = p;
                m.Body.position = p;
                m.Body.linearVelocity = Vector3.zero;
                k++;
            }
        }

        static void Shot(string name, Pose? pose)
        {
            var outDir = SessionState.GetString(KeyOut, Path.GetFullPath("Temp/RickMortySmoke"));
            var go = new GameObject("SmokeCam");
            var cam = go.AddComponent<Camera>();
            var main = Camera.main;
            if (pose.HasValue) go.transform.SetPositionAndRotation(pose.Value.position, pose.Value.rotation);
            else if (main != null) go.transform.SetPositionAndRotation(main.transform.position + Vector3.up * (main.transform.position.y < 0.5f ? 1.5f : 0f), main.transform.rotation);
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 300f;
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            try
            {
                var req = new RenderPipeline.StandardRequest { destination = rt };
                if (RenderPipeline.SupportsRenderRequest(cam, req)) RenderPipeline.SubmitRenderRequest(cam, req);
                else { cam.targetTexture = rt; cam.Render(); }
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
                UnityEngine.Object.Destroy(tex);
            }
            catch (Exception e) { s_Notes.Add("Captura fallo " + name + ": " + e.Message); }
            finally
            {
                cam.targetTexture = null;
                rt.Release();
                UnityEngine.Object.Destroy(rt);
                UnityEngine.Object.Destroy(go);
            }
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            var outDir = SessionState.GetString(KeyOut, Path.GetFullPath("Temp/RickMortySmoke"));
            var report = "=== NOTAS ===\n" + string.Join("\n", s_Notes) + "\n\n=== ERRORES (" + s_Errors.Count + ") ===\n" + string.Join("\n\n", s_Errors.Distinct());
            File.WriteAllText(Path.Combine(outDir, "reporte.txt"), report);
            Debug.Log("[Smoke] Reporte en " + outDir + " · errores: " + s_Errors.Count);
            bool batch = SessionState.GetBool(KeyBatch, false);
            SessionState.SetBool(KeyActive, false);
            EditorApplication.ExitPlaymode();
            if (batch) EditorApplication.delayCall += () => EditorApplication.Exit(s_Errors.Count > 0 ? 2 : 0);
        }
    }
}
