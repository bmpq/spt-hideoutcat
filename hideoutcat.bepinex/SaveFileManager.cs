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
        private const string GenericSaveFileName = "unknownprofile.json";
        private static readonly string SavePath;

        static string profileId;

        static SaveFileManager()
        {
            SavePath = Path.Combine(BepInEx.Paths.PluginPath, ModDirectoryName, SaveDirectoryName);
        }

        private static string GetPlayerSaveFilePath()
        {
            if (!string.IsNullOrEmpty(profileId))
            {
                return Path.Combine(SavePath, $"{profileId}.json");
            }

            // GamePlayerOwner.MyPlayer returns null on hideout load, so using this weird method instead
            string fetchedProfileId = Singleton<ClientApplication<ISession>>.Instance?.GetClientBackEndSession()?.Profile?.Id;

            if (!string.IsNullOrEmpty(fetchedProfileId))
            {
                profileId = fetchedProfileId;
                Debug.Log($"Successfully fetched profile ID: {profileId}");

                string genericFilePath = Path.Combine(SavePath, GenericSaveFileName);
                string profileFilePath = Path.Combine(SavePath, $"{profileId}.json");

                // auto migrate generic save to profile-specific save
                if (File.Exists(genericFilePath) && !File.Exists(profileFilePath))
                {
                    try
                    {
                        File.Move(genericFilePath, profileFilePath);
                        Debug.Log($"Migrated generic save file to profile-specific save for ID '{profileId}'.");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to migrate generic save file: {ex}");
                    }
                }

                return profileFilePath;
            }

            Debug.LogWarning("Could not get Player Profile ID. Using generic save file as a fallback.");
            return Path.Combine(SavePath, GenericSaveFileName);
        }

        public static void Save(CatSaveData saveData)
        {
            string filePath = GetPlayerSaveFilePath();

            try
            {
                Directory.CreateDirectory(SavePath);

                string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);

                File.WriteAllText(filePath, json);

                Debug.Log($"Successfully saved cat data for profile to: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"FAILED to save cat data: {ex}");
            }
        }

        public static CatSaveData Load()
        {
            string filePath = GetPlayerSaveFilePath();

            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.Log($"No save file found at '{Path.GetFileName(filePath)}'. Will start fresh.");
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
