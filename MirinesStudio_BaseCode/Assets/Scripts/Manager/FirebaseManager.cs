//using Cysharp.Threading.Tasks;
//using Firebase;
//using Firebase.Extensions;
//using Firebase.Firestore;
////using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using UnityEngine;

//namespace MirinesStudio
//{
//    public class FirebaseManager : BaseManager
//    {
//        // ----- Param -----

//        private Dictionary<string, string> countryCodeDic = new Dictionary<string, string>();

//        private FirebaseApp fbApp;
//        private FirebaseFirestore fbStore;
//        private CheckUserLocale _userLocale = null;
//        public CheckUserLocale UserLocale => _userLocale;

//        // ----- Init -----

//        public override async UniTask Task_Init()
//        {
//            await FirebaseApp.CheckAndFixDependenciesAsync();

//            fbApp = FirebaseApp.DefaultInstance;
//            fbStore = FirebaseFirestore.DefaultInstance;

//            SetCountryCode();

//#if UNITY_EDITOR
//            _userLocale = new CheckUserLocale_Editor();
//            //#elif UNITY_IOS
//            //          _userLocale = new CheckUserLocale_IOS();
//#elif UNITY_ANDROID
//            _userLocale = new CheckUserLocale_AOS();
//#endif

//            await base.Task_Init();
//        }

//        public override void Init()
//        {
//            base.Init();
//        }

//        public async UniTask<bool> UpdateDBAsync(DBCollectionName dbName, string userID, Dictionary<string, object> updateData, bool upsert = false, CancellationToken ct = default)
//        {
//            DocumentReference docRef = fbStore.Collection(dbName.ToString()).Document(userID);
//            updateData["UpdateTime"] = FieldValue.ServerTimestamp;

//            try
//            {
//                // 광고 시청뒤 바로 파이어베이스 DB 로직 호출시 앱 터짐 
//                // 그것을 방지하고자 추가 처리함 ( useCurrentSynchronizationContext : false ) 
//                Task task;

//                if (upsert)
//                {
//                    task = docRef.SetAsync(updateData, SetOptions.MergeAll);
//                    //await docRef.SetAsync(updateData, SetOptions.MergeAll).AsUniTask().AttachExternalCancellation(ct);
//                }
//                else
//                {
//                    task = docRef.UpdateAsync(updateData);
//                    //await docRef.UpdateAsync(updateData).AsUniTask().AttachExternalCancellation(ct);
//                }

//                await task.AsUniTask(useCurrentSynchronizationContext: false).AttachExternalCancellation(ct);

//                MSLog.Log("[Firestore] UpdateDBAsync success");
//                return true;
//            }
//            catch (FirestoreException e)
//            {
//                MSLog.LogError($"[Firestore] UpdateDBAsync FirestoreException: {e.ErrorCode} {e.Message}");
//                return false;
//            }
//            catch (System.Exception e)
//            {
//                MSLog.LogError($"[Firestore] UpdateDBAsync Exception: {e}");
//                return false;
//            }
//        }

//        #region Version Check

//        // 버전 체크
//        public async UniTask<string> FetchVersionPassAsync()
//        {
//            var docRef = fbStore.Collection(DBCollectionName.VersionInfo.ToString()).Document("Version");
//            var snap = await docRef.GetSnapshotAsync();
//            if (!snap.Exists)
//                return string.Empty;

//            var info = snap.ConvertTo<VersionInfo>();

//            return info.Current;
//        }

//        #endregion Version Check

//        #region BlackList
//        // 블랙리스트 정보 가저오기
//        public async UniTask<BlackList> FetchBlackListAsync(string userID)
//        {
//            Query qry = fbStore.Collection(DBCollectionName.BlackList.ToString()).WhereEqualTo("UserID", userID).WhereGreaterThan("ExpireDate", DateTime.UtcNow.AddHours(9));
//            QuerySnapshot snapShot = await qry.GetSnapshotAsync();

//            if (snapShot.Count == 0)
//                return null;

//            foreach (DocumentSnapshot doc in snapShot.Documents)
//            {
//                BlackList list = doc.ConvertTo<BlackList>();

//                return list;
//            }

//            return null;
//        }
//        #endregion BlackList

