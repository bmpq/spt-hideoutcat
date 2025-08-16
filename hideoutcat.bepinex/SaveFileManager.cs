using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using System;
using System.IO;
using tarkin.hideoutcat.Persistent;
using UnityEngine;

namespace tarkin.hideoutcat.bepinex
{
    internal class SaveFileManager
    {
        private const string ModDirectoryName = "tarkin";
        private const string SaveDirectoryName = "catsaves";
        private static readonly string SavePath;

        static string profileId;

        static SaveFileManager()
        {
            SavePath = Path.Combine(BepInEx.Paths.PluginPath, ModDirectoryName, SaveDirectoryName);
        }

        private static string GetPlayerSaveFilePath()
        {
            if (string.IsNullOrEmpty(profileId))
            {
                // GamePlayerOwner.MyPlayer returns null on hideout load, so using this weird method instead
                profileId = Singleton<ClientApplication<ISession>>.Instance.GetClientBackEndSession().Profile.Id;
                if (string.IsNullOrEmpty(profileId))
                {
                    Debug.LogError("Could not get Player Profile ID. Cannot save or load.");
                    return null;
                }
            }

            return Path.Combine(SavePath, $"{profileId}.json");
        }

        public static void Save(CatSaveData saveData)
        {
            string filePath = GetPlayerSaveFilePath();
            if (filePath == null) return;

            try
            {
                Directory.CreateDirectory(SavePath);

                string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);

                File.WriteAllText(filePath, json);

                Debug.Log($"Successfully saved cat data for profile to: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"FAILED to save cat data: {ex}");
            }
        }

        public static CatSaveData Load()
        {
            string filePath = GetPlayerSaveFilePath();
            if (filePath == null) return null;

            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning("No save file found for this profile. Will start fresh.");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                CatSaveData saveData = JsonConvert.DeserializeObject<CatSaveData>(json);

                Debug.Log($"Successfully loaded cat data for profile from: {filePath}");
                return saveData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"FAILED to load or parse cat data. A new save will be created next time. Error: {ex}");
                return null;
            }
        }
    }
}
