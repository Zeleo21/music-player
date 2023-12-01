using System.Collections;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using musicPlayer.Patcher;
using UnityEngine;
using UnityEngine.Networking;


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

namespace musicPlayer.Patcher
{

    class MusicLogger
    {
        public static BepInEx.Logging.ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource("MusicPlayer");
    }

    [HarmonyPatch]
    internal class MusicPlayer
    {
        [HarmonyPatch(typeof(BoomboxItem), "StartMusic")]
        [HarmonyPrefix]
        public static bool StartMusic_Prefix(BoomboxItem __instance,
            bool startMusic, bool pitchDown = false)
        {
            if (startMusic)
            {
                // Replace the audio clip assignment with your custom logic
                MusicLogger.logger.LogInfo("Doing some sound !!");

                __instance.boomboxAudio.clip = LoadCustomAudioClip();
                __instance.boomboxAudio.pitch = 1f;
                __instance.boomboxAudio.Play();
            }

            return false;
        }

        private static AudioClip LoadCustomAudioClip()
        {
            WWW www = new WWW($"file://{Plugin.filePath}");
            MusicLogger.logger.LogInfo(www.url);
            AudioClip audioClip = www.GetAudioClip(false, false, AudioType.WAV);
            return audioClip;
        }
    }
}
