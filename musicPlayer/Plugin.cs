using BepInEx;
using HarmonyLib;
using UnityEngine;
namespace musicPlayer
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;

        private Harmony _harmony;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            _harmony = new Harmony("musicPlayer");
            _harmony.PatchAll();
            Logger.LogInfo((object)"MusicPlayer is loaded and Harmony initiated");
            instance = this;
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
        [HarmonyPostfix]
        public static void StartMusic_Prefix(BoomboxItem __instance,
            bool startMusic, bool pitchDown = false)
        {
            MusicLogger.logger.LogInfo("Doing some sound !!");
            if (startMusic)
            {
                // Replace the audio clip assignment with your custom logic
                __instance.boomboxAudio.clip = LoadCustomAudioClip();
                __instance.boomboxAudio.pitch = 1f;
                __instance.boomboxAudio.Play();
            }
        }

        private static AudioClip LoadCustomAudioClip()
        {
            string audioFile = "assets/tellem.wav";
            
            WWW www = new WWW("file://" + audioFile);
            MusicLogger.logger.LogInfo("File : [ " + www +  " ]");
            AudioClip audioClip = www.GetAudioClip(false, false, AudioType.WAV);
            
            return audioClip;
        }
    }
}
