//using Cysharp.Threading.Tasks;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using UnityEngine;

//#if UNITY_ANDROID || UNITY_IOS
//using Firebase.Analytics;
//using Firebase;
//#endif

//namespace MirinesStudio
//{

//    public class AnalyticsManager : BaseManager
//    {
//        // ----- Param -----

//        private FirebaseApp fbApp;

//        // ----- Init -----

//        public override async UniTask Task_Init()
//        {
//            // 다른 파이어베이스 기능 추가가 없다면 여기서 처리
//            await FirebaseApp.CheckAndFixDependenciesAsync();

//            fbApp = FirebaseApp.DefaultInstance;
//            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);


//            await base.Task_Init();
//        }

//        public override void Init()
//        {
//            base.Init();
//        }

//        // ----- Set -----

//        // ----- Get -----

//        // ----- Main -----

//        public void SendLog(string keym, Parameter[] param)
//        {
//            FirebaseAnalytics.LogEvent(keym, param);
//        }

//        // ----- Logic -----

//#if UNITY_ANDROID || UNITY_IOS

//        private void SendLog(string key, Dictionary<string, object> dic)
//        {
//            List<Parameter> parameters = new List<Parameter>();

//            parameters.AddRange(GetBaseParameters());

//#if Dev
//            var sb = new StringBuilder();
//            sb.AppendLine($"[ANALYTICS] Send : {key}");
//            foreach (var item in dic)
//                sb.AppendLine($"{item.Key} / {item.Value}");
//MSLog.Log(sb.ToString());
//#endif

//            using (var iter = dic.GetEnumerator())
//            {
//                while (iter.MoveNext())
//                {
//                    if (iter.Current.Value == null)
//                        parameters.Add(new Parameter(iter.Current.Key, ""));

//                    var _type = iter.Current.Value.GetType();
//                    if (_type.FullName == "System.Int32")
//                    {
//                        int temp;
//                        if (int.TryParse(iter.Current.Value.ToString(), out temp))
//                            parameters.Add(new Parameter(iter.Current.Key, temp));
//                    }
//                    else if (_type.FullName == "System.Int64")
//                    {
//                        long temp;
//                        if (long.TryParse(iter.Current.Value.ToString(), out temp))
//                            parameters.Add(new Parameter(iter.Current.Key, temp));
//                    }
//                    else if (_type.FullName == "System.Double")
//                    {
//                        double temp;
//                        if (double.TryParse(iter.Current.Value.ToString(), out temp))
//                            parameters.Add(new Parameter(iter.Current.Key, temp));
//                    }
//                    else
//                        parameters.Add(new Parameter(iter.Current.Key, iter.Current.Value.ToString()));
//                }
//            }

//            //parameters.AddRange(GetLocalTimeParameters());

//            SendLog(key, parameters.ToArray());
//        }

//        #region BaseParameter

//        private List<Parameter> GetBaseParameters()
//        {
//            var list = new List<Parameter>();
//            list.Add(new Parameter("TS", DateTime.UtcNow.ToString()));
//            list.Add(new Parameter("appVer", Application.version));
//            list.Add(new Parameter("osType", GetOSType()));
//            list.Add(new Parameter("deviceName", SystemInfo.deviceModel));
//            list.Add(new Parameter("language", Application.systemLanguage.ToString()));

//#if Dev
//            list.Add(new Parameter("environment", "Dev"));
//#elif Live
//            list.Add(new Parameter("environment", "Live"));
//#else
//            list.Add(new Parameter("environment", "Etc"));
//#endif

//            return list;
//        }

//        //private List<Parameter> GetLocalTimeParameters()
//        //{
//        //    var list = new List<Parameter>();
//        //    list.Add(new Parameter("CDT", DateTimeOffset.Now.ToUnixTimeSeconds()));
//        //    list.Add(new Parameter("DT", DateTime.Now.ToString("yyyyMMdd")));

//        //    return list;
//        //}

//        private string GetOSType()
//        {
//#if UNITY_ANDROID
//            return "ANDROID";
//#elif UNITY_IOS
//            return "IOS";
//#elif UNITY_WEBGL
//            return "WEBGL";
//#else
//            return "ETC";
//#endif
//        }

//        //        private string GetOSVersion()
//        //        {
//        //            string origin = SystemInfo.operatingSystem;
//        //#if UNITY_ANDROID
//        //            var temp = origin.Split('/');
//        //            for (int i = 0; i < temp.Length; i++)
//        //            {
//        //                if (temp[i].Contains("Android"))
//        //                    return temp[i];
//        //            }

//        //            return origin;
//        //#elif UNITY_IOS
//        //            return origin;
//        //#else
//        //            return origin;
//        //#endif
//        //        }

//        //private string GetLanguageCode()
//        //{
//        //    if (languageCodeDic.ContainsKey(Application.systemLanguage.ToString()))
//        //        return languageCodeDic[Application.systemLanguage.ToString()];

//        //    return "UNKNOWN";
//        //}

//        #endregion BaseParameter

//#endif

//        #region Custom Log

//        public void SendAdsViewLog(AdsViewType type, CatType catType, int stageId)
//        {
//#if UNITY_ANDROID || UNITY_IOS
//            var dictionary = new Dictionary<string, object>();

//            dictionary.Add("TAG", "ADSVIEW");
//            dictionary.Add("TYPE", type.ToString());
//            dictionary.Add("CAT_TYPE", catType.ToString());
//            dictionary.Add("STAGE_ID", stageId);

//            SendLog("ADSVIEW", dictionary);
//#endif
//        }

//        #endregion Custom Log
//    }
//}
