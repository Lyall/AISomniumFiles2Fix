using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(AISomniumFiles2Mod.AISomniumFiles2Fix), "AI: Somnium Files 2", "1.0.0", "Lyall")]
[assembly: MelonGame("SpikeChunsoft", "AI_TheSomniumFiles2")]
namespace AISomniumFiles2Mod
{
    public class AISomniumFiles2Fix : MelonMod
    {
        public static MelonPreferences_Category Fixes;
        public static MelonPreferences_Entry<int> DesiredResolutionX;
        public static MelonPreferences_Entry<int> DesiredResolutionY;
        public static MelonPreferences_Entry<bool> Fullscreen;
        public static MelonPreferences_Entry<bool> UIFix;

        public override void OnApplicationStart()
        {
            LoggerInstance.Msg("Application started.");

            Fixes = MelonPreferences.CreateCategory("AISomnium2Fix");
            Fixes.SetFilePath("Mods/AISomnium2Fix.cfg");
            DesiredResolutionX = Fixes.CreateEntry("Resolution_Width", Display.main.systemWidth, "", "Custom resolution width"); // Set default to something safe
            DesiredResolutionY = Fixes.CreateEntry("Resolution_Height", Display.main.systemHeight, "", "Custom resolution height"); // Set default to something safe
            Fullscreen = Fixes.CreateEntry("Fullscreen", true, "", "Set to true for borderless fullscreen or false for windowed");
            UIFix = Fixes.CreateEntry("UI_Fixes", true, "", "Fixes UI issues at ultrawide/wider");

        }

        [HarmonyPatch]
        public class CustomResolution
        {
            [HarmonyPatch(typeof(Game.LauncherArgs), nameof(Game.LauncherArgs.OnRuntimeMethodLoad))]
            [HarmonyPostfix]
            public static void SetResolution()
            {
                if (!Fullscreen.Value)
                {
                    Screen.SetResolution(DesiredResolutionX.Value, DesiredResolutionY.Value, FullScreenMode.Windowed);
                }
                else
                {
                    Screen.SetResolution(DesiredResolutionX.Value, DesiredResolutionY.Value, FullScreenMode.FullScreenWindow);
                }

                MelonLogger.Msg($"Screen resolution set to {DesiredResolutionX.Value}x{DesiredResolutionY.Value}, Fullscreen = {Fullscreen.Value}");

                // Set mouse cursor to invisible. Why is this not default?
                Cursor.visible = false;
                MelonLogger.Msg("Set cursor to invisible.");
            }
        }

        [HarmonyPatch]
        public class UIFixes
        {
            // Set screen match mode when object has CanvasScaler enabled
            [HarmonyPatch(typeof(CanvasScaler), "OnEnable")]
            [HarmonyPostfix]
            public static void SetScreenMatchMode(CanvasScaler __instance)
            {
                if (UIFix.Value)
                {
                    __instance.m_ScreenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                }  
            }

            // Fix letterboxing to span screen
            [HarmonyPatch(typeof(Game.CinemaScope), "Show")]
            [HarmonyPostfix]
            public static void LetterboxFix(Game.CinemaScope __instance)
            {
                if (UIFix.Value)
                {
                    var GameObjects = GameObject.FindObjectsOfType<Game.CinemaScope>();
                    foreach (var GameObject in GameObjects)
                    {
                        float NewAspectRatio = (float)Screen.width / (float)Screen.height;
                        GameObject.transform.localScale = new Vector3(1 * NewAspectRatio, 1f, 1f);
                    }
                    MelonLogger.Msg("Letterboxing spanned.");
                }
            }
        }        
    }
}