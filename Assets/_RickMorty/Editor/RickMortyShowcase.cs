using System;
using System.IO;
using RickMorty.Dev;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RickMorty.EditorTools
{
    /// <summary>
    /// Recorrido automatico con capturas para documentar el juego (lo usa _Tools/pdf para armar el PDF).
    /// Menu "Rick y Morty/4. Recorrido con capturas" o batch: -executeMethod ...RickMortyShowcase.BatchRun -showcaseOut &lt;dir&gt;
    /// </summary>
    [InitializeOnLoad]
    public static class RickMortyShowcase
    {
        const string KeyActive = "RM_Showcase_Active", KeyBatch = "RM_Showcase_Batch", KeyOut = "RM_Showcase_Out", KeyFilm = "RM_Showcase_Film";

        static RickMortyShowcase()
        {
            if (!SessionState.GetBool(KeyActive, false)) return;
            EditorApplication.playModeStateChanged += OnPlayMode;
            if (EditorApplication.isPlaying) EditorApplication.delayCall += Begin;
        }

        [MenuItem("Rick y Morty/4. Recorrido con capturas", priority = 4)]
        public static void RunFromMenu() => Run(false, Path.GetFullPath("Documentacion/capturas"));

        public static void BatchRun()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-showcaseOut");
            Run(true, i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.GetFullPath("Documentacion/capturas"));
        }

        /// <summary>Video MP4 del gameplay (1280x720, 30 fps): -executeMethod ...RickMortyShowcase.BatchFilm -filmOut &lt;archivo.mp4&gt;</summary>
        public static void BatchFilm()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-filmOut");
            var path = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.GetFullPath("Documentacion/RickMorty_Gameplay.mp4");
            SessionState.SetString(KeyFilm, path);
            SessionState.SetInt("RM_Film_Test", Array.IndexOf(args, "-filmTest") >= 0 ? 3 : 0);
            Run(true, Path.GetDirectoryName(path));
        }

        [MenuItem("Rick y Morty/5. Video del gameplay (MP4)", priority = 5)]
        public static void FilmFromMenu()
        {
            SessionState.SetString(KeyFilm, Path.GetFullPath("Documentacion/RickMorty_Gameplay.mp4"));
            Run(false, Path.GetFullPath("Documentacion"));
        }

        static void Run(bool batch, string outDir)
        {
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

        static bool s_Started;

        static void Begin()
        {
            if (s_Started || !SessionState.GetBool(KeyActive, false)) return;
            s_Started = true;
            var go = new GameObject("[Showcase]");
            var r = go.AddComponent<ShowcaseRunner>();
            r.outDir = SessionState.GetString(KeyOut, Path.GetFullPath("Documentacion/capturas"));
            r.onFinished = Finish;
            var film = SessionState.GetString(KeyFilm, "");
            if (!string.IsNullOrEmpty(film)) { r.filmMode = true; r.filmPath = film; r.filmTestParts = SessionState.GetInt("RM_Film_Test", 0); }
        }

        static void Finish()
        {
            bool batch = SessionState.GetBool(KeyBatch, false);
            SessionState.SetBool(KeyActive, false);
            SessionState.SetString(KeyFilm, "");
            EditorApplication.ExitPlaymode();
            if (batch) EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }
    }
}
