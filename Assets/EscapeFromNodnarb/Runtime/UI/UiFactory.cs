using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

namespace EscapeFromNodnarb
{
    public static class UiFactory
    {
        private static Font runtimeFont;

        public static Font RuntimeFont
        {
            get
            {
                if (runtimeFont == null)
                {
                    runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return runtimeFont;
            }
        }

        public static RectTransform CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject safeObject = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safeObject.transform.SetParent(canvasObject.transform, false);
            RectTransform safe = safeObject.GetComponent<RectTransform>();
            Stretch(safe);
            EnsureEventSystem(parent);
            return safe;
        }

        public static RectTransform Panel(Transform parent, string name, Color color, bool raycast = true)
        {
            GameObject value = new GameObject(name, typeof(RectTransform), typeof(Image));
            value.transform.SetParent(parent, false);
            RectTransform rect = value.GetComponent<RectTransform>();
            Stretch(rect);
            Image image = value.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return rect;
        }

        public static Text Label(Transform parent, string name, string value, int size, Color color, TextAnchor alignment)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            Text label = labelObject.GetComponent<Text>();
            label.font = RuntimeFont;
            label.text = value;
            label.fontSize = size;
            label.fontStyle = size >= 28 ? FontStyle.Bold : FontStyle.Normal;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.supportRichText = false;
            label.raycastTarget = false;
            label.resizeTextForBestFit = size >= 18;
            label.resizeTextMinSize = Mathf.Max(12, size - 8);
            label.resizeTextMaxSize = size;
            if (size >= 18)
            {
                Outline outline = labelObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.58f);
                outline.effectDistance = new Vector2(1.0f, -1.0f);
                outline.useGraphicAlpha = true;
            }
            return label;
        }

        public static Button ActionButton(Transform parent, string name, string value, UnityAction action, bool primary)
        {
            return ActionButton(parent, name, value, action, primary, 34);
        }

        public static Button ActionButton(Transform parent, string name, string value, UnityAction action, bool primary, int fontSize)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(TactileButton));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = primary ? GameTheme.SignalButton : GameTheme.SurfaceChrome;
            Outline trim = buttonObject.AddComponent<Outline>();
            trim.effectColor = UiFactory.Alpha(primary ? GameTheme.SignalBright : GameTheme.Rule, primary ? 0.72f : 0.88f);
            trim.effectDistance = new Vector2(1f, -1f);
            trim.useGraphicAlpha = true;
            Button button = buttonObject.GetComponent<Button>();
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            ColorBlock colors = button.colors;
            colors.normalColor = primary ? GameTheme.SignalButton : GameTheme.SurfaceChrome;
            colors.highlightedColor = primary ? Color.Lerp(GameTheme.SignalButtonBright, GameTheme.Text, 0.16f) : Color.Lerp(GameTheme.SurfaceChrome, GameTheme.Text, 0.12f);
            colors.pressedColor = primary ? Color.Lerp(GameTheme.SignalButton, GameTheme.Void, 0.26f) : GameTheme.Surface;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = Color.Lerp(GameTheme.SurfaceGlassDeep, GameTheme.Rule, 0.45f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            Text text = Label(buttonObject.transform, "Text", value, fontSize, primary ? GameTheme.Void : GameTheme.Text, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, new Vector2(24f, 8f), new Vector2(-24f, -8f));
            return button;
        }

        public static Image Rule(Transform parent, string name, Color color)
        {
            GameObject value = new GameObject(name, typeof(RectTransform), typeof(Image));
            value.transform.SetParent(parent, false);
            Image image = value.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        public static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.zero);
        }

        public static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            Anchor(rect, Vector2.zero, Vector2.one, offsetMin, offsetMax);
        }

        public static Rect NormalizedSafeArea(Rect safeArea, int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            Rect clamped = safeArea;
            clamped.xMin = Mathf.Clamp(clamped.xMin, 0f, screenWidth);
            clamped.xMax = Mathf.Clamp(clamped.xMax, clamped.xMin, screenWidth);
            clamped.yMin = Mathf.Clamp(clamped.yMin, 0f, screenHeight);
            clamped.yMax = Mathf.Clamp(clamped.yMax, clamped.yMin, screenHeight);
            return new Rect(
                clamped.xMin / screenWidth,
                clamped.yMin / screenHeight,
                clamped.width / screenWidth,
                clamped.height / screenHeight);
        }

        public static Color Alpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static void EnsureEventSystem(Transform parent)
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventObject.transform.SetParent(parent, false);
        }
    }

    public sealed class TactileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Vector3 baseScale = Vector3.one;

        private void OnEnable()
        {
            transform.localScale = baseScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = NodnarbSettings.ReducedMotionEnabled ? baseScale : baseScale * 0.97f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = baseScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = baseScale;
        }
    }

    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            Apply();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            lastSafeArea = safe;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            RectTransform rect = (RectTransform)transform;
            Rect normalized = UiFactory.NormalizedSafeArea(safe, Screen.width, Screen.height);
            rect.anchorMin = new Vector2(normalized.xMin, normalized.yMin);
            rect.anchorMax = new Vector2(normalized.xMax, normalized.yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
