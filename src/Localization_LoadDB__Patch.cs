using HarmonyLib;
using JetBrains.Annotations;
using MGSC;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static MGSC.Localization;

namespace ItalianTranslation
{
    /// <summary>
    /// Adds the language to the game's database.  
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.LoadDB))]
    public static class Localization_LoadDB__Patch
    {

        /// <summary>
        /// The unique ID for the custom language.  This is a high value to avoid conflicts with the existing enum values.
        /// </summary>
        public const Localization.Lang CustomLanguageId = (Localization.Lang)200;

        public static void Postfix(Localization __instance)
        {
            LoadTranslations(__instance);

            //Force to overwrite the player prefs. 
            //  This is more of a niciety so the user doesn't have to go to the language settings on first run.
            //  TODO:  Verify - If the mod is removed, I think it will keep reverting to English and throw errors.
            PlayerPrefs.SetInt("LocalizationManager.currentLang", (int)CustomLanguageId);
            __instance.currentLang = CustomLanguageId;

            if(Plugin.Config.ExportData)
            {
                try
                {
                    LocalizationExport(Path.Combine(Plugin.ConfigDirectories.ModPersistenceFolder, "localization_export.tsv"));
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError(ex, "Error exporting localization data");
                }
            }

        }

        /// <summary>
        /// Writes out the localization 
        /// </summary>
        /// <param name="outputFileName"></param>
        private static void LocalizationExport(string outputFileName)
        {
            TextAsset textAsset = Resources.Load("localization") as TextAsset;
            WriteIfDifferent(outputFileName, textAsset.text);
        }

        /// <summary>
        /// Writes the file if the data is different from what is already on disk.
        /// </summary>
        private static void WriteIfDifferent(string exportFilePath, string output)
        {
            if (!File.Exists(exportFilePath) || File.ReadAllText(exportFilePath) != output)
            {
                File.WriteAllText(exportFilePath, output);
            }
        }


        /// <summary>
        /// Loads the localization file and adds it  to the game's localization db.
        /// </summary>
        /// <param name="localization"></param>
        public static void LoadTranslations(Localization localization)
        {
            Dictionary<Localization.Lang, Dictionary<string, string>> db = localization.db;

            Dictionary<string, string> translationItems = GetFileData(Path.Combine(Plugin.ConfigDirectories.ModAssemblyPath, "localization.tsv"), localization);

            db[CustomLanguageId] = translationItems;

            //--- Set the language to use the default font
            SingletonMonoBehaviour<LocalizationFontKeeper>.Instance.FontPresets
                .DoIf(
                    x => x.AvaialableLangs.Contains(Localization.Lang.EnglishUS),
                    x => x.AvaialableLangs.Add(CustomLanguageId)
                    );
        }

        /// <summary>
        /// Loads the translation file from a TSV file and returns a dictionary of the translation items.
        /// Adds the English version of the localiation if any translations are missing.
        /// </summary>
        /// <param name="file">The TSV file path containing string key and string text</param>
        /// <param name="localization">The localization object containing the translation data</param>
        /// <returns>A dictionary containing the translation items</returns>

        private static Dictionary<string, string> GetFileData(string file, Localization localization)
        {
            var translationLookup = new Dictionary<string, string>();

            if (!File.Exists(file))
            {
                throw new ApplicationException($"Translation file not found: {file}");
            }

            using (var reader = new System.IO.StreamReader(file))
            {
                int lineNumber = 0;

                try
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNumber++;

                        int tabIndex = line.IndexOf('\t');
                        if (tabIndex == -1)
                        {
                            throw new ApplicationException($"Did not find the key column. " +
                                $"No tab was found. Line text: '{line}'");
                        }

                        string key = line.Substring(0, tabIndex);
                        string value = line.Substring(tabIndex + 1);
                        translationLookup[key] = value;
                    }

                }
                catch (Exception ex)
                {
                    throw new ApplicationException($"Error reading translation file. Line: {lineNumber}", ex);
                }

            }

            //--Check for any missing translations and add the English version if missing
            Dictionary<string, string> englishItems = localization.db[Localization.Lang.EnglishUS];

            englishItems
                .DoIf(x =>
                    !translationLookup.TryGetValue(x.Key, out string text),
                    x =>
                    {
                        //Log empty *only* if the text is not empty or the key is blank.  The game's translation data
                        //  doesn't have a column name for the key, so ignore it.

                        //  NOTE: Still adding the key to the localization db since the game will show 
                        //  an error if the key is missing.
                        if(!string.IsNullOrEmpty(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
                        {
                            Plugin.Logger.LogWarning($"Missing translation for key '{x.Key}'.  Using English version.");
                        }

                        translationLookup.Add(x.Key, x.Value);
                    }
                );

            return translationLookup;
        }
    }
}
