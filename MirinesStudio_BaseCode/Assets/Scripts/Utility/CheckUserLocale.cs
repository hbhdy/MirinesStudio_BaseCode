#if UNITY_IOS
using System;
using System.IO;
using System.Runtime.InteropServices;
#endif

using UnityEngine;

namespace MirinesStudio
{
    public abstract class CheckUserLocale
    {
        public string _country { get; protected set; } = string.Empty;

        public abstract void Init();
    }

    public class CheckUserLocale_Editor : CheckUserLocale
    {
        public override void Init()
        {

        }
    }

#if UNITY_ANDROID

    public class CheckUserLocale_AOS : CheckUserLocale
    {
        public override void Init()
        {
            using (AndroidJavaClass javaClass = new AndroidJavaClass("java.util.Locale"))
            {
                if (javaClass != null)
                {
                    using (AndroidJavaObject locale = javaClass.CallStatic<AndroidJavaObject>("getDefault"))
                    {
                        if (locale != null)
                        {
                            var localeVal = locale.Call<string>("getCountry");
                            _country = localeVal;
                        }
                        else
                            Debug.Log("Android java object locale is NULL");
                    }
                }
                else
                {
                    Debug.Log("Android Java Class is NULL");
                }
            }
        }
    }

#endif

#if UNITY_IOS
    public class CheckUserLocale_IOS : CheckUserLocale
    {
        [DllImport("__Internal")]
        private static extern string IOSgetPhoneCountryCode();

        public override void Init()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _country = IOSgetPhoneCountryCode();
#else
            _country = "US"; // 에디터용 기본값
#endif
        }
    }

    // iOS에서는 MyPluginClass 필요 없음, 과감히 삭제해도 됩니다.
#endif
}