using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MirinesStudio
{
    public static class StaticGameData
    {
        // ----- Config --------------------------------------------------------------------------------------------------------

        // ----- UserInfo ------------------------------------------------------------------------------------------------------

        public static bool OnBGM = true;
        public static bool OnSFX = true;
        public static bool OnVibration = false;
        public static bool OnReview = false;

        // ----- Other ---------------------------------------------------------------------------------------------------------

        public static GameState GameState = GameState.Lobby;

        public static readonly string ConvertCommaText = "u002c";

        // ----- Init -----

        public static bool isInIt = false;

        public static void Init()
        {
            GameConfig.Get(GameConfig.BGM, ref OnBGM);
            GameConfig.Get(GameConfig.SFX, ref OnSFX);
            GameConfig.Get(GameConfig.VIBRATION, ref OnVibration);
            GameConfig.Get(GameConfig.REVIEW, ref OnReview);

            isInIt = true;
        }

        // ----- Set -----

        // ----- Get -----

        // ----- Logic -----

        public static float RandRoll() => UnityEngine.Random.value;

        public static Vector2 GetWorldToScreenPoint(Vector3 pos) => GameCore.UI.GetMainCam().WorldToScreenPoint(pos);

        public static Vector3 GetScreenToViewportPoint(Vector3 pos) => GameCore.UI.GetMainCam().ScreenToViewportPoint(pos);

        // 리스트에서 하나 랜덤 뽑아 제거
        private static T PopRandom<T>(List<T> list)
        {
            int r = UnityEngine.Random.Range(0, list.Count);
            var v = list[r];
            list.RemoveAt(r);
            return v;
        }

        // ----- Permission ------------------------------------------------------------------------------------------------------

        //public const string POST_NOTIFICATIONS = "android.permission.POST_NOTIFICATIONS";

        //public static void RequestPermission(string permissionName)
        //{
        //    if (UtilFunction.AndroidOsBuild.SDK_INT >= 33)
        //    {
        //        if (!Permission.HasUserAuthorizedPermission(permissionName))
        //        {
        //            var callbacks = new PermissionCallbacks();
        //            callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;                                 // 권한 거부됨
        //            callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;                               // 권한이 부여됨
        //            callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;   // 권한이 거부되었으며 다시 묻지 않음
        //            Permission.RequestUserPermission(permissionName, callbacks);
        //        }
        //    }
        //    else
        //        SetPushNotification(true);
        //}

        //// 권한 거부됨
        //private static void PermissionCallbacks_PermissionDenied(string permissionName)
        //{
        //    MSLog.Log($"{permissionName} PermissionCallbacks_PermissionDenied", LogColor.yellow);
        //    switch (permissionName)
        //    {
        //        case POST_NOTIFICATIONS:
        //            SetPushNotification(false);
        //            break;
        //    }
        //}

        //// 권한이 부여됨
        //private static void PermissionCallbacks_PermissionGranted(string permissionName)
        //{
        //    MSLog.Log($"{permissionName} PermissionCallbacks_PermissionGranted", LogColor.cyan);
        //    switch (permissionName)
        //    {
        //        case POST_NOTIFICATIONS:
        //            SetPushNotification(true);
        //            break;
        //    }
        //}

        //// 권한 설정이 거부되었으며 다시 묻지 않음
        //private static void PermissionCallbacks_PermissionDeniedAndDontAskAgain(string permissionName)
        //{
        //    MSLog.Log($"{permissionName} PermissionCallbacks_PermissionDeniedAndDontAskAgain", LogColor.red);
        //    switch (permissionName)
        //    {
        //        case POST_NOTIFICATIONS:
        //            SetPushNotification(false);
        //            break;
        //    }
        //}

        //public static void SetPushNotification(bool state)
        //{
        //    GameConfig.Set(GameConfig.PUSH, state);
        //    MSLog.Log($" OnPush :: {state}", LogColor.lightblue);
        //}

    }
}