using Cysharp.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MirinesStudio
{
    public class LocalizationManager : BaseManager
    {
        // ----- Param -----

        public Language CurLanguage { get; private set; }
        //public Font LocalizeFont { get; private set; }

        private Regex myRegex = new Regex("u002c");
        private string commaText = "u002c";

        public event System.Action OnLanguageChanged;

        // ----- Init -----

        public override async UniTask Task_Init()
        {
            SetLanguage();
            await base.Task_Init();
        }

        public override void Init()
        {
            base.Init();
        }

        // ----- Set -----

        private void SetLanguage()
        {
            int curLanguageIndex = 0;

            curLanguageIndex = Application.systemLanguage switch
            {
                SystemLanguage.Korean => Language.ko.ToInt(),
                SystemLanguage.English => Language.en.ToInt(),
                SystemLanguage.Japanese => Language.ja.ToInt(),
                SystemLanguage.Spanish => Language.es.ToInt(),
                SystemLanguage.Portuguese => Language.pt.ToInt(),
                SystemLanguage.French => Language.fr.ToInt(),
                SystemLanguage.German => Language.de.ToInt(),
                SystemLanguage.Russian => Language.ru.ToInt(),

                _ => 1,
            };

            //en, kr,
            CurLanguage = (Language)curLanguageIndex;
        }

        public void ChangeLanguage(Language language)
        {
            if (CurLanguage == language)
                return;

            CurLanguage = language;

            OnLanguageChanged?.Invoke();
        }

        // ----- Get -----

        public string GetLocalizedString(string key)
        {
            LocalizationMetaData data = GameCore.DATA.GetLocalizationMetaData(key);

            if (data != null)
            {
                string value = CurLanguage switch
                {
                    Language.ko => data.Korean.Trim(),
                    Language.en => data.English.Trim(),
                    _ => string.Empty,
                };

                return string.IsNullOrEmpty(value) ? $" [{CurLanguage.enumName()}] '{key}' is Empty !"
                    : ConvertCommaAndSlash(value);
            }

            return $"'{key}' Not Found !";
        }

        private string ConvertCommaAndSlash(string text)
        {
            string value = text;
            if (value.Contains(commaText))
                value = myRegex.Replace(value, ",");

            return UtilFunction.ReplaceNewLine(value);
        }

        // ----- Main -----

        // ----- Logic -----
    }
}