//        #region UserInfo
//        public async UniTask<UserInfo> FetchUserInfoAsync(DBCollectionName dbName, string userID)
//        {
//            var docRef = fbStore.Collection(dbName.ToString()).Document(userID);
//            var snap = await docRef.GetSnapshotAsync();
//            if (!snap.Exists)
//                return null;

//            var info = snap.ConvertTo<UserInfo>();

//            return info;
//        }

//        // UserInfo 객체 데이터를 저장
//        public void SaveUserInfo(string collectionName, UserInfo userData)
//        {
//            DocumentReference docRef = fbStore.Collection(collectionName).Document(userData.ID);

//            docRef.SetAsync(userData).ContinueWithOnMainThread(task =>
//            {
//                if (task.IsFaulted)
//                {
//                    MSLog.Log("SaveData Fail");
//                }
//                if (task.IsCanceled)
//                {
//                    MSLog.Log("SaveData Cancel");
//                }

//                MSLog.Log("SaveData Complete");
//            });
//        }

//        public string GetCountryCode()
//        {
//            string code = GetCountryCode1(); // OS 로케일
//            if (code.Equals("UNKNOWN"))
//            {
//                code = GetCountryCode2();
//                if (code.Equals("UNKNOWN"))
//                {
//                    code = GetCountryCode3();
//                }
//            }
//            if (string.IsNullOrEmpty(code))
//                code = "UNKNOWN";

//            return code.ToUpper();
//        }


//        public string GetCountryCode1()
//        {
//            try
//            {
//                Debug.Log("== GetCountryCode1");
//                var locale = UserLocale;

//                var countryCode = locale._country.ToUpper();
//                if (countryCodeDic.ContainsKey(countryCode))
//                    return countryCodeDic[countryCode];
//                else
//                    return "UNKNOWN";
//            }
//            catch (Exception e)
//            {
//                MSLog.LogError($"FirebaseManager> fail to GetCountryCode..\n{e.ToString()}");
//                return "UNKNOWN";
//            }
//        }

//        public static string GetCountryCode2()
//        {
//            Debug.Log("== GetCountryCode2");
//#if UNITY_ANDROID
//            try
//            {
//                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
//                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
//                using (var context = currentActivity.Call<AndroidJavaObject>("getApplicationContext"))
//                using (var resources = context.Call<AndroidJavaObject>("getResources"))
//                using (var configuration = resources.Call<AndroidJavaObject>("getConfiguration"))
//                using (var locale = configuration.Get<AndroidJavaObject>("locale"))
//                {
//                    string country = locale.Call<string>("getCountry");
//                    if (string.IsNullOrEmpty(country))
//                    {
//                        country = locale.Call<string>("toString"); // 예: "ko_KR"
//                        if (!string.IsNullOrEmpty(country))
//                            country = country.Substring(country.Length - 2, 2);
//                    }
//                    Debug.Log($"== Android CountryCode: {country}");
//                    return string.IsNullOrEmpty(country) ? "UNKNOWN" : country;
//                }
//            }
//            catch (System.Exception e)
//            {
//                Debug.LogWarning("GetCountryCode2() failed: " + e.Message);
//                return "UNKNOWN";
//            }
//#else
//            return Application.systemLanguage.ToString();
//#endif
//        }

//        public string GetCountryCode3()
//        {
//            Debug.Log("== GetCountryCode3");

//            SystemLanguage lang = Application.systemLanguage;
//            switch (lang)
//            {
//                case SystemLanguage.English: return "US";
//                case SystemLanguage.Korean: return "KR";
//                case SystemLanguage.Japanese: return "JP";
//                default:
//                    return "UNKNOWN";
//            }
//        }

//        #endregion UserInfo
 


//        #region Payment

//        // 새로 구매한 캐쉬 아이템 저장
//        public async UniTask<bool> SavePaymentAsync(Payment purchaseItem, CancellationToken ct = default)
//        {
//            if (purchaseItem == null)
//                return false;

//            DocumentReference docRef = fbStore.Collection(DBCollectionName.Payment.ToString()).Document(purchaseItem.OrderID);

//            if (purchaseItem == null)
//                return false;
//            purchaseItem.Status = 1;

