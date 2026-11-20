using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RickMorty
{
    /// <summary>
    /// Cierre de la aplicacion: resultados verificables de la partida, historial guardado
    /// en JSON, creditos flotando en el espacio y botones para reiniciar o salir.
    /// Va en el objeto "EndRoom" (vacio espacial lejos del garaje).
    /// </summary>
    public class EndScreen : MonoBehaviour
    {
        public static EndScreen Instance { get; private set; }

        public Transform playerSpot;
        [TextArea(5, 20)]
        public string credits =
            "<size=150%><font=\"Titulos SDF\"><color=#7DFF6A>RICK & MORTY</color></font></size>\ny las mezclas de otra dimensión\n\n" +
            "<size=75%>Proyecto Final\nFundamentos de Realidad Virtual 2026\nUAO Immersive Solutions</size>\n\n" +
            "<size=75%><color=#9FD8FF>UN JUEGO DE</color></size>\n" +
            "<size=125%>William Franco L.\nHugo Chavarro Escobar\nJuan David Muñoz S.\nSebastian Montero I.</size>\n\n" +
            "<size=70%>Hecho con Unity 6 y XR Interaction Toolkit</size>\n\n" +
            // La lista de autores es obligatoria (licencias CC-BY) pero va en letra chica y corrida, para que no opaque al equipo
            "<size=75%><color=#9FD8FF>MODELOS 3D DE SKETCHFAB (CC-BY)</color></size>\n" +
            "<size=52%><color=#B8C8C0>Yury Misiyuk, Brian Fox, amt16, Guilherme Navarro, Demonic Arts, JMGameDev, NexusB, Senpai-3689, " +
            "Carlos_SR22, artoire, Ruslan Gadzhiev, sixonetwo, Pieter Ferreira, Kevin.Popescu, PolyProps3D, " +
            "SpatialNeglect (CC-BY-NC), assetfactory, widthRider, Jamie McFarlane, Simlion, AleixoAlonso, marishka1611, " +
            "BRT_GAMES, rickmaolly, Miljan Bojovic, DJMaesen, Daniel, Glowbox 3D, bez_glaza, Lora, 3DGunsmith, " +
            "Julio_Mendez, June's Cloud, Daniyal Malik, ZiemniaQ, jimbogies, wasabicats, davidfischer.\n" +
            "Texturas y cielo: Poly Haven (CC0)</color></size>\n\n" +
            "<size=60%>Rick and Morty es propiedad de sus creadores.\nProyecto académico sin fines de lucro.</size>\n\n" +
            "Ningún capibara fue lastimado...\nesperamos.";

        Canvas m_Results, m_Credits;
        CanvasGroup m_CreditsGroup;
        float m_CreditsFloorY;
        TextMeshProUGUI m_Title, m_Body, m_History, m_CreditsText;

        void Awake()
        {
            Instance = this;
            BuildStars();
        }

        void BuildStars()
        {
            var ps = FX.Attach(transform, Color.white, 30f, 0.05f, 0.0f, 0f, 25f, true);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.maxParticles = 400;
            ps.transform.localPosition = new Vector3(0, 2, 0);
        }

        public void Show(bool victory, RunRecord run, string extraLog)
        {
            gameObject.SetActive(true);
            var origin = playerSpot != null ? playerSpot : transform;

            // Piso invisible enorme: la plataforma mide 4x4 m y el jugador puede seguir caminando. Antes, al
            // acercarse a los creditos, se salia del borde y caia para siempre sin poder tocar "Jugar de nuevo".
            var floor = new GameObject("PisoInvisible");
            floor.transform.SetParent(transform, false);
            floor.transform.position = origin.position + Vector3.down * 0.1f;
            floor.transform.rotation = Quaternion.identity;
            var fc = floor.AddComponent<BoxCollider>();
            fc.size = new Vector3(60f, 0.2f, 60f);

            // Panel mas cerca y mas grande: texto de 0.6-0.8 grados -> ~1 grado o mas
            m_Results = UIKit.WorldCanvas("ResultsCanvas", transform, new Vector2(1100, 1000), 0.0013f, true);
            m_Results.transform.position = origin.position + origin.forward * 1.6f + Vector3.up * 1.45f + origin.right * -0.35f;
            m_Results.transform.rotation = Quaternion.LookRotation(origin.forward, Vector3.up) * Quaternion.Euler(0, -12f, 0);
            var accent = victory ? UIKit.Neon : UIKit.Danger;
            UIKit.Fill("Marco", m_Results.transform, accent);
            var root = UIKit.Panel("Bg", m_Results.transform, new Color(0.03f, 0.06f, 0.08f, 0.98f), Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -6)).transform;

            m_Title = UIKit.Line("Title", root, victory ? "¡LO LOGRASTE, MORTY!" : "ATRAPADO POR EL FBI", 76, accent, TextAlignmentOptions.Center,
                new Vector2(0, 0.86f), new Vector2(1, 0.98f), 30, 0);
            UIKit.AsTitle(m_Title);
            var sub = UIKit.Block("Sub", root, victory ? "Fabricaste la Pistola de Portales y escapaste a la Nebulosa Prometeo."
                : "No terminaste la Pistola de Portales a tiempo. Rick va a tener que clonarte otra vez.", 26, 34, new Color(0.85f, 0.9f, 0.95f),
                new Vector2(0.03f, 0.762f), new Vector2(0.97f, 0.868f), 16);   // caben 2 lineas (antes la derrota terminaba en "...")
            sub.alignment = TextAlignmentOptions.Center;

            // Puntaje y rango grandes
            var big = UIKit.Panel("Puntaje", root, new Color(1f, 1f, 1f, 0.05f), new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.77f), Vector2.zero, Vector2.zero);
            UIKit.Line("PtsLabel", big.transform, "PUNTAJE", 28, new Color(0.65f, 0.75f, 0.85f), TextAlignmentOptions.TopLeft, new Vector2(0, 0.62f), new Vector2(0.6f, 1), 26, 10);
            UIKit.Line("Pts", big.transform, run.score.ToString("N0"), 84, Color.white, TextAlignmentOptions.BottomLeft, new Vector2(0, 0), new Vector2(0.6f, 0.75f), 26, 8).fontStyle = FontStyles.Bold;
            UIKit.Line("RankLabel", big.transform, "RANGO", 28, new Color(0.65f, 0.75f, 0.85f), TextAlignmentOptions.TopRight, new Vector2(0.6f, 0.62f), new Vector2(1, 1), 26, 10);
            UIKit.Line("Rank", big.transform, run.rank, 96, accent, TextAlignmentOptions.BottomRight, new Vector2(0.6f, 0), new Vector2(1, 0.8f), 26, 4).fontStyle = FontStyles.Bold;

            // Estadisticas en dos columnas
            (string k, string v)[] stats =
            {
                ("Tiempo jugado", GameTimer.Format(run.seconds)),
                ("Descubrimientos", run.discoveries + " de " + GameDatabase.DiscoverableCount),
                ("Explosiones", run.explosions.ToString()),
                ("Criaturas eliminadas", run.kills.ToString()),
                ("Easter eggs", run.easterEggs + " de 8"),
                ("Mejor puntaje", SaveSystem.Data.bestScore.ToString("N0")),
            };
            for (int i = 0; i < stats.Length; i++)
            {
                int col = i % 2, row = i / 2;
                var cell = UIKit.Rect("Stat" + i, root, new Vector2(0.05f + col * 0.46f, 0.47f - row * 0.075f), new Vector2(0.49f + col * 0.46f, 0.545f - row * 0.075f));
                UIKit.Line("K", cell, stats[i].k, 34, new Color(0.65f, 0.75f, 0.85f), TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(0.62f, 1), 8, 0);
                UIKit.Line("V", cell, stats[i].v, 36, Color.white, TextAlignmentOptions.MidlineRight, new Vector2(0.6f, 0), new Vector2(1, 1), 8, 0).fontStyle = FontStyles.Bold;
            }

            // Ultimas partidas
            UIKit.Line("HistTitle", root, "ÚLTIMAS PARTIDAS", 26, new Color(0.65f, 0.75f, 0.85f), TextAlignmentOptions.MidlineLeft,
                new Vector2(0.05f, 0.262f), new Vector2(0.95f, 0.3f), 8, 0);
            int n = 0;
            foreach (var r in SaveSystem.Data.runs)
            {
                if (n >= 3) break;
                var line = $"{r.date}     {(r.result == "Victoria" ? "Victoria" : "Derrota")}     {r.score:N0} pts ({r.rank})     {r.discoveries} descubiertos";
                UIKit.Line("Run" + n, root, line, 27, n == 0 ? Color.white : new Color(0.7f, 0.78f, 0.86f), TextAlignmentOptions.MidlineLeft,
                    new Vector2(0.05f, 0.218f - n * 0.042f), new Vector2(0.95f, 0.26f - n * 0.042f), 8, 0);
                n++;
            }

            UIKit.Button("Replay", root, "Jugar de nuevo", () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().buildIndex : 0),
                new Vector2(0.05f, 0.022f), new Vector2(0.48f, 0.118f), fontSize: 44);
            UIKit.Button("Quit", root, "Salir", Quit, new Vector2(0.52f, 0.022f), new Vector2(0.95f, 0.118f), color: new Color(0.5f, 0.12f, 0.1f, 0.95f), fontSize: 44);

            // Creditos flotando lentamente
            m_Credits = UIKit.WorldCanvas("CreditsCanvas", transform, new Vector2(800, 1400), 0.0015f, false);
            // a ~2.7 m y 28 grados a la derecha (antes 4.1 m: letra de 0.67 grados y fuera de la plataforma)
            m_Credits.transform.position = origin.position + origin.forward * 2.3f + origin.right * 1.75f;
            m_Credits.transform.rotation = Quaternion.LookRotation(origin.forward, Vector3.up) * Quaternion.Euler(0, 37f, 0);
            m_CreditsGroup = m_Credits.gameObject.AddComponent<CanvasGroup>();
            m_CreditsText = UIKit.Text("Credits", m_Credits.transform, credits, 46, new Color(0.8f, 1f, 0.85f), TextAlignmentOptions.Top);
            m_CreditsFloorY = origin.position.y;
            StartCoroutine(CoScrollCredits());
        }

        IEnumerator CoScrollCredits()
        {
            // El texto entra por abajo y sube hasta que su ultima linea pasa por encima de la vista; luego se
            // desvanece y vuelve a empezar. (Antes el recorrido era fijo de 4 m: las ultimas lineas nunca subian
            // del piso y el bucle daba un salto brusco.)
            yield return null;   // un cuadro para que TMP calcule el alto real del texto
            const float scale = 0.0015f, half = 1400 * 0.0015f * 0.5f;
            float textH = Mathf.Max(1f, m_CreditsText.preferredHeight * scale);
            var basePos = m_Credits.transform.position;
            float range = textH + 2.2f;
            float t = 0;
            while (true)
            {
                t += Time.deltaTime;
                float s = Mathf.Repeat(t * 0.16f, range);
                // 's' = cuanto ha subido el borde superior del texto desde 0.5 m sobre el piso
                basePos.y = m_CreditsFloorY + 0.5f - half + s;
                m_Credits.transform.position = basePos;
                if (m_CreditsGroup != null) m_CreditsGroup.alpha = Mathf.Clamp01(s / 0.4f) * Mathf.Clamp01((range - s) / 0.5f);
                yield return null;
            }
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
