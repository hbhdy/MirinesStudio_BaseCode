using UnityEngine;

#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

namespace MirinesStudio
{
    public static class Vibration
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject activity;
        static AndroidJavaObject vibrator;        // API < 31 직접 사용, API 31+ 는 manager에서 꺼냄
        static AndroidJavaObject vibratorManager; // API 31+
        static AndroidJavaClass  buildVersion;
        static int sdk;

        static void EnsureInited()
        {
            if (buildVersion == null)
            {
                buildVersion = new AndroidJavaClass("android.os.Build$VERSION");
                sdk = buildVersion.GetStatic<int>("SDK_INT");
            }
            if (activity == null)
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
            if (sdk >= 31)
            {
                if (vibratorManager == null)
                {
                    // VibratorManager 경로
                    using var ctx = activity;
                    vibratorManager = ctx.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
                }
                if (vibrator == null && vibratorManager != null)
                {
                    vibrator = vibratorManager.Call<AndroidJavaObject>("getDefaultVibrator");
                }
            }
            else
            {
                if (vibrator == null)
                {
                    using var ctx = activity;
                    vibrator = ctx.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }
        }

        static bool HasVibratorImpl()
        {
            EnsureInited();
            if (vibrator == null) return false;
            return vibrator.Call<bool>("hasVibrator");
        }

        static void VibrateOneShotImpl(long ms, int amplitude /*1~255, -1=default*/)
        {
            EnsureInited();
            if (!HasVibratorImpl()) return;

            if (sdk >= 26)
            {
                // VibrationEffect.createOneShot(ms, amplitude)
                using var veClass = new AndroidJavaClass("android.os.VibrationEffect");
                var effect = veClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude <= 0 ? 255 : amplitude);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                // Deprecated path (still works on legacy)
                vibrator.Call("vibrate", ms);
            }
        }

        static void VibratePatternImpl(long[] pattern) // [wait, vibrate, wait, vibrate...]
        {
            EnsureInited();
            if (!HasVibratorImpl()) return;

            if (sdk >= 26)
            {
                using var veClass = new AndroidJavaClass("android.os.VibrationEffect");
                var effect = veClass.CallStatic<AndroidJavaObject>("createWaveform", pattern, -1);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", pattern, -1);
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void iOSHapticLight();
        [DllImport("__Internal")] static extern void iOSHapticMedium();
        [DllImport("__Internal")] static extern void iOSHapticHeavy();
        [DllImport("__Internal")] static extern void iOSHapticLong();
        [DllImport("__Internal")] static extern void iOSHapticDoubleTap();
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    public static extern void WebGLVibrate(int milliseconds);
#endif
        // ====== Public API ======

        public static void Light()
        {
            if (!StaticGameData.OnVibration)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateOneShotImpl(30, 128);
#elif UNITY_IOS && !UNITY_EDITOR
            iOSHapticLight();
#elif UNITY_WEBGL && !UNITY_EDITOR
            WebGLVibrate(200); // 200ms
#else
            Handheld.Vibrate();
#endif
        }

        public static void Medium()
        {
            if (!StaticGameData.OnVibration)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateOneShotImpl(50, 200);
#elif UNITY_IOS && !UNITY_EDITOR
            iOSHapticMedium();
#elif UNITY_WEBGL && !UNITY_EDITOR
            WebGLVibrate(600); // 200ms
#else
            Handheld.Vibrate();
#endif
        }

        public static void Heavy()
        {
            if (!StaticGameData.OnVibration)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateOneShotImpl(80, 255);
#elif UNITY_IOS && !UNITY_EDITOR
            iOSHapticHeavy();
#elif UNITY_WEBGL && !UNITY_EDITOR
            WebGLVibrate(1000); // 200ms
#else
            Handheld.Vibrate();
#endif
        }

        public static void LongHeavy()
        {
            if (!StaticGameData.OnVibration)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateOneShotImpl(130, 255);
#elif UNITY_IOS && !UNITY_EDITOR
            iOSHapticLong();
#elif UNITY_WEBGL && !UNITY_EDITOR
            WebGLVibrate(1200); // 200ms
#else
            Handheld.Vibrate();
#endif
        }

        public static void DoubleTap()
        {
            if (!StaticGameData.OnVibration)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            VibratePatternImpl(new long[] { 0, 35, 40, 50 });
#elif UNITY_IOS && !UNITY_EDITOR
            iOSHapticDoubleTap();
#elif UNITY_WEBGL && !UNITY_EDITOR
            WebGLVibrate(200); // 200ms
#else
            Handheld.Vibrate();
#endif
        }

        public static bool HasVibrator()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return HasVibratorImpl();
#else
            return true; // 에뮬/기타는 일단 true
#endif
        }

        public static void Cancel()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            EnsureInited();
            vibrator?.Call("cancel");
#endif
        }
    }
}

