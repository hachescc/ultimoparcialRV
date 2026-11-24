using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace RickMorty
{
    public static class UIKit
    {
        public static readonly Color Neon = new Color(0.45f, 1f, 0.45f);
        public static readonly Color Cyan = new Color(0.4f, 0.9f, 1f);
        public static readonly Color PanelDark = new Color(0.03f, 0.06f, 0.08f, 0.88f);
        public static readonly Color Holo = new Color(0.25f, 0.75f, 1f, 0.35f);
        public static readonly Color Danger = new Color(1f, 0.3f, 0.25f);

        static Sprite s_Rounded;

        public static Sprite Rounded
        {
            get
            {
                if (s_Rounded != null) return s_Rounded;
                const int n = 64, r = 16;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(r - x, x - (n - 1 - r)));
                    float dy = Mathf.Max(0, Mathf.Max(r - y, y - (n - 1 - r)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d + 0.5f)));
                }
                tex.Apply();
                s_Rounded = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
                return s_Rounded;
            }
        }

        public static Canvas WorldCanvas(string name, Transform parent, Vector2 sizePx, float metersPerPx, bool interactive)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizePx;
            rt.localScale = Vector3.one * metersPerPx;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 3f;
            if (interactive)
            {
                go.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
            if (Camera.main != null) canvas.worldCamera = Camera.main;
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin = default, Vector2 offsetMax = default, bool rounded = true)
        {
            var rt = Rect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded)
            {
                img.sprite = Rounded;
                img.type = Image.Type.Sliced;
            }
            img.raycastTarget = false;
            return img;
        }

        public static Image Fill(string name, Transform parent, Color color, bool rounded = true)
            => Panel(name, parent, color, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, rounded);

        static TMP_FontAsset s_TitleFont;

        public static TMP_FontAsset TitleFont
        {
            get
            {
                if (s_TitleFont == null) s_TitleFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Titulos SDF");
                return s_TitleFont;
            }
        }

        public static T AsTitle<T>(T t) where T : TMP_Text
        {
            var f = TitleFont;
            if (t != null && f != null) { t.font = f; t.fontStyle = FontStyles.Normal; }
            return t;
        }

        public static string TitleTag(string s) => "<font=\"Titulos SDF\">" + s + "</font>";

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft, Vector2? anchorMin = null, Vector2? anchorMax = null,
            Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            var rt = Rect(name, parent, anchorMin ?? Vector2.zero, anchorMax ?? Vector2.one, offsetMin, offsetMax);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        public static TextMeshProUGUI Line(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align,
            Vector2 anchorMin, Vector2 anchorMax, float padX, float padY)
        {
            var t = Text(name, parent, text, size, color, align, anchorMin, anchorMax, new Vector2(padX, padY), new Vector2(-padX, -padY));
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.enableAutoSizing = true;
            t.fontSizeMin = size * 0.6f;
            t.fontSizeMax = size;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        public static TextMeshProUGUI Block(string name, Transform parent, string text, float min, float max, Color color,
            Vector2 anchorMin, Vector2 anchorMax, float pad)
        {
            var t = Text(name, parent, text, max, color, TextAlignmentOptions.TopLeft, anchorMin, anchorMax, new Vector2(pad, pad * 0.5f), new Vector2(-pad, -pad * 0.5f));
            t.enableAutoSizing = true;
            t.fontSizeMin = min;
            t.fontSizeMax = max;
            t.lineSpacing = -4;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, Action onClick, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin = default, Vector2 offsetMax = default, Color? color = null, float fontSize = 28f)
        {
            var img = Panel(name, parent, color ?? new Color(0.15f, 0.45f, 0.2f, 0.95f), anchorMin, anchorMax, offsetMin, offsetMax);
            img.raycastTarget = true;
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            btn.colors = colors;
            btn.targetGraphic = img;
            var t = Text("Label", img.transform, label, fontSize, Color.white, TextAlignmentOptions.Center);
            t.fontStyle = FontStyles.Bold;
            if (onClick != null)
            {
                btn.onClick.AddListener(() =>
                {
                    AudioFX.Play("button", btn.transform.position, 0.6f);
                    onClick();
                });
            }
            return btn;
        }

        public static void OnHover(GameObject go, UnityAction enter, UnityAction exit)
        {
            var trigger = go.GetComponent<EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<EventTrigger>();
            if (enter != null)
            {
                var e = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                e.callback.AddListener(_ => enter());
                trigger.triggers.Add(e);
            }
            if (exit != null)
            {
                var e = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                e.callback.AddListener(_ => exit());
                trigger.triggers.Add(e);
            }
        }
    }
}
