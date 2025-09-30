using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.Configuration;
using UnityEngine;
using System.Linq;

namespace WhoIsYou
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Date Everything.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        public static ConfigEntry<bool> EnabledByDefault;
        public static ConfigEntry<KeyboardShortcut> ToggleKey;
        public static ConfigEntry<bool> EnableDebug;
        public static ConfigEntry<bool> DisplayUnmetNames;

        public static KeyboardShortcut ToggleKeybindValue => ToggleKey.Value;

        private static bool overlayEnabled = true;
        private static bool lastToggleKeyState = false;
        private static GameObject overlayUI;
        private static OverlayGUI overlayGUI;

        private void Awake()
        {
            Logger = base.Logger;

            EnabledByDefault = Config.Bind(
                "General",
                "EnabledByDefault",
                true,
                "Set to true to enable the overlay by default."
            );

            ToggleKey = Config.Bind(
                "Keybinds",
                "ToggleKey",
                new KeyboardShortcut(KeyCode.BackQuote), // Default ` key
                "Key to toggle the Dateable name overlay."
            );

            EnableDebug = Config.Bind(
                "Debugging",
                "EnableDebug",
                false,
                "When true, logs extra diagnostic information about the target."
            );

            DisplayUnmetNames = Config.Bind(
                "General",
                "DisplayUnmetNames",
                false,
                "When true, also display overlay for Dateables you haven't met yet."
            );

            Harmony.CreateAndPatchAll(typeof(OverlayManagerPatch));
            overlayEnabled = EnabledByDefault.Value;

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is loaded!");
            Logger.LogInfo("Overlay is " + (overlayEnabled ? "ENABLED" : "DISABLED") + " by default");
            Logger.LogWarning("Toggle Key: " + ToggleKey.Value);
            Logger.LogInfo($"Debug mode: {(EnableDebug.Value ? "ON" : "OFF")}");
            Logger.LogInfo($"DisplayUnmetNames: {(DisplayUnmetNames.Value ? "ON" : "OFF")}");
        }

        private static void CreateOverlayUI()
        {
            if (overlayUI != null) return;

            overlayUI = new GameObject("WhoIsYouOverlay");
            overlayGUI = overlayUI.AddComponent<OverlayGUI>();
            Object.DontDestroyOnLoad(overlayUI);
        }

        private static string lastSuppressedInternalName = null;

        public static void UpdateOverlayForInteractable(InteractableObj obj)
        {
            if (!overlayEnabled || obj == null)
            {
                HideOverlay();
                return;
            }

            var dateviators = Singleton<Dateviators>.Instance;
            if (dateviators == null || !dateviators.Equipped)
            {
                HideOverlay();
                return;
            }

            string internalName = "";
            try { internalName = obj.InternalName(); } catch { }

            // If configured not to show Unmet, check date status and bail early
            if (!string.IsNullOrEmpty(internalName) && !DisplayUnmetNames.Value)
            {
                try
                {
                    var save = Singleton<Save>.Instance;
                    if (save != null)
                    {
                        var status = save.GetDateStatus(internalName);
                        if (status == RelationshipStatus.Unmet)
                        {
                            if (EnableDebug.Value && lastSuppressedInternalName != internalName)
                            {
                                Logger.LogInfo($"Suppressing overlay for unmet target '{internalName}' (config DisplayUnmetNames is OFF)");
                                lastSuppressedInternalName = internalName;
                            }
                            HideOverlay();
                            return;
                        }
                    }
                }
                catch { }
            }
            lastSuppressedInternalName = null;

            if (overlayUI == null)
            {
                CreateOverlayUI();
            }

            if (overlayGUI != null)
            {
                string displayName = !string.IsNullOrEmpty(internalName) ? internalName : obj.name;
                overlayGUI.objectName = ToTitleCase(displayName);
                overlayGUI.showOverlay = true;
            }
        }

        private static void HideOverlay()
        {
            if (overlayGUI != null)
            {
                overlayGUI.showOverlay = false;
            }
        }

        public class OverlayGUI : MonoBehaviour
        {
            public string objectName = "";
            public bool showOverlay = false;
            private GUIStyle textStyle;

            // Tunables for sizing
            private const float PaddingX = 24f;   // horizontal padding around text
            private const float PaddingY = 10f;   // vertical padding around text
            private const float TopMargin = 50f;  // distance from the top of the screen
            private const float ScreenMargin = 20f; // min margin from screen edges

            private void Start()
            {
                // Scale font size based on vertical resolution vs. 1080p baseline
                float scale = Mathf.Clamp(Screen.height / 1080f, 0.75f, 2.0f);
                int baseSize = 24;
                int scaledSize = Mathf.RoundToInt(baseSize * scale);
                textStyle = new GUIStyle
                {
                    fontSize = scaledSize,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                textStyle.clipping = TextClipping.Overflow; // keep it single line
            }

            private void OnGUI()
            {
                if (!showOverlay || string.IsNullOrEmpty(objectName)) return;

                // Measure text size for dynamic box
                GUIContent content = new GUIContent(objectName);
                Vector2 textSize = textStyle.CalcSize(content);

                // Compute width/height with padding, clamp to screen with margins
                float maxWidth = Mathf.Max(0f, Screen.width - (ScreenMargin * 2f));
                float boxWidth = Mathf.Min(textSize.x + (PaddingX * 2f), maxWidth);
                float boxHeight = textSize.y + (PaddingY * 2f);

                // Center horizontally at the top
                float x = (Screen.width - boxWidth) * 0.5f;
                float y = TopMargin;

                // Background
                GUI.color = new Color(0, 0, 0, 0.7f);
                GUI.DrawTexture(new Rect(x, y, boxWidth, boxHeight), Texture2D.whiteTexture);

                // Text
                GUI.color = Color.white;
                GUI.Label(new Rect(x, y, boxWidth, boxHeight), content, textStyle);
            }
        }

        private static string ToTitleCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            string[] words = input.Trim().Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (word.Length > 0)
                {
                    char first = char.ToUpperInvariant(word[0]);
                    string rest = word.Length > 1 ? word.Substring(1).ToLowerInvariant() : "";
                    words[i] = first + rest;
                }
            }
            return string.Join(" ", words);
        }

        [HarmonyPatch(typeof(InteractableManager), "Update")]
        public static class OverlayManagerPatch
        {
            public static void Postfix(InteractableManager __instance)
            {
                bool keyDown = ToggleKeybindValue.IsDown();
                if (keyDown && !lastToggleKeyState)
                {
                    overlayEnabled = !overlayEnabled;
                    if (!overlayEnabled)
                        HideOverlay();
                    if (EnableDebug.Value)
                        Logger.LogInfo($"Overlay toggled {(overlayEnabled ? "ON" : "OFF")} by keybind");
                }
                lastToggleKeyState = keyDown;

                // Only show overlay if UI would be shown and equipped
                var obj = __instance.GetActiveObj();
                Plugin.UpdateOverlayForInteractable(obj);
            }
        }
    }
}
