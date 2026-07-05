using HarmonyLib;
using MGSC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ItalianTranslation
{
    /// <summary>
    /// Returns the language setting in the game's settings.  This is required as the game will not load any text
    /// if the mod is removed.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.ChangeLang))]
    public static class Localization_ChangeLang__Patch
    {
        public static void Postfix(Localization __instance)
        {
            //Set the player preferences back to English.  Otherwise, if the mod is removed
            //  the game won't load any text.
            PlayerPrefs.SetInt("LocalizationManager.currentLang", (int)Localization.Lang.EnglishUS);
        }
        
    }
}
