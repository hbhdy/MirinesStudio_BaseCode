//using System.Collections.Generic;
//using System.Linq;
//using GoogleMobileAds.Api;

//namespace MirinesStudio
//{
//    public static class CurrentGDPR
//    {
//        private static bool _isGdprOn;
//        private static string _purposeConsent, _vendorConsent, _vendorLi, _purposeLi, _partnerConsent;

//        static CurrentGDPR()
//        {
//            InitData();
//        }

//        public static void InitData()
//        {
//            int gdprNum = 0;

//            gdprNum = ApplicationPreferences.GetInt("IABTCF_gdprApplies");
//            _purposeConsent = ApplicationPreferences.GetString("IABTCF_PurposeConsents");
//            _vendorConsent = ApplicationPreferences.GetString("IABTCF_VendorConsents");
//            _vendorLi = ApplicationPreferences.GetString("IABTCF_VendorLegitimateInterests");
//            _purposeLi = ApplicationPreferences.GetString("IABTCF_PurposeLegitimateInterests");
//            _partnerConsent = ApplicationPreferences.GetString("IABTCF_AddtlConsent");

//            // 0 이면 아예 GDPR 대상이 아님. 1이어야 GDPR
//            if (gdprNum == 1)
//                _isGdprOn = true;
//            else
//                _isGdprOn = false;

//            MSLog.Log("GDPR을 띄우는가? " + _isGdprOn);
//            MSLog.Log("광고에 필요한 권한 동의: " + _purposeConsent);
//            MSLog.Log("광고에 필요한 적법관심(광고 개인화?) 동의: " + _vendorConsent);
//            MSLog.Log("구글에 동의처리가 되어있는가?: " + _vendorLi);
//            MSLog.Log("구글에 적법관심(광고 개인화?) 처리 여부: " + _purposeLi);
//            MSLog.Log("파트너 네트워크 여부: " + _partnerConsent);
//        }

//        /// <summary>
//        /// GDPR을 띄워야 할 유저인지(EEA = 유럽 + 영국) 리턴
//        /// </summary>
//        /// <returns>EEA 여부</returns>
//        public static bool IsGDPR()
//        {
//            return _isGdprOn;
//        }

//        /// <summary>
//        /// 광고에 필요한 권한 동의 여부
//        /// </summary>
//        /// <returns>동의 여부</returns>
//        public static bool IsPurposeConsents()
//        {
//            // Example value:
//            // 동의   : "1111111111"
//            // 비동의 : "0"

//            if (true == string.IsNullOrEmpty(_purposeConsent))
//            {
//                return false;
//            }

//            MSLog.Log($"[IsPurposeConsents] {HasAttribute(_purposeConsent, 1)}");

//            // Purposes are zero-indexed. Index 0 contains information about Purpose 1.
//            // _purposeConsent[0]에 1(동의)이 있는지 체크
//            return HasAttribute(_purposeConsent, 1);
//        }

//        /// <summary>
//        /// 광고에 필요한 적법관심(광고 개인화?) 동의 여부
//        /// </summary>
//        /// <returns>동의 여부</returns>
//        public static bool IsVendorConsents()
//        {
//            if (true == string.IsNullOrEmpty(_vendorConsent))
//            {
//                return false;
//            }

//            // _vendorConsent[0]에 1(동의)이 있는지 체크
//            return HasAttribute(_vendorConsent, 1);
//        }

//        // 광고가 보여지는지 여부 리턴
//        public static bool CanAdShow()
//        {
//            int googleId = 755;
//            bool hasGoogleVendorConsent = HasAttribute(_vendorConsent, googleId);
//            bool hasGoogleVendorLi = HasAttribute(_vendorLi, googleId);

//            // 광고 가능 - 비개인화 광고
//            // return HasConsentFor(new List<int> { 1 }, _purposeConsent, hasGoogleVendorConsent)
//            //        && HasConsentOrLegitimateInterestFor(new List<int> { 2, 7, 9, 10 }, 
//            //            _purposeConsent, _purposeLi, hasGoogleVendorConsent, hasGoogleVendorLi);

//            // 광고 가능 - 제한적인 광고 - 1에 대한 권한이 없어도 됨 ㅇㅇ
//            return HasConsentOrLegitimateInterestFor(new List<int> { 2, 7, 9, 10 },
//                       _purposeConsent, _purposeLi, hasGoogleVendorConsent, hasGoogleVendorLi);
//        }

//        // 개인화 광고가 보여지는지 여부 리턴
//        public static bool CanShowPersonalizedAds()
//        {
//            int googleId = 755;
//            bool hasGoogleVendorConsent = HasAttribute(_vendorConsent, googleId);
//            bool hasGoogleVendorLi = HasAttribute(_vendorLi, googleId);

//            return HasConsentFor(new List<int> { 1, 3, 4 }, _purposeConsent, hasGoogleVendorConsent)
//                   && HasConsentOrLegitimateInterestFor(new List<int> { 2, 7, 9, 10 },
//                       _purposeConsent, _purposeLi, hasGoogleVendorConsent, hasGoogleVendorLi);
//        }

//        public static bool IsPartnerConsent(string partnerID) // 파트너 권한 있는지 확인
//        {
//            return _partnerConsent.Contains(partnerID);
//        }

//        // 이진 문자열의 "index" 위치에 "1"이 있는지 확인합니다(1 기반).
//        private static bool HasAttribute(string input, int index)
//        {
//            return (index <= input.Length) && (input[index - 1] == '1');
//        }

//        // 목적 목록에 대한 동의가 주어졌는지 확인합니다.
//        private static bool HasConsentFor(List<int> purposes, string purposeConsent, bool hasVendorConsent)
//        {
//            return purposes.All(p => HasAttribute(purposeConsent, p)) && hasVendorConsent;
//        }

//        // 목적 목록에 대한 공급자의 동의 또는 정당한 이익이 있는지 확인합니다.
//        private static bool HasConsentOrLegitimateInterestFor(List<int> purposes, string purposeConsent, string purposeLI, bool hasVendorConsent, bool hasVendorLI)
//        {
//            return purposes.All(p =>
//                (HasAttribute(purposeLI, p) && hasVendorLI) ||
//                (HasAttribute(purposeConsent, p) && hasVendorConsent));
//        }
//    }
//}