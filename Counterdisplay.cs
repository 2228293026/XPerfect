using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityModManagerNet;
using System;
using System.Collections;

namespace XPerfect
{
    public static class CounterDisplay
    {
        private static readonly Color32 PlusMinusColor = new Color32(96, 255, 78, 255);
        private static readonly Color32 XColor = new Color32(77, 204, 255, 255);

        private static string _cachedSpace = "";
        private static int _lastSpacing = -1;

        private static GameObject _canvasObj;
        private static TMP_Text _text;
        private static TMP_FontAsset _cachedFont;


        public static void Create()
        {
            if (_canvasObj != null) return;

            _canvasObj = new GameObject("XPerfect_Canvas");
            UnityEngine.Object.DontDestroyOnLoad(_canvasObj);

            var canvas = _canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = _canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(
                Screen.currentResolution.width,
                Screen.currentResolution.height
            );

            _text = CreateLabel(_canvasObj);

            if (_cachedFont != null)
                _text.font = _cachedFont;
        }

        public static void Destroy()
        {
            if (_canvasObj == null) return;
            UnityEngine.Object.Destroy(_canvasObj);
            _canvasObj = null;
            _text = null;
        }


        private static TMP_Text CreateLabel(GameObject parent)
        {
            var go = new GameObject("CounterText");
            go.transform.SetParent(parent.transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400f, 100f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = Main.Settings.CounterFontSize;
            tmp.text = "0 0 0";
            tmp.richText = true;

            var mat = new Material(tmp.fontSharedMaterial);
            mat.DisableKeyword(TMPro.ShaderUtilities.Keyword_Outline);
            mat.EnableKeyword(TMPro.ShaderUtilities.Keyword_Underlay);
            mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlayOffsetX, 0.5f);
            mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlayOffsetY, -0.5f);
            mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlaySoftness, 0.5f);
            mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlayDilate, 0f);
            mat.SetColor(TMPro.ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 1f));
            tmp.fontSharedMaterial = mat;

            return tmp;
        }

        public static void ApplyFont()
        {
            try
            {
                _cachedFont = FindFont();
                if (_cachedFont == null)
                {
                    return;
                }
                if (_text != null) _text.font = _cachedFont;
            }
            catch (Exception ex)
            {
                UnityModManager.Logger.Log($"[XPerfect] ApplyFont error: {ex}");
            }
        }

        private static TMP_FontAsset FindFont()
        {
            try
            {
                var fontTMP = RDString.fontData.fontTMP;
                if (fontTMP != null)
                {
                    return fontTMP;
                }
            }
            catch (Exception ex)
            {
                UnityModManager.Logger.Log($"[XPerfect] RDString.fontData error: {ex}");
            }

            foreach (var t in UnityEngine.Object.FindObjectsOfType<TMP_Text>())
            {
                if (t == null || t.font == null) continue;
                if (ReferenceEquals(t, _text)) continue;
                if (t.font.name.IndexOf("Liberation", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                return t.font;
            }
            return null;
        }

        public static void ApplySettings()
        {
            if (_text == null) return;

            _text.rectTransform.anchoredPosition = new Vector2(
                Main.Settings.CounterX,
                Main.Settings.CounterY
            );
            _text.fontSize = Main.Settings.CounterFontSize;
        }

        public static void Refresh()
        {
            if (_canvasObj == null) return;

            var ctrl = scrController.instance;
            var cdt = scrConductor.instance;

            bool isPlaying = ctrl != null && cdt != null && !ctrl.paused && cdt.isGameWorld;

            bool visible = Main.Enabled && Main.Settings.ShowCounter && isPlaying;
            _canvasObj.SetActive(visible);
            if (!visible) return;

            string space = GetSpace();

            _text.text =
                $"{ColorText(AccuracyState.PlusPerfectCount.ToString(), PlusMinusColor)}" +
                $"{space}" +
                $"{ColorText(AccuracyState.XPerfectCount.ToString(), XColor)}" +
                $"{space}" +
                $"{ColorText(AccuracyState.MinusPerfectCount.ToString(), PlusMinusColor)}";

            ApplySettings();
        }

        private static string GetSpace()
        {
            int s = Main.Settings.CounterSpacing * 2;
            if (s != _lastSpacing)
            {
                _cachedSpace = new string(' ', s);
                _lastSpacing = s;
            }
            return _cachedSpace;
        }

        private static string ColorText(string text, Color32 color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }
    }

    public class CounterRunner : MonoBehaviour
    {
        private bool _wasPaused = false;

        private void Update()
        {
            var ctrl = scrController.instance;
            if (ctrl == null) return;

            bool isPaused = ctrl.paused;
            if (isPaused != _wasPaused)
            {
                _wasPaused = isPaused;
                CounterDisplay.Refresh();
            }
        }

        private void Awake()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        private void OnSceneChanged(
            UnityEngine.SceneManagement.Scene _,
            UnityEngine.SceneManagement.Scene __)
        {
            StartCoroutine(DelayedRefresh());
        }

        private IEnumerator DelayedRefresh()
        {
            yield return null;
            CounterDisplay.ApplyFont();
            CounterDisplay.Refresh();
        }
    }

    [HarmonyPatch(typeof(scrMistakesManager), "AddHit")]
    public static class CounterRefreshOnHitPatch
    {
        static void Postfix()
        {
            try { CounterDisplay.Refresh(); }
            catch (Exception ex) { UnityModManager.Logger.Log($"[XPerfect] CounterRefresh error: {ex}"); }
        }
    }

    [HarmonyPatch(typeof(scnEditor), "SwitchToEditMode")]
    public static class EditorSwitchToEditModePatch
    {
        static void Postfix()
        {
            try { CounterDisplay.Refresh(); }
            catch (Exception ex) { UnityModManager.Logger.Log($"[XPerfect] SwitchToEditMode error: {ex}"); }
        }
    }

    [HarmonyPatch(typeof(scrController), "Start_Rewind")]
    public static class CounterResetOnStartPatch
    {
        static void Postfix()
        {
            try { CounterDisplay.Refresh(); }
            catch (Exception ex) { UnityModManager.Logger.Log($"[XPerfect] CounterReset error: {ex}"); }
        }
    }
}