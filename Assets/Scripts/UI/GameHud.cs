using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace QuantumCrossy.UI
{
    public sealed class GameHud : MonoBehaviour
    {
        const string WarmupText =
            "길건너 양자들 고득점 확률 진행 광자 스와이프 마우스 드래그 또는 WASD 방향키 스페이스는 전진 " +
            "게이트가 다가올 때 타이밍을 맞춰 뛰어들면 양자 도약 파동함수 붕괴 측정 게이트에 닿았습니다 " +
            "관측된 상태 점수 직전 모은 다시 하기 바닥 낮음 중간 높음 전설 +%./";

        Font font;
        Text probabilityText;
        Text distanceText;
        Text photonText;
        Text toastText;
        Image probabilityFill;
        GameObject hintRoot;
        GameObject collapseRoot;
        Text collapseScore;
        Text collapseState;
        Text collapseDetail;
        RectTransform safeRoot;
        Rect lastSafe;
        Coroutine toastRoutine;

        public void Build(Action onRestart)
        {
            font = KoreanFont.Load();
            KoreanFont.Warmup(font, WarmupText, 64);

            var canvasGo = new GameObject("HUD", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var safeGo = CreateChild(canvasGo.transform, "Safe");
            safeRoot = safeGo.GetComponent<RectTransform>();
            Stretch(safeRoot);

            probabilityText = CreateText(safeRoot, "Probability", 52, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Place(probabilityText.rectTransform, 0.06f, 0.90f, 0.94f, 0.97f);

            var barBack = CreateImage(safeRoot, "BarBack", new Color(1f, 1f, 1f, 0.12f));
            Place(barBack.rectTransform, 0.12f, 0.875f, 0.88f, 0.895f);
            var barFill = CreateImage(safeRoot, "BarFill", new Color(0.45f, 0.95f, 1f, 1f));
            Place(barFill.rectTransform, 0.12f, 0.875f, 0.88f, 0.895f);
            probabilityFill = barFill;
            probabilityFill.type = Image.Type.Filled;
            probabilityFill.fillMethod = Image.FillMethod.Horizontal;
            probabilityFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            probabilityFill.fillAmount = 0.08f;

            distanceText = CreateText(safeRoot, "Distance", 32, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.85f, 0.92f, 0.95f));
            Place(distanceText.rectTransform, 0.08f, 0.82f, 0.48f, 0.87f);
            photonText = CreateText(safeRoot, "Photons", 32, FontStyle.Normal, TextAnchor.MiddleRight, new Color(1f, 0.92f, 0.7f));
            Place(photonText.rectTransform, 0.5f, 0.82f, 0.92f, 0.87f);

            hintRoot = CreateChild(safeRoot, "Hint");
            var hintRect = hintRoot.GetComponent<RectTransform>();
            Place(hintRect, 0.07f, 0.02f, 0.93f, 0.13f);
            Text hint = CreateText(hintRoot.transform, "HintText", 28, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.82f));
            Stretch(hint.rectTransform);
            hint.text = "스와이프 / 마우스 드래그 / WASD\n스페이스는 전진\n게이트가 다가올 때 맞춰 뛰면 양자 도약";

            toastText = CreateText(safeRoot, "Toast", 42, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.7f));
            Place(toastText.rectTransform, 0.1f, 0.62f, 0.9f, 0.70f);
            toastText.gameObject.SetActive(false);

            collapseRoot = CreateChild(safeRoot, "Collapse");
            var panel = collapseRoot.GetComponent<RectTransform>();
            Place(panel, 0.07f, 0.24f, 0.93f, 0.78f);
            var panelImage = collapseRoot.AddComponent<Image>();
            panelImage.sprite = WhiteSprite();
            panelImage.color = new Color(0.03f, 0.04f, 0.08f, 0.94f);

            Text title = CreateText(panel, "Title", 48, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.75f));
            Place(title.rectTransform, 0.06f, 0.82f, 0.94f, 0.96f);
            title.text = "파동함수 붕괴";

            Text reason = CreateText(panel, "Reason", 28, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.95f));
            Place(reason.rectTransform, 0.08f, 0.72f, 0.92f, 0.82f);
            reason.text = "측정 게이트에 닿았습니다";

            collapseState = CreateText(panel, "State", 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.7f, 0.95f, 1f));
            Place(collapseState.rectTransform, 0.08f, 0.60f, 0.92f, 0.72f);

            collapseScore = CreateText(panel, "Score", 72, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Place(collapseScore.rectTransform, 0.06f, 0.40f, 0.94f, 0.60f);

            collapseDetail = CreateText(panel, "Detail", 28, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.82f, 0.86f, 0.9f));
            Place(collapseDetail.rectTransform, 0.08f, 0.24f, 0.92f, 0.40f);

            var buttonGo = CreateChild(panel, "Restart");
            Place(buttonGo.GetComponent<RectTransform>(), 0.18f, 0.06f, 0.82f, 0.20f);
            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.sprite = WhiteSprite();
            buttonImage.color = new Color(0.25f, 0.9f, 0.95f);
            var button = buttonGo.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(() => onRestart?.Invoke());
            Text buttonLabel = CreateText(buttonGo.transform, "Label", 36, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.05f, 0.08f, 0.12f));
            Stretch(buttonLabel.rectTransform);
            buttonLabel.text = "다시 하기";
            buttonLabel.raycastTarget = false;

            collapseRoot.SetActive(false);
            SetProbability(0.08f);
            SetDistance(0);
            SetPhotons(0);
            ApplySafeArea();
        }

        public void SetProbability(float probability)
        {
            float clamped = Mathf.Clamp01(probability);
            if (probabilityText != null)
                probabilityText.text = "고득점 확률 " + (clamped * 100f).ToString("0.0") + "%";
            if (probabilityFill != null)
            {
                probabilityFill.fillAmount = Mathf.Max(0.02f, clamped);
                probabilityFill.color = Color.Lerp(new Color(0.95f, 0.35f, 0.7f), new Color(0.4f, 1f, 0.95f), clamped);
            }
        }

        public void SetDistance(int distance)
        {
            if (distanceText != null)
                distanceText.text = "진행 " + Mathf.Max(0, distance);
        }

        public void SetPhotons(int count)
        {
            if (photonText != null)
                photonText.text = "광자 " + Mathf.Max(0, count);
        }

        public void ShowQuantumJump(int bonusPhotons)
        {
            if (toastText == null)
                return;
            toastText.text = "양자 도약 +" + bonusPhotons;
            toastText.gameObject.SetActive(true);
            if (toastRoutine != null)
                StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(HideToast());
        }

        public void ShowCollapse(int score, string stateName, float probability, int photons, int distance)
        {
            if (hintRoot != null)
                hintRoot.SetActive(false);
            if (toastText != null)
                toastText.gameObject.SetActive(false);
            if (collapseState != null)
                collapseState.text = "관측된 상태  " + stateName;
            if (collapseScore != null)
                collapseScore.text = score.ToString();
            if (collapseDetail != null)
            {
                collapseDetail.text =
                    "진행 " + distance + "칸  /  광자 " + photons + "\n" +
                    "붕괴 직전 고득점 확률 " + (Mathf.Clamp01(probability) * 100f).ToString("0.0") + "%";
            }

            if (collapseRoot != null)
                collapseRoot.SetActive(true);
        }

        void Update()
        {
            if (safeRoot == null)
                return;
            if (lastSafe != Screen.safeArea)
                ApplySafeArea();
        }

        void ApplySafeArea()
        {
            lastSafe = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0)
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

        IEnumerator HideToast()
        {
            yield return new WaitForSeconds(0.85f);
            if (toastText != null)
                toastText.gameObject.SetActive(false);
            toastRoutine = null;
        }

        Text CreateText(Transform parent, string name, int size, FontStyle style, TextAnchor anchor, Color color)
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

        Image CreateImage(Transform parent, string name, Color color)
        {
            var go = CreateChild(parent, name);
            var image = go.AddComponent<Image>();
            image.sprite = WhiteSprite();
            image.color = color;
            image.raycastTarget = false;
            return image;
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