//            try
//            {
//                await docRef.SetAsync(purchaseItem, SetOptions.MergeAll).AsUniTask().AttachExternalCancellation(ct);
//                MSLog.Log($"[SavePaymentAsync] success. UserID: {purchaseItem.UserID}, OrderID:{purchaseItem.OrderID}, ProductID: {purchaseItem.ProductID} ");
//                return true;
//            }
//            catch (FirestoreException e)
//            {
//                MSLog.LogError($"[SavePaymentAsync] FirestoreException: {e.ErrorCode} {e.Message}");
//                return false;
//            }
//            catch (System.Exception e)
//            {
//                MSLog.LogError($"[SavePaymentAsync] Exception: {e}");
//                return false;
//            }
//        }

//        // 구매 상태 변경 ( 1 : 최초 등록, 0:아이템 지급 완료)
//        public async UniTask<bool> UpdatePaymentAsync(string userID, string orderID, int status, CancellationToken ct = default)
//        {
//            MSLog.Log($"[UpdatePaymentAsync] Begin. UserID: {userID}, OrderID: {orderID}, Updated status {status}");

//            DocumentReference docRef = fbStore.Collection(DBCollectionName.Payment.ToString()).Document(orderID);

//            Dictionary<string, object> updates = new Dictionary<string, object>
//            {
//                { "Status", status },
//                { "UpdateTime", FieldValue.ServerTimestamp}
//            };

//            try
//            {
//                await docRef.UpdateAsync(updates).AsUniTask().AttachExternalCancellation(ct);

//                MSLog.Log($"[UpdatePaymentAsync] success. UserID:{userID}, OrderID:{orderID}, Status: {status}");
//                return true;
//            }
//            catch (FirestoreException e)
//            {
//                MSLog.LogError($"[UpdatePaymentAsync] FirestoreException: {e.ErrorCode} {e.Message}");
//                return false;
//            }
//            catch (System.Exception e)
//            {
//                MSLog.LogError($"[UpdatePaymentAsync] Exception: {e}");
//                return false;
//            }
//        }

//        //// true: 광고 제거를 사용중
//        //public async UniTask<bool> FetchNoAdPaymentAsync(string userID)
//        //{
//        //    Query qry1 = fbStore.Collection(DBCollectionName.Payment.ToString()).WhereEqualTo("UserID", userID).WhereEqualTo("ProductID", ProductItem.NoAd_Permanent.ToString()).OrderByDescending("UpdateTime").Limit(1);

//        //    QuerySnapshot snapShop1 = await qry1.GetSnapshotAsync();
//        //    if (snapShop1.Count > 0)
//        //        return true;

//        //    Query qry2 = fbStore.Collection(DBCollectionName.Payment.ToString()).WhereEqualTo("UserID", userID).WhereEqualTo("ProductID", ProductItem.NoAd_Month.ToString()).OrderByDescending("UpdateTime").Limit(1);
//        //    QuerySnapshot snapShop2 = await qry2.GetSnapshotAsync();
//        //    if (snapShop2.Count == 0)
//        //        return false;

//        //    foreach (DocumentSnapshot doc in snapShop2.Documents)
//        //    {
//        //        Payment pm = doc.ConvertTo<Payment>();

//        //        if (pm.UpdateTime.ToDateTime().AddDays(30) < DateTime.Now)
//        //            return false;

//        //        MSLog.Log($"[FetchNoAdPaymentAsync] success. OrderID: {pm.OrderID}, Date: {pm.UpdateTime.ToDateTime()}");
//        //    }

//        //    return true;
//        //}


//        #endregion Payment

//        // ----- Set ------------------------------------------------------------------------------------------------------

//        #region Country Code(3digits)

