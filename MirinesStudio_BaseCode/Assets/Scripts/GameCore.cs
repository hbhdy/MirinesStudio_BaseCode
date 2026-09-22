using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using UnityEngine;

namespace MirinesStudio
{
    public class GameCore : SingletonMono<GameCore>
    {
        // ----- Param -----

        [Header("Base Managers")]
        public List<BaseManager> managerList;

        private Dictionary<Type, BaseManager> dicManagers = new Dictionary<Type, BaseManager>();

        public static ResourceManager RSS => Instance.Get<ResourceManager>();

        public static DataManager DATA => Instance.Get<DataManager>();

        public static UIManager UI => Instance.Get<UIManager>();

        public static SoundManager SOUND => Instance.Get<SoundManager>();
        
        public static LocalizationManager LOCAL => Instance.Get<LocalizationManager>();

        //public static SceneLoadingManager SCENE => Instance.Get<SceneLoadingManager>();

        //public static FirebaseManager FB => Instance.Get<FirebaseManager>();

        //public static GPGSManager GPGS => Instance.Get<GPGSManager>();

        //public static AdMobManager ADMOB => Instance.Get<AdMobManager>();

        //public static IAPManager IAP => Instance.Get<IAPManager>();


        [HideInInspector]
        public static bool isCoreReady = false;

        // ----- Init -----

        public T Get<T>() where T : BaseManager
        {
            var type = typeof(T);
            return dicManagers.ContainsKey(type) ? dicManagers[type] as T : null;
        }

        public async void Start()
        {
            // 프로젝트 기본 내부 설정 처리
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Application.targetFrameRate = 60;
            Time.timeScale = 1;

            dicManagers.Clear();

            for (int i = 0; i < managerList.Count; i++)
            {
                await managerList[i].Task_Init();

                var type = managerList[i].GetType();
                dicManagers.Add(type, managerList[i]);
            }

            StaticGameData.Init();

            for (int i = 0; i < managerList.Count; i++)
                managerList[i].Init();

            isCoreReady = true;
        }

        // ----- Set -----

        // ----- Get -----

        // ----- Main -----

        public void OnApplicationPause(bool pause)
        {

        }

        public void OnApplicationQuit()
        {

        }

        // ----- Logic -----

    }
}