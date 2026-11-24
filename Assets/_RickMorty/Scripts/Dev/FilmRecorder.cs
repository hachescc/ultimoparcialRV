#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RickMorty.Dev
{
    public class FilmRecorder : MonoBehaviour
    {
        public struct FilmShot
        {
            public Vector3 pos; public Quaternion rot; public float fov; public bool useMain;
            public string section, title; public float hold;
        }

        public int width = 1280, height = 720, fps = 30;
        public int fxLayer = 4;
        public float maxSeconds = 900f;

        MediaEncoder m_Enc;
        RenderTexture m_Rt;
        Texture2D m_Tex;
        Camera m_Cam;
        TextMeshPro m_Caption;
        bool m_Audio;
        int m_Channels;
        readonly Queue<FilmShot> m_Queue = new Queue<FilmShot>();
        FilmShot m_Target;
        bool m_HasTarget, m_Snap = true;
        float m_CaptionAlpha, m_CaptionTimer;
        public int Frames { get; private set; }
        public bool Paused;

        public bool HasQueued => m_Queue.Count > 0;

        public void Begin(string path)
        {
            Time.captureFramerate = fps;

            var camGo = new GameObject("[FilmCam]");
            DontDestroyOnLoad(camGo);
            m_Cam = camGo.AddComponent<Camera>();
            m_Cam.nearClipPlane = 0.01f; m_Cam.farClipPlane = 400f; m_Cam.fieldOfView = 70f;
            m_Cam.enabled = false;
            var data = m_Cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            var capGo = new GameObject("[FilmCaption]");
            DontDestroyOnLoad(capGo);
            m_Caption = capGo.AddComponent<TextMeshPro>();
            m_Caption.fontSize = 0.32f;
            m_Caption.alignment = TextAlignmentOptions.BottomLeft;
            m_Caption.textWrappingMode = TextWrappingModes.Normal;
            m_Caption.rectTransform.sizeDelta = new Vector2(0.9f, 0.3f);
            m_Caption.color = new Color(1, 1, 1, 0);

            m_Rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            m_Tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            var video = new VideoTrackAttributes
            {
                frameRate = new MediaRational(fps), width = (uint)width, height = (uint)height,
                includeAlpha = false, bitRateMode = UnityEditor.VideoBitrateMode.Medium
            };
            int rate = AudioSettings.outputSampleRate;
            m_Channels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
            m_Audio = rate > 0;
            try
            {
                if (m_Audio)
                {
                    var audio = new AudioTrackAttributes { sampleRate = new MediaRational(rate), channelCount = (ushort)m_Channels, language = "es" };
                    m_Enc = new MediaEncoder(path, video, audio);
                    AudioRenderer.Start();
                }
                else m_Enc = new MediaEncoder(path, video);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Film] Sin audio (" + e.Message + "), grabando solo video");
                m_Audio = false;
                m_Enc = new MediaEncoder(path, video);
            }
            Debug.Log($"[Film] Grabando {width}x{height}@{fps} → {path} (audio: {(m_Audio ? rate + " Hz" : "no")})");
        }

        public void Queue(FilmShot s) => m_Queue.Enqueue(s);

        public void FollowPlayer() { m_Target.useMain = true; m_HasTarget = true; m_Snap = true; m_CaptionTimer = 0f; }

        public IEnumerator PlayNext()
        {
            var s = m_Queue.Dequeue();
            bool cut = !m_HasTarget || s.useMain != m_Target.useMain || (s.pos - m_Cam.transform.position).sqrMagnitude > 2.5f * 2.5f;
            m_Target = s; m_HasTarget = true; m_Snap = cut;
            m_Caption.text = $"<size=62%><color=#7DFF6A>{s.section}</color></size>\n<mark=#000000B0 padding=\"14,14,6,6\">{s.title}</mark>";
            m_CaptionTimer = s.hold + (cut ? 0f : 0.9f);
            int frames = Mathf.RoundToInt((s.hold + (cut ? 0f : 0.9f)) * fps);
            for (int i = 0; i < frames; i++) yield return null;
        }

        void LateUpdate()
        {
            if (m_Enc == null) return;
            if (Paused || !m_HasTarget)
            {
                if (m_Audio) { int k = AudioRenderer.GetSampleCountForCaptureFrame(); if (k > 0) { var junk = new NativeArray<float>(k * m_Channels, Allocator.Temp); AudioRenderer.Render(junk); junk.Dispose(); } }
                return;
            }
            float dt = 1f / fps;

            Vector3 tp; Quaternion tr; float tf;
            if (m_Target.useMain && Camera.main != null)
            {
                var mc = Camera.main.transform;
                tp = mc.position; tr = mc.rotation; tf = 80f;
                m_Snap = true;
            }
            else { tp = m_Target.pos; tr = m_Target.rot; tf = m_Target.fov; }
            var t = m_Cam.transform;
            if (m_Snap) { t.SetPositionAndRotation(tp, tr); m_Cam.fieldOfView = tf; m_Snap = m_Target.useMain; }
            else
            {
                float k = 1f - Mathf.Exp(-dt * 3.2f);
                t.SetPositionAndRotation(Vector3.Lerp(t.position, tp, k), Quaternion.Slerp(t.rotation, tr, k));
                m_Cam.fieldOfView = Mathf.Lerp(m_Cam.fieldOfView, tf, k);
            }
            m_Cam.cullingMask = m_Target.useMain ? ~0 : ~(1 << fxLayer);

            m_CaptionTimer -= dt;
            float target = m_CaptionTimer > 0.4f ? 1f : 0f;
            m_CaptionAlpha = Mathf.MoveTowards(m_CaptionAlpha, target, dt * 3f);
            m_Caption.color = new Color(1, 1, 1, m_CaptionAlpha);
            float halfH = 0.6f * Mathf.Tan(m_Cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfW = halfH * width / height;
            m_Caption.fontSize = halfH * 1.45f;
            m_Caption.rectTransform.sizeDelta = new Vector2(halfW * 1.7f, halfH * 0.8f);
            m_Caption.transform.SetPositionAndRotation(t.position + t.rotation * new Vector3(-halfW * 0.08f, -halfH * 0.52f, 0.6f), t.rotation);

            var req = new RenderPipeline.StandardRequest { destination = m_Rt };
            if (RenderPipeline.SupportsRenderRequest(m_Cam, req)) RenderPipeline.SubmitRenderRequest(m_Cam, req);
            else { m_Cam.targetTexture = m_Rt; m_Cam.Render(); m_Cam.targetTexture = null; }
            var prev = RenderTexture.active;
            RenderTexture.active = m_Rt;
            m_Tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            m_Tex.Apply(false);
            RenderTexture.active = prev;
            m_Enc.AddFrame(m_Tex);

            if (m_Audio)
            {
                int n = AudioRenderer.GetSampleCountForCaptureFrame();
                if (n > 0)
                {
                    var buf = new NativeArray<float>(n * m_Channels, Allocator.Temp);
                    AudioRenderer.Render(buf);
                    m_Enc.AddSamples(buf);
                    buf.Dispose();
                }
            }
            Frames++;
            if (Frames >= maxSeconds * fps) { Debug.LogError("[Film] Tope de duracion alcanzado: se cierra el video"); End(); return; }
            if (Frames % 300 == 0) Debug.Log($"[Film] {Frames} cuadros ({Frames / (float)fps:0} s)");
        }

        public void End()
        {
            if (m_Enc == null) return;
            if (m_Audio) AudioRenderer.Stop();
            m_Enc.Dispose();
            m_Enc = null;
            Time.captureFramerate = 0;
            Debug.Log($"[Film] Listo: {Frames} cuadros = {Frames / (float)fps:0.0} s");
        }

        void OnDestroy() => End();
    }
}
#endif
