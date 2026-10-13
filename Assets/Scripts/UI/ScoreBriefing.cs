using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using QuantumCrossy.Core;
using QuantumCrossy.Scoring;

namespace QuantumCrossy.UI
{
    /// <summary>
    /// 플레이 전에 점수 식을 보여 준다. 탭이나 키로 닫히면 게임이 시작된다.
    /// </summary>
    public sealed class ScoreBriefing : MonoBehaviour
    {
        const float InputDelay = 0.2f;

        Action onStart;
        RectTransform safeRoot;
        Rect lastSafe;
        float unlockAt;
        bool closed;

        public void Show(QuantumCrossyConfig config, ScoreDistribution distribution, Action started)
        {
            onStart = started;
            unlockAt = Time.unscaledTime + InputDelay;
            string body = BuildBody(config, distribution);
            Font font = KoreanFont.Load();
            KoreanFont.Warmup(font, "길건너 양자들" + body + "탭하거나 아무 키나 눌러 시작", 64);

            var canvasGo = new GameObject("BriefingCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var dim = CreateChild(canvasGo.transform, "Dim");
            Stretch(dim.GetComponent<RectTransform>());
            var dimImage = dim.AddComponent<Image>();
            dimImage.sprite = WhiteSprite();
            dimImage.color = new Color(0.02f, 0.025f, 0.06f, 0.94f);
            dimImage.raycastTarget = false;

            var safeGo = CreateChild(canvasGo.transform, "Safe");
            safeRoot = safeGo.GetComponent<RectTransform>();
            Stretch(safeRoot);

            Text title = CreateText(safeRoot, "Title", font, 60, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.55f, 0.95f, 1f));
            Place(title.rectTransform, 0.08f, 0.84f, 0.92f, 0.96f);
            title.text = "길건너 양자들";

            Text rules = CreateText(safeRoot, "Rules", font, 34, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.92f, 0.95f, 0.98f));
            Place(rules.rectTransform, 0.07f, 0.22f, 0.93f, 0.82f);
            rules.text = body;

            Text prompt = CreateText(safeRoot, "Prompt", font, 32, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.65f));
            Place(prompt.rectTransform, 0.08f, 0.05f, 0.92f, 0.16f);
            prompt.text = "탭하거나 아무 키나 눌러 시작";

            ApplySafeArea();
        }

        void Update()
        {
            if (closed)
                return;

            if (safeRoot != null && lastSafe != Screen.safeArea)
                ApplySafeArea();

            if (Time.unscaledTime < unlockAt || !DismissPressed())
                return;

            closed = true;
            Action start = onStart;
            onStart = null;
            start?.Invoke();
            Destroy(gameObject);
        }

        static bool DismissPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                return true;

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                return true;

            Touchscreen screen = Touchscreen.current;
            if (screen != null && screen.primaryTouch.press.wasPressedThisFrame)
                return true;

            return false;
        }

        static string BuildBody(QuantumCrossyConfig config, ScoreDistribution distribution)
        {
            var states = new StringBuilder();
            for (int i = 0; i < distribution.StateCount; i++)
            {
                ScoreDistribution.Eigenstate state = distribution.StateAt(i);
                if (i > 0)
                    states.Append("   ");
                states.Append(state.Name);
                states.Append(' ');
                states.Append(state.Scale.ToString("0.##"));
            }

            float percent = distribution.HighScoreProbability * 100f;
            return
                "광선을 모으면 고득점 확률이 오른다.\n" +
                "고득점은 배수 " + distribution.HighScoreCutoff.ToString("0.##") +
                " 이상이다. 지금은 " + percent.ToString("0.#") + "%.\n\n" +
                "게이트에 닿으면 측정된다.\n" +
                "그 확률로 상태를 하나 뽑고, 게임은 끝난다.\n" +
                "점수 = 상태 배수 × 전진한 칸 × " + config.scorePerDistance.ToString("0.##") + "\n" +
                states + "\n\n" +
                "퀀텀 점프로 게이트 줄을 넘으면\n" +
                "보너스 광선 " + config.quantumJumpBonusPhotons + "개. 한 줄에 한 번.";
        }

        void ApplySafeArea()
        {
            lastSafe = Screen.safeArea;
            if (safeRoot == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Vector2 min = lastSafe.position;
            Vector2 max = lastSafe.position + lastSafe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            safeRoot.anchorMin = min;
            safeRoot.anchorMax = max;
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
        }

        static Text CreateText(Transform parent, string name, Font font, int size, FontStyle style, TextAnchor anchor, Color color)
        {
            var go = CreateChild(parent, name);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        static Sprite whiteSprite;

        static Sprite WhiteSprite()
        {
            if (whiteSprite != null)
                return whiteSprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return whiteSprite;
        }
    }
}
