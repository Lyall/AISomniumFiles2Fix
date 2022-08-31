using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(AISomniumFiles2Mod.AISomniumFiles2Fix), "AI: Somnium Files 2", "1.0.6", "Lyall")]
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
        public static MelonPreferences_Entry<bool> IncreaseQuality;
        public static MelonPreferences_Entry<bool> bDisableMouseCursor;

        public override void OnApplicationStart()
        {
            LoggerInstance.Msg("Application started.");

            Fixes = MelonPreferences.CreateCategory("AISomnium2Fix");
            Fixes.SetFilePath("Mods/AISomnium2Fix.cfg");
            DesiredResolutionX = Fixes.CreateEntry("Resolution_Width", Display.main.systemWidth, "", "Custom resolution width"); // Set default to something safe
            DesiredResolutionY = Fixes.CreateEntry("Resolution_Height", Display.main.systemHeight, "", "Custom resolution height"); // Set default to something safe
            Fullscreen = Fixes.CreateEntry("Fullscreen", true, "", "Set to true for fullscreen or false for windowed");
            UIFix = Fixes.CreateEntry("UI_Fixes", true, "", "Fixes UI issues at ultrawide/wider");
            IncreaseQuality = Fixes.CreateEntry("IncreaseQuality", true, "", "Increase graphical quality."); // 
            bDisableMouseCursor = Fixes.CreateEntry("DIsableMouseCursor", true, "", "Set to true to force the mouse cursor to be invisible.");
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
            }
        }

        [HarmonyPatch]
        public class UIFixes
        {
            public static float NativeAspectRatio = (float)16 / 9;
            public static float NewAspectRatio = (float)DesiredResolutionX.Value / DesiredResolutionY.Value;
            public static float AspectMultiplier = NewAspectRatio / NativeAspectRatio;

            // Set screen match mode when object has CanvasScaler enabled
            [HarmonyPatch(typeof(CanvasScaler), "OnEnable")]
            [HarmonyPostfix]
            public static void SetScreenMatchMode(CanvasScaler __instance)
            {
                if (NewAspectRatio > NativeAspectRatio && UIFix.Value)
                {
                    __instance.m_ScreenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                }  
            }

            // Fix letterboxing to span screen
            [HarmonyPatch(typeof(Game.CinemaScope), "Show")]
            [HarmonyPostfix]
            public static void LetterboxFix()
            {
                if (NewAspectRatio > NativeAspectRatio && UIFix.Value)
                {
                    var GameObjects = GameObject.FindObjectsOfType<Game.CinemaScope>();
                    foreach (var GameObject in GameObjects)
                    {
                        
                        GameObject.transform.localScale = new Vector3(1 * AspectMultiplier, 1f, 1f);
                    }
                    MelonLogger.Msg("Letterboxing spanned.");
                }
            }

            // Fix filters to span screen
            // This is jank but I can't think of a better solution right now.
            [HarmonyPatch(typeof(Game.FilterController), "Black")]
            [HarmonyPatch(typeof(Game.FilterController), "FadeIn")]
            [HarmonyPatch(typeof(Game.FilterController), "FadeInWait")]
            [HarmonyPatch(typeof(Game.FilterController), "FadeOut")]
            [HarmonyPatch(typeof(Game.FilterController), "FadeOutWait")]
            [HarmonyPatch(typeof(Game.FilterController), "Flash")]
            [HarmonyPatch(typeof(Game.FilterController), "Set")]
            [HarmonyPatch(typeof(Game.FilterController), "SetValue")]
            [HarmonyPostfix]
            public static void FilterFix()
            {
                if (NewAspectRatio > NativeAspectRatio && UIFix.Value)
                {
                    var GameObjects = GameObject.FindObjectsOfType<Game.FilterController>();
                    foreach (var GameObject in GameObjects)
                    {

                        GameObject.transform.localScale = new Vector3(1 * AspectMultiplier, 1f, 1f);
                    }
                    // Log spam
                    //MelonLogger.Msg("Filter spanned.");
                }
            }

            // Fix eye fade filter
            [HarmonyPatch(typeof(Game.EyeFadeFilter), "FadeIn")]
            [HarmonyPatch(typeof(Game.EyeFadeFilter), "FadeInWait")]
            [HarmonyPatch(typeof(Game.EyeFadeFilter), "FadeOut")]
            [HarmonyPatch(typeof(Game.EyeFadeFilter), "FadeOutWait")]
            [HarmonyPostfix]
            public static void EyeFadeFilterFix()
            {
                if (NewAspectRatio > NativeAspectRatio && UIFix.Value)
                {
                    var GameObjects = GameObject.FindObjectsOfType<Game.EyeFadeFilter>();
                    foreach (var GameObject in GameObjects)
                    {

                        GameObject.transform.localScale = new Vector3(1 * AspectMultiplier, 1f, 1f);
                    }
                    // Log spam
                    //MelonLogger.Msg("EyeFade filter spanned.");
                }
            }

            // Scale cutscene viewport when video plays
            [HarmonyPatch(typeof(Game.VideoController), nameof(Game.VideoController.Prepare))]
            [HarmonyPostfix]
            public static void FixCutsceneViewport(Game.VideoController __instance)
            {
                if (NewAspectRatio > NativeAspectRatio)
                {
                    var cutsceneImage = __instance.world.Image;
                    cutsceneImage.transform.localScale = new Vector3(1 / AspectMultiplier, 1f, 1f);
                    MelonLogger.Msg("Cutscene viewport scaled horizontally.");
                }
            }

            // Reset cutscene viewport after video ends
            [HarmonyPatch(typeof(Game.VideoController), nameof(Game.VideoController.Stop))]
            [HarmonyPostfix]
            public static void ResetCutsceneViewport(Game.VideoController __instance)
            {
                if (NewAspectRatio > NativeAspectRatio)
                {
                    var cutsceneImage = __instance.world.Image;
                    cutsceneImage.transform.localScale = new Vector3(1f, 1f, 1f);
                    MelonLogger.Msg("Cutscene viewport scale reset to default.");
                }
            }
        }

        [HarmonyPatch]
        public class QualityPatches
        {
            // Enable high-quality SMAA for all cameras
            [HarmonyPatch(typeof(Game.CameraController), "OnEnable")]
            [HarmonyPostfix]
            public static void CameraQualityFix(Game.CameraController __instance)
            {
                if (IncreaseQuality.Value)
                {
                    var UACD = __instance._camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                    UACD.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    UACD.antialiasingQuality = UnityEngine.Rendering.Universal.AntialiasingQuality.High;
                    MelonLogger.Msg("Camera set to SMAA High.");
                }
            }
        }

        [HarmonyPatch]
        public class CursorPatches
        {
            // Mouse cursor visibility
            [HarmonyPatch(typeof(UnityEngine.Cursor), nameof(UnityEngine.Cursor.visible), MethodType.Setter)]
            [HarmonyPrefix]
            public static void CameraQualityFix(UnityEngine.Cursor __instance, ref bool __0)
            {
                if (bDisableMouseCursor.Value)
                {
                    __0 = false;
                    // Log Spam
                    // Seems to set cursor visibility a lot, :|
                    //MelonLogger.Msg("Forced mouse cursor to be invisible.");
                }
            }
        }
    }
}