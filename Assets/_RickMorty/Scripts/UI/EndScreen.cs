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
            "RICK & MORTY\ny las mezclas de otra dimension\n\n" +
            "Proyecto Final - Fundamentos de Realidad Virtual 2026\nUAO Immersive Solutions\n\n" +
            "Diseno del juego\n[Companero 1]\n[Companero 2]\n\n" +
            "Desarrollo Unity / XR\n[Tu nombre]\n\n" +
            "Hecho con Unity 6 + XR Interaction Toolkit\n\n" +
            "Modelos 3D de Sketchfab (CC-BY)\n" +
            "Yury Misiyuk · Brian Fox · amt16 · Guilherme Navarro\nDemonic Arts · JMGameDev · NexusB · Senpai-3689\n" +
            "Carlos_SR22 · artoire · Ruslan Gadzhiev · sixonetwo\nPieter Ferreira · Kevin.Popescu · PolyProps3D\n" +
            "SpatialNeglect (CC-BY-NC) · assetfactory · widthRider\nJamie McFarlane · Simlion · AleixoAlonso · marishka1611\n" +
            "BRT_GAMES · rickmaolly · Miljan Bojovic · DJMaesen\nDaniel · Glowbox 3D · bez_glaza · Lora · 3DGunsmith\n\n" +
            "Texturas: Poly Haven (CC0)\n\n" +
            "Rick and Morty es propiedad de sus creadores.\nProyecto academico sin fines de lucro.\n\n" +
            "Ningún capibara fue lastimado...\nesperamos.";

        Canvas m_Results, m_Credits;
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

            m_Results = UIKit.WorldCanvas("ResultsCanvas", transform, new Vector2(1100, 1000), 0.0012f, true);
            m_Results.transform.position = origin.position + origin.forward * 1.8f + Vector3.up * 1.5f + origin.right * -0.4f;
            m_Results.transform.rotation = Quaternion.LookRotation(origin.forward, Vector3.up) * Quaternion.Euler(0, -12f, 0);
            var accent = victory ? UIKit.Neon : UIKit.Danger;
            UIKit.Fill("Marco", m_Results.transform, accent);
            var root = UIKit.Panel("Bg", m_Results.transform, new Color(0.03f, 0.06f, 0.08f, 0.98f), Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -6)).transform;

            m_Title = UIKit.Line("Title", root, victory ? "¡LO LOGRASTE, MORTY!" : "ATRAPADO POR EL FBI", 76, accent, TextAlignmentOptions.Center,
                new Vector2(0, 0.86f), new Vector2(1, 0.98f), 30, 0);
            UIKit.AsTitle(m_Title);
            UIKit.Line("Sub", root, victory ? "Fabricaste la Pistola de Portales y escapaste a la Nebulosa Prometeo."
                : "No terminaste la Pistola de Portales a tiempo. Rick va a tener que clonarte otra vez.", 28, new Color(0.85f, 0.9f, 0.95f),
                TextAlignmentOptions.Center, new Vector2(0, 0.79f), new Vector2(1, 0.86f), 40, 0);

            // Puntaje y rango grandes
            var big = UIKit.Panel("Puntaje", root, new Color(1f, 1f, 1f, 0.05f), new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.77f), Vector2.zero, Vector2.zero);
            UIKit.Line("PtsLabel", big.transform, "PUNTAJE", 24, new Color(0.65f, 0.75f, 0.85f), TextAlignmentOptions.TopLeft, new Vector2(0, 0.62f), new Vector2(0.6f, 1), 26, 10);
            UIKit.Line("Pts", big.transform, run.score.ToString("N0"), 84, Color.white, TextAlignmentOptions.BottomLeft, new Vector2(0, 0), new Vector2(0.6f, 0.75f), 26, 8).fontStyle = FontStyles.Bold;
            UIKit.Line("RankLabel", big.transform, "RANGO", 24, new Color(0.65f, 0.75f, 0.85f), TextAlignmentOptions.TopRight, new Vector2(0.6f, 0.62f), new Vector2(1, 1), 26, 10);
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
                UIKit.Line("K", cell, stats[i].k, 28, new Color(0.65f, 0.75f, 0.85f), TextAlignmentOptions.MidlineLeft, new Vector2(0, 0), new Vector2(0.62f, 1), 8, 0);
                UIKit.Line("V", cell, stats[i].v, 30, Color.white, TextAlignmentOptions.MidlineRight, new Vector2(0.6f, 0), new Vector2(1, 1), 8, 0).fontStyle = FontStyles.Bold;
            }

            // Ultimas partidas
            UIKit.Line("HistTitle", root, "ÚLTIMAS PARTIDAS", 22, new Color(0.65f, 0.75f, 0.85f), TextAlignmentOptions.MidlineLeft,
                new Vector2(0.05f, 0.235f), new Vector2(0.95f, 0.27f), 8, 0);
            int n = 0;
            foreach (var r in SaveSystem.Data.runs)
            {
                if (n >= 3) break;
                var line = $"{r.date}     {(r.result == "Victoria" ? "Victoria" : "Derrota")}     {r.score:N0} pts ({r.rank})     {r.discoveries} descubiertos";
                UIKit.Line("Run" + n, root, line, 22, n == 0 ? Color.white : new Color(0.7f, 0.78f, 0.86f), TextAlignmentOptions.MidlineLeft,
                    new Vector2(0.05f, 0.195f - n * 0.04f), new Vector2(0.95f, 0.235f - n * 0.04f), 8, 0);
                n++;
            }

            UIKit.Button("Replay", root, "Jugar de nuevo", () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().buildIndex : 0),
                new Vector2(0.05f, 0.02f), new Vector2(0.48f, 0.08f), fontSize: 36);
            UIKit.Button("Quit", root, "Salir", Quit, new Vector2(0.52f, 0.02f), new Vector2(0.95f, 0.08f), color: new Color(0.5f, 0.12f, 0.1f, 0.95f), fontSize: 36);

            // Creditos flotando lentamente
            m_Credits = UIKit.WorldCanvas("CreditsCanvas", transform, new Vector2(800, 1400), 0.0015f, false);
            m_Credits.transform.position = origin.position + origin.forward * 3.5f + origin.right * 2.2f;
            m_Credits.transform.rotation = Quaternion.LookRotation(origin.forward, Vector3.up) * Quaternion.Euler(0, 25f, 0);
            m_CreditsText = UIKit.Text("Credits", m_Credits.transform, credits, 46, new Color(0.8f, 1f, 0.85f), TextAlignmentOptions.Top);
            StartCoroutine(CoScrollCredits());
        }

        IEnumerator CoScrollCredits()
        {
            var start = m_Credits.transform.position - Vector3.up * 1.5f;
            float t = 0;
            while (true)
            {
                t += Time.deltaTime;
                m_Credits.transform.position = start + Vector3.up * Mathf.Repeat(t * 0.12f, 4f);
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
