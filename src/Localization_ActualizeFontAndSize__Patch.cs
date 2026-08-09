using HarmonyLib;
using MGSC;
using TMPro;
using static MGSC.Localization;

namespace ItalianTranslation
{
    /// <summary>
    /// Adds the font info for the language. 
    /// The font has to be set here since other mods have compatibiliity issues otherwise.
    /// </summary>
    [HarmonyPatch(typeof(Localization), nameof(Localization.ActualizeFontAndSize), 
        typeof(TextMeshProUGUI), typeof(Lang), typeof(TextContext))]
    public static class Localization_ActualizeFontAndSize__Patch
    { 

        private static bool Inited = false;


        public static void Prefix()
        {
            if (Inited) return;
            Inited = true;  

            //---Set the language to use the default font
            SingletonMonoBehaviour<LocalizationFontKeeper>.Instance.FontPresets
                .DoIf(
                    x => x.AvaialableLangs.Contains(Localization.Lang.EnglishUS),
                    x => x.AvaialableLangs.Add(Localization_LoadDB__Patch.CustomLanguageId)
                    );
        }

    }

}
