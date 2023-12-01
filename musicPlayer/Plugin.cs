using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using CustomBoomboxTracks.Utilities;
using HarmonyLib;
using musicPlayer;
using MusicPlayer.Manager;
using musicPlayer.Patcher;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;


namespace musicPlayer
{
    
    public static class ConfigSettings
    {
        public static ConfigEntry<string> musicPath;

        public static void BindConfigSettings()
        {
            MusicLogger.logger.LogInfo("Binding the settings");
            musicPath = ((BaseUnityPlugin)Plugin.instance).Config.Bind<string>("musicPlayer", "CustomMusicPath", "music/tellem.wav", "Absolute or local video path. Use forward slashes in your path. Local paths are local to the BepInEx/plugins folder, and should not begin with a slash.");
        }
    }
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;

        private Harmony _harmony;

        public static string filePath;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            instance = this;
            ConfigSettings.BindConfigSettings();
            filePath = ConfigSettings.musicPath.Value;
            if (filePath.Length <= 0 || Enumerable.Contains(filePath, ' '))
            {
                filePath = ((ConfigEntryBase)ConfigSettings.musicPath).DefaultValue.ToString();
            }
            filePath = filePath.Replace('/', Path.DirectorySeparatorChar);
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(Paths.PluginPath, filePath.TrimStart(new char[1] { '/' }));
            }
            Logger.LogInfo("Using configuration from file : " + filePath);
            
            _harmony = new Harmony("musicPlayer");
            _harmony.PatchAll();
            Logger.LogInfo((object)"MusicPlayer is loaded and Harmony initiated");
        }
    }
}
namespace CustomBoomboxTracks.Utilities
{
    public class SharedCoroutineStarter : MonoBehaviour
    {
        private static SharedCoroutineStarter _instance;

        public static Coroutine StartCoroutine(IEnumerator routine)
        {
            //IL_0012: Unknown result type (might be due to invalid IL or missing references)
            if ((Object)(object)_instance == (Object)null)
            {
                _instance = new GameObject("Shared Coroutine Starter").AddComponent<SharedCoroutineStarter>();
                Object.DontDestroyOnLoad((Object)(object)_instance);
            }
            return ((MonoBehaviour)_instance).StartCoroutine(routine);
        }
    }
}


namespace musicPlayer.Patcher
{

    class MusicLogger
    {
        public static BepInEx.Logging.ManualLogSource logger =
            BepInEx.Logging.Logger.CreateLogSource("MusicPlayer");
    }
    
    [HarmonyPatch(typeof(BoomboxItem), "Start")]
    internal class BoomboxItem_Start
    {
        private static void Postfix(BoomboxItem __instance)
        {
            if (AudioManager.finishedLoading)
            {
                MusicLogger.logger.LogInfo("SETTING THE MUSIC");
                AudioManager.SetMusic(__instance);
                return;
            }
            MusicLogger.logger.LogError("Could not Set the music or it has not finished loading");
            AudioManager.OnAllSongsLoaded += delegate
            {
                AudioManager.SetMusic(__instance);
            };
        }
    }
    [HarmonyPatch(typeof(BoomboxItem), "StartMusic")]
    internal class BoomboxItem_StartMusic
    {
        private static void Postfix(BoomboxItem __instance,bool startMusic, bool pitchDown = false)
        {
            if (startMusic)
            {
                MusicLogger.logger.LogInfo("Starting the music...");
                __instance.boomboxAudio.clip = __instance.musicAudios[0];
            }
        }
    }
    
    [HarmonyPatch(typeof(StartOfRound), "Awake")]
    internal class StartOfRound_Awake
    {
        private static void Prefix()
        {
            MusicLogger.logger.LogInfo("LOADING FILES: " + $"{Plugin.filePath}");
            AudioManager.LoadAudio();
        }
    }
}

namespace MusicPlayer.Manager
    {

        internal static class AudioManager
        {

            public static bool finishedLoading = false;
            
            private static List<AudioClip> clips = new List<AudioClip>();

            public static event Action OnAllSongsLoaded;
            
            
            public static void LoadAudio()
            {
                Coroutine item = SharedCoroutineStarter.StartCoroutine(LoadCustomAudioClip());
                SharedCoroutineStarter.StartCoroutine(WaitForClip(item));
            }
            
            
            private static IEnumerator LoadCustomAudioClip()
            {
                MusicLogger.logger.LogInfo("Loading the custom clip...");
                UnityWebRequest loader =
                    UnityWebRequestMultimedia.GetAudioClip($"{Plugin.filePath}",
                        AudioType.WAV);
                
                loader.SendWebRequest();
                while (!loader.isDone)
                {
                    MusicLogger.logger.LogDebug("waiting...");
                    yield return null;
                }

                if (loader.error != null)
                {
                    MusicLogger.logger.LogError(
                        "Error loading clip from path: " + Plugin.filePath + "\n" +
                        loader.error);
                    MusicLogger.logger.LogError(loader.error);
                    yield break;
                }

                AudioClip content = DownloadHandlerAudioClip.GetContent(loader);
                MusicLogger.logger.LogInfo("Loaded " + Plugin.filePath);
                    ((Object) content).name = Path.GetFileName(Plugin.filePath);
                    clips.Add(content);
            }
            
            private static IEnumerator WaitForClip(Coroutine coroutine)
            {
                yield return coroutine;
                finishedLoading = true;
                AudioManager.OnAllSongsLoaded?.Invoke();
                AudioManager.OnAllSongsLoaded = null;
            }

            public static void SetMusic(BoomboxItem __instance)
            {
                __instance.musicAudios = clips.ToArray();
                MusicLogger.logger.LogInfo("Set the music of the Boombox item class");
            }
        }
    }
