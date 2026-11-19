using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Vineta dinamica de confort (anti-mareo): oscurece los bordes al caminar o girar con el joystick.
    /// Se activa/desactiva desde el menu de pausa y se guarda en el archivo de guardado.
    /// </summary>
    public class ComfortVignette : MonoBehaviour
    {
        public static ComfortVignette Instance { get; private set; }

        public Transform origin;
        [Range(0, 1)] public float maxStrength = 0.85f;
        public float moveSpeedForMax = 1.5f;
        public float turnSpeedForMax = 120f;
        public float smoothing = 6f;

        public bool Enabled
        {
            get => m_Enabled;
            set
            {
                m_Enabled = value;
                SaveSystem.Data.comfortVignette = value;
                SaveSystem.Save();
            }
        }

        bool m_Enabled = true;
        Material m_Mat;
        Renderer m_Renderer;
        Vector3 m_LastPos;
        float m_LastYaw;
        float m_Current;

        void Awake()
        {
            Instance = this;
            m_Enabled = SaveSystem.Data.comfortVignette;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.name = "ComfortVignette";
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = new Vector3(0, 0, 0.14f);
            quad.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            m_Renderer = quad.GetComponent<Renderer>();
            m_Mat = MatLib.TransparentInstance(Color.black);
            m_Mat.SetTexture("_BaseMap", BuildTexture());
            m_Mat.renderQueue = 3980;
            m_Renderer.sharedMaterial = m_Mat;
            m_Renderer.sortingOrder = 31980;
            m_Renderer.enabled = false;
        }

        static Texture2D BuildTexture()
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.95f, d));
                tex.SetPixel(x, y, new Color(0, 0, 0, a));
            }
            tex.Apply();
            return tex;
        }

        void Start()
        {
            if (origin != null)
            {
                m_LastPos = origin.position;
                m_LastYaw = origin.eulerAngles.y;
            }
        }

        void LateUpdate()
        {
            if (origin == null || Time.deltaTime <= 0f) return;
            var pos = origin.position;
            float yaw = origin.eulerAngles.y;
            var v = pos - m_LastPos; v.y = 0;
            float speed = v.magnitude / Time.deltaTime;
            float turn = Mathf.Abs(Mathf.DeltaAngle(m_LastYaw, yaw)) / Time.deltaTime;
            m_LastPos = pos;
            m_LastYaw = yaw;

            float target = 0f;
            if (m_Enabled)
            {
                // Saltos muy grandes (teletransporte/snap turn) no deben activar la vineta
                if (speed < 20f) target = Mathf.Max(target, Mathf.Clamp01(speed / moveSpeedForMax));
                if (turn < 2000f) target = Mathf.Max(target, Mathf.Clamp01(turn / turnSpeedForMax));
            }
            m_Current = Mathf.Lerp(m_Current, target * maxStrength, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            m_Renderer.enabled = m_Current > 0.01f;
            m_Mat.SetColor("_BaseColor", new Color(0, 0, 0, m_Current));
        }
    }
}