//        public void SetCountryCode()
//        {
//            countryCodeDic.Add("BD", "BGD");
//            countryCodeDic.Add("BE", "BEL");
//            countryCodeDic.Add("BF", "BFA");
//            countryCodeDic.Add("BG", "BGR");
//            countryCodeDic.Add("BA", "BIH");
//            countryCodeDic.Add("BB", "BRB");
//            countryCodeDic.Add("WF", "WLF");
//            countryCodeDic.Add("BL", "BLM");
//            countryCodeDic.Add("BM", "BMU");
//            countryCodeDic.Add("BN", "BRN");
//            countryCodeDic.Add("BO", "BOL");
//            countryCodeDic.Add("BH", "BHR");
//            countryCodeDic.Add("BI", "BDI");
//            countryCodeDic.Add("BJ", "BEN");
//            countryCodeDic.Add("BT", "BTN");
//            countryCodeDic.Add("JM", "JAM");
//            countryCodeDic.Add("BV", "BVT");
//            countryCodeDic.Add("BW", "BWA");
//            countryCodeDic.Add("WS", "WSM");
//            countryCodeDic.Add("BQ", "BES");
//            countryCodeDic.Add("BR", "BRA");
//            countryCodeDic.Add("BS", "BHS");
//            countryCodeDic.Add("JE", "JEY");
//            countryCodeDic.Add("BY", "BLR");
//            countryCodeDic.Add("BZ", "BLZ");
//            countryCodeDic.Add("RU", "RUS");
//            countryCodeDic.Add("RW", "RWA");
//            countryCodeDic.Add("RS", "SRB");
//            countryCodeDic.Add("TL", "TLS");
//            countryCodeDic.Add("RE", "REU");
//            countryCodeDic.Add("TM", "TKM");
//            countryCodeDic.Add("TJ", "TJK");
//            countryCodeDic.Add("RO", "ROU");
//            countryCodeDic.Add("TK", "TKL");
//            countryCodeDic.Add("GW", "GNB");
//            countryCodeDic.Add("GU", "GUM");
//            countryCodeDic.Add("GT", "GTM");
//            countryCodeDic.Add("GS", "SGS");
//            countryCodeDic.Add("GR", "GRC");
//            countryCodeDic.Add("GQ", "GNQ");
//            countryCodeDic.Add("GP", "GLP");
//            countryCodeDic.Add("JP", "JPN");
//            countryCodeDic.Add("GY", "GUY");
//            countryCodeDic.Add("GG", "GGY");
//            countryCodeDic.Add("GF", "GUF");
//            countryCodeDic.Add("GE", "GEO");
//            countryCodeDic.Add("GD", "GRD");
//            countryCodeDic.Add("GB", "GBR");
//            countryCodeDic.Add("GA", "GAB");
//            countryCodeDic.Add("SV", "SLV");
//            countryCodeDic.Add("GN", "GIN");
//            countryCodeDic.Add("GM", "GMB");
//            countryCodeDic.Add("GL", "GRL");
//            countryCodeDic.Add("GI", "GIB");
//            countryCodeDic.Add("GH", "GHA");
//            countryCodeDic.Add("OM", "OMN");
//            countryCodeDic.Add("TN", "TUN");
//            countryCodeDic.Add("JO", "JOR");
//            countryCodeDic.Add("HR", "HRV");
//            countryCodeDic.Add("HT", "HTI");
//            countryCodeDic.Add("HU", "HUN");
//            countryCodeDic.Add("HK", "HKG");
//            countryCodeDic.Add("HN", "HND");
//            countryCodeDic.Add("HM", "HMD");
//            countryCodeDic.Add("VE", "VEN");
//            countryCodeDic.Add("PR", "PRI");
//            countryCodeDic.Add("PS", "PSE");
//            countryCodeDic.Add("PW", "PLW");
//            countryCodeDic.Add("PT", "PRT");
//            countryCodeDic.Add("SJ", "SJM");
//            countryCodeDic.Add("PY", "PRY");
//            countryCodeDic.Add("IQ", "IRQ");
//            countryCodeDic.Add("PA", "PAN");
//            countryCodeDic.Add("PF", "PYF");
//            countryCodeDic.Add("PG", "PNG");
//            countryCodeDic.Add("PE", "PER");
//            countryCodeDic.Add("PK", "PAK");
//            countryCodeDic.Add("PH", "PHL");
//            countryCodeDic.Add("PN", "PCN");
//            countryCodeDic.Add("PL", "POL");
//            countryCodeDic.Add("PM", "SPM");
//            countryCodeDic.Add("ZM", "ZMB");
//            countryCodeDic.Add("EH", "ESH");
//            countryCodeDic.Add("EE", "EST");
//            countryCodeDic.Add("EG", "EGY");
//            countryCodeDic.Add("ZA", "ZAF");
//            countryCodeDic.Add("EC", "ECU");
//            countryCodeDic.Add("IT", "ITA");
//            countryCodeDic.Add("VN", "VNM");
//            countryCodeDic.Add("SB", "SLB");
//            countryCodeDic.Add("ET", "ETH");
//            countryCodeDic.Add("SO", "SOM");
//            countryCodeDic.Add("ZW", "ZWE");
//            countryCodeDic.Add("SA", "SAU");
//            countryCodeDic.Add("ES", "ESP");
//            countryCodeDic.Add("ER", "ERI");
//            countryCodeDic.Add("ME", "MNE");
//            countryCodeDic.Add("MD", "MDA");
//            countryCodeDic.Add("MG", "MDG");
//            countryCodeDic.Add("MF", "MAF");
//            countryCodeDic.Add("MA", "MAR");
//            countryCodeDic.Add("MC", "MCO");
//            countryCodeDic.Add("UZ", "UZB");
//            countryCodeDic.Add("MM", "MMR");
//            countryCodeDic.Add("ML", "MLI");
//            countryCodeDic.Add("MO", "MAC");
//            countryCodeDic.Add("MN", "MNG");
//            countryCodeDic.Add("MH", "MHL");
//            countryCodeDic.Add("MK", "MKD");
//            countryCodeDic.Add("MU", "MUS");
//            countryCodeDic.Add("MT", "MLT");
//            countryCodeDic.Add("MW", "MWI");
//            countryCodeDic.Add("MV", "MDV");
//            countryCodeDic.Add("MQ", "MTQ");
//            countryCodeDic.Add("MP", "MNP");
//            countryCodeDic.Add("MS", "MSR");
//            countryCodeDic.Add("MR", "MRT");
//            countryCodeDic.Add("IM", "IMN");
//            countryCodeDic.Add("UG", "UGA");
//            countryCodeDic.Add("TZ", "TZA");
//            countryCodeDic.Add("MY", "MYS");
//            countryCodeDic.Add("MX", "MEX");
//            countryCodeDic.Add("IL", "ISR");
//            countryCodeDic.Add("FR", "FRA");
//            countryCodeDic.Add("IO", "IOT");
//            countryCodeDic.Add("SH", "SHN");
//            countryCodeDic.Add("FI", "FIN");
//            countryCodeDic.Add("FJ", "FJI");
//            countryCodeDic.Add("FK", "FLK");
//            countryCodeDic.Add("FM", "FSM");
//            countryCodeDic.Add("FO", "FRO");
//            countryCodeDic.Add("NI", "NIC");
//            countryCodeDic.Add("NL", "NLD");
//            countryCodeDic.Add("NO", "NOR");
//            countryCodeDic.Add("NA", "NAM");
//            countryCodeDic.Add("VU", "VUT");
//            countryCodeDic.Add("NC", "NCL");
//            countryCodeDic.Add("NE", "NER");
//            countryCodeDic.Add("NF", "NFK");
//            countryCodeDic.Add("NG", "NGA");
//            countryCodeDic.Add("NZ", "NZL");
//            countryCodeDic.Add("NP", "NPL");
//            countryCodeDic.Add("NR", "NRU");
//            countryCodeDic.Add("NU", "NIU");
//            countryCodeDic.Add("CK", "COK");
//            countryCodeDic.Add("XK", "XKX");
//            countryCodeDic.Add("CI", "CIV");
//            countryCodeDic.Add("CH", "CHE");
//            countryCodeDic.Add("CO", "COL");
//            countryCodeDic.Add("CN", "CHN");
//            countryCodeDic.Add("CM", "CMR");
//            countryCodeDic.Add("CL", "CHL");
//            countryCodeDic.Add("CC", "CCK");
//            countryCodeDic.Add("CA", "CAN");
//            countryCodeDic.Add("CG", "COG");
//            countryCodeDic.Add("CF", "CAF");
//            countryCodeDic.Add("CD", "COD");
//            countryCodeDic.Add("CZ", "CZE");
//            countryCodeDic.Add("CY", "CYP");
//            countryCodeDic.Add("CX", "CXR");
//            countryCodeDic.Add("CR", "CRI");
//            countryCodeDic.Add("CW", "CUW");
//            countryCodeDic.Add("CV", "CPV");
//            countryCodeDic.Add("CU", "CUB");
//            countryCodeDic.Add("SZ", "SWZ");
//            countryCodeDic.Add("SY", "SYR");
//            countryCodeDic.Add("SX", "SXM");
//            countryCodeDic.Add("KG", "KGZ");
//            countryCodeDic.Add("KE", "KEN");
//            countryCodeDic.Add("SS", "SSD");
//            countryCodeDic.Add("SR", "SUR");
//            countryCodeDic.Add("KI", "KIR");
//            countryCodeDic.Add("KH", "KHM");
//            countryCodeDic.Add("KN", "KNA");
//            countryCodeDic.Add("KM", "COM");
//            countryCodeDic.Add("ST", "STP");
//            countryCodeDic.Add("SK", "SVK");
//            countryCodeDic.Add("KR", "KOR");
//            countryCodeDic.Add("SI", "SVN");
//            countryCodeDic.Add("KP", "PRK");
//            countryCodeDic.Add("KW", "KWT");
//            countryCodeDic.Add("SN", "SEN");
//            countryCodeDic.Add("SM", "SMR");
//            countryCodeDic.Add("SL", "SLE");
//            countryCodeDic.Add("SC", "SYC");
//            countryCodeDic.Add("KZ", "KAZ");
//            countryCodeDic.Add("KY", "CYM");
//            countryCodeDic.Add("SG", "SGP");
//            countryCodeDic.Add("SE", "SWE");
//            countryCodeDic.Add("SD", "SDN");
//            countryCodeDic.Add("DO", "DOM");
//            countryCodeDic.Add("DM", "DMA");
//            countryCodeDic.Add("DJ", "DJI");
//            countryCodeDic.Add("DK", "DNK");
//            countryCodeDic.Add("VG", "VGB");
//            countryCodeDic.Add("DE", "DEU");
//            countryCodeDic.Add("YE", "YEM");
//            countryCodeDic.Add("DZ", "DZA");
//            countryCodeDic.Add("US", "USA");
//            countryCodeDic.Add("UY", "URY");
//            countryCodeDic.Add("YT", "MYT");
//            countryCodeDic.Add("UM", "UMI");
//            countryCodeDic.Add("LB", "LBN");
//            countryCodeDic.Add("LC", "LCA");
//            countryCodeDic.Add("LA", "LAO");
//            countryCodeDic.Add("TV", "TUV");
//            countryCodeDic.Add("TW", "TWN");
//            countryCodeDic.Add("TT", "TTO");
//            countryCodeDic.Add("TR", "TUR");
//            countryCodeDic.Add("LK", "LKA");
//            countryCodeDic.Add("LI", "LIE");
//            countryCodeDic.Add("LV", "LVA");
//            countryCodeDic.Add("TO", "TON");
//            countryCodeDic.Add("LT", "LTU");
//            countryCodeDic.Add("LU", "LUX");
//            countryCodeDic.Add("LR", "LBR");
//            countryCodeDic.Add("LS", "LSO");
//            countryCodeDic.Add("TH", "THA");
//            countryCodeDic.Add("TF", "ATF");
//            countryCodeDic.Add("TG", "TGO");
//            countryCodeDic.Add("TD", "TCD");
//            countryCodeDic.Add("TC", "TCA");
//            countryCodeDic.Add("LY", "LBY");
//            countryCodeDic.Add("VA", "VAT");
//            countryCodeDic.Add("VC", "VCT");
//            countryCodeDic.Add("AE", "ARE");
//            countryCodeDic.Add("AD", "AND");
//            countryCodeDic.Add("AG", "ATG");
//            countryCodeDic.Add("AF", "AFG");
//            countryCodeDic.Add("AI", "AIA");
//            countryCodeDic.Add("VI", "VIR");
//            countryCodeDic.Add("IS", "ISL");
//            countryCodeDic.Add("IR", "IRN");
//            countryCodeDic.Add("AM", "ARM");
//            countryCodeDic.Add("AL", "ALB");
//            countryCodeDic.Add("AO", "AGO");
//            countryCodeDic.Add("AQ", "ATA");
//            countryCodeDic.Add("AS", "ASM");
//            countryCodeDic.Add("AR", "ARG");
//            countryCodeDic.Add("AU", "AUS");
//            countryCodeDic.Add("AT", "AUT");
//            countryCodeDic.Add("AW", "ABW");
//            countryCodeDic.Add("IN", "IND");
//            countryCodeDic.Add("AX", "ALA");
//            countryCodeDic.Add("AZ", "AZE");
//            countryCodeDic.Add("IE", "IRL");
//            countryCodeDic.Add("ID", "IDN");
//            countryCodeDic.Add("UA", "UKR");
//            countryCodeDic.Add("QA", "QAT");
//            countryCodeDic.Add("MZ", "MOZ");
//        }

//        #endregion Country Code(3digits)
//    }
//}