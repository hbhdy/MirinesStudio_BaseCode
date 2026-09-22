using System;
using System.Reflection;

namespace MirinesStudio
{
    public static class EnumHelper
    {
        public static string enumName(this Enum en) => GetEnum(en);
        public static string GetEnum(Enum en)
        {
            Type type = en.GetType();
            FieldInfo field = type.GetField(en.ToString());
            if (field.GetCustomAttributes(typeof(EnumName), false) is EnumName[] attrs && attrs.Length > 0)
                return attrs[0].Value;

            return en.ToString();
        }
    }

    public class EnumName : Attribute
    {
        private string _value;

        public EnumName(string value) => _value = value;

        public string Value => _value;
    }

    public enum Language
    {
        [EnumName("한국어")] ko = 0,
        [EnumName("English")] en = 1,
        [EnumName("日本語")] ja = 2,
        [EnumName("Español")] es = 3,    // - 스페인어 (es)
        [EnumName("Português")] pt = 4,  // - 포르투갈어 (pt)
        [EnumName("Français")] fr = 5,   // - 프랑스어 (fr)
        [EnumName("Deutsch")] de = 6,    // - 독일어 (de)
        [EnumName("Русский")] ru = 7,    // - 러시아어 (ru)
    }

    public enum TableMetaDataType
    {
        None = 0,

        LocalizationMetaData,

        Count,
    }

    public enum UIType
    {
        None,

        [UIAttrType("Popup/PopupUI_Common")]
        PopupUI_Common,
    }

    public enum Canvas_SortOrder
    {
        [EnumName("화면")] SCREEN = 100,
        [EnumName("허드")] HUD = 1000,
        [EnumName("팝업")] POPUP = 3000,
    }

    public enum AdOutcome
    {
        Completed,      // 보상 조건 충족 (영상 끝까지)
        Skipped,        // 닫힘/스킵 (보상 X)
        NotAvailable,   // 광고 준비 안됨
        NotNetwork,     // 네트워크 연결 안됨
        Failed          // 재생 실패
    }

    public enum GameState
    {
        Lobby,
        InGame,
    }

    public enum DBCollectionName
    {
        UserInfo,
        Payment,
        BlackList,
        VersionInfo,
    }
}
