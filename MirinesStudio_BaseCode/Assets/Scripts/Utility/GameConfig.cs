
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

static public class GameConfig
{
    #region Key
    public const string MID = "mid";

    public const string PUSH = "PUSH";
    public const string Confirm_Notification = "CONFIRM_NOTIFICATION";

    public const string BGM = "BGM";
    public const string SFX = "SFX";
    public const string VIBRATION = "VIBRATION";

    public const string REVIEW = "REVIEW";

    public const string ADS_INTERSTITIAL = "ADS_INTERSTITIAL";

    public const string GUEST_KEY = "Guest_Tester";

    // 암호화
    private static string s_strEncodeKey = "9de1f0c839a77d2c3a97d2cd123574dc";

    #endregion
    public static readonly DateTime OldDateTime = new DateTime(2000, 1, 1);

    static public void Set(string strName, string strValue)
    {
        string strEncodeName = EncodeMD5(strName);
        string strEncodeValue = EncodeTripleDES(strValue);
        PlayerPrefs.SetString(strEncodeName, strEncodeValue);
    }
    static public void Set(string strName, int iValue)
    {
        Set(strName, iValue.ToString());
    }
    static public void Set(string strName, float fValue)
    {
        Set(strName, fValue.ToString());
    }
    static public void Set(string strName, bool bValue)
    {
        Set(strName, bValue ? "True" : "False");
    }
    static public void Set(string strName, ulong ulValue)
    {
        Set(strName, ulValue.ToString());
    }
    static public void Set(string strName, long lValue)
    {
        Set(strName, lValue.ToString());
    }
    static public void Set(string strName, uint uiValue)
    {
        Set(strName, uiValue.ToString());
    }
    static public void Set(string strName, double dValue)
    {
        Set(strName, dValue.ToString());
    }

    static public void Save()
    {
        PlayerPrefs.Save();
    }

    static public void Delete(string strName)
    {
        string strEncodeName = EncodeMD5(strName);
        if (PlayerPrefs.HasKey(strEncodeName))
            PlayerPrefs.DeleteKey(strEncodeName);
    }

    static public bool Get(string strName, ref string strValue)
    {
        string strEncodeName = EncodeMD5(strName);
        if (PlayerPrefs.HasKey(strEncodeName))
        {
            string strEncodeValue = PlayerPrefs.GetString(strEncodeName);
            strValue = DecodeTripleDES(strEncodeValue);
            return true;
        }
        return false;
    }

    static public bool Get(string strName, ref byte byteValue)
    {
        string strValue = string.Empty;
        if (Get(strName, ref strValue))
        {
            try
            {
                byteValue = System.Convert.ToByte(strValue);
            }
            catch
            {
                byteValue = 0;
            }
            return true;
        }
        return false;
    }

    static public bool Get(string strName, ref int iValue)
    {
        string strValue = string.Empty;
        if (Get(strName, ref strValue))
        {
            try
            {
                iValue = System.Convert.ToInt32(strValue);
            }
            catch
            {
                iValue = 0;
            }
            return true;
        }
        return false;
    }

    static public bool Get(string strName, ref float fValue)
    {
        string strValue = string.Empty;
        if (Get(strName, ref strValue))
        {
            try
            {
                fValue = System.Convert.ToSingle(strValue);
            }
            catch
            {
                fValue = 0.0f;
            }
            return true;
        }
        return false;
    }

    static public bool Get(string strName, ref bool bValue)
    {
        string strValue = string.Empty;
        if (Get(strName, ref strValue))
        {
            bValue = strValue == "True";
            return true;
        }
        return false;
    }

    static public bool Get(string strName, ref ulong ulValue)
    {
        string strValue = string.Empty;
        if (Get(strName, ref strValue))
        {
            try
            {
                ulValue = System.Convert.ToUInt64(strValue);
            }
            catch
            {
                ulValue = 0;
            }
            return true;
        }
        return false;
    }

    static public bool Get(string strName, ref long lValue)
    {
        string strValue = string.Empty;
        if (Get(strName, ref strValue))
        {
            try
            {
                lValue = System.Convert.ToInt64(strValue);
            }
            catch
            {
                lValue = 0;
            }
            return true;
        }
        return false;
    }

    static public bool Get(string strName, ref uint uiValue)
    {
        string strValue = string.Empty;
        if (Get(strName, ref strValue))
        {
            try
            {
                uiValue = System.Convert.ToUInt32(strValue);
            }
            catch
            {
                uiValue = 0;
            }
            return true;
        }
        return false;
    }

    static public bool Get(string strName, ref double dValue)
    {
        string strValue = string.Empty;
        if (Get(strName, ref strValue))
        {
            try
            {
                dValue = System.Convert.ToDouble(strValue);
            }
            catch
            {
                dValue = 0;
            }
            return true;
        }
        return false;
    }

    static public bool HasKey(string strName)
    {
        string strEncodeName = EncodeMD5(strName);
        return PlayerPrefs.HasKey(strEncodeName);
    }

    static public string EncodeMD5(string strValue)
    {
        StringBuilder strBuilder = new StringBuilder();
        MD5 md5Hash = MD5.Create();
        byte[] hashData = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(strValue));
        foreach (byte hashValue in hashData)
        {
            strBuilder.Append(hashValue.ToString());
        }
        return strBuilder.ToString();
    }

    public static string EncodeTripleDES(string strValue)
    {
        MD5 md5Hash = new MD5CryptoServiceProvider();
        byte[] secret = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(s_strEncodeKey));

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(strValue);

        TripleDES des = new TripleDESCryptoServiceProvider();
        des.Key = secret;
        des.Mode = CipherMode.ECB;
        ICryptoTransform xform = des.CreateEncryptor();
        byte[] encrypted = xform.TransformFinalBlock(bytes, 0, bytes.Length);

        return System.Convert.ToBase64String(encrypted);
    }

    public static string DecodeTripleDES(string strValue)
    {
        if (strValue.Length <= 0)
            return "";

        MD5 md5Hash = new MD5CryptoServiceProvider();
        byte[] secret = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(s_strEncodeKey));

        byte[] bytes = System.Convert.FromBase64String(strValue);
        TripleDES des = new TripleDESCryptoServiceProvider();

        des.Key = secret;
        des.Mode = CipherMode.ECB;
        ICryptoTransform xform = des.CreateDecryptor();
        byte[] decrypted = xform.TransformFinalBlock(bytes, 0, bytes.Length);

        return Encoding.UTF8.GetString(decrypted);
    }

    public static string EncodeTripleDES(string strValue, Encoding encodingType)
    {
        MD5 md5Hash = new MD5CryptoServiceProvider();
        byte[] secret = md5Hash.ComputeHash(encodingType.GetBytes(s_strEncodeKey));

        byte[] bytes = encodingType.GetBytes(strValue);

        TripleDES des = new TripleDESCryptoServiceProvider();
        des.Key = secret;
        des.Mode = CipherMode.ECB;
        ICryptoTransform xform = des.CreateEncryptor();
        byte[] encrypted = xform.TransformFinalBlock(bytes, 0, bytes.Length);

        return System.Convert.ToBase64String(encrypted);
    }

    public static string DecodeTripleDES(string strValue, Encoding encodingType)
    {
        MD5 md5Hash = new MD5CryptoServiceProvider();
        byte[] secret = md5Hash.ComputeHash(encodingType.GetBytes(s_strEncodeKey));

        byte[] bytes = System.Convert.FromBase64String(strValue);
        TripleDES des = new TripleDESCryptoServiceProvider();

        des.Key = secret;
        des.Mode = CipherMode.ECB;
        ICryptoTransform xform = des.CreateDecryptor();
        byte[] decrypted = xform.TransformFinalBlock(bytes, 0, bytes.Length);

        return encodingType.GetString(decrypted);
    }

    public static void SetClass<T>(string name, T instance)
    {
        using (var ms = new MemoryStream())
        {
            new BinaryFormatter().Serialize(ms, instance);
            GameConfig.Set(name, System.Convert.ToBase64String(ms.ToArray()));
        }
    }

    public static T GetClass<T>(string name) where T : new()
    {
        string getStr = string.Empty;
        GameConfig.Get(name, ref getStr);
        if (getStr == string.Empty)
            return default(T);

        byte[] bytes = System.Convert.FromBase64String(getStr);
        using (var ms = new MemoryStream(bytes))
        {
            object obj = new BinaryFormatter().Deserialize(ms);
            return (T)obj;
        }
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteAll();
    }
}
//using System;
//using System.Text;
//using System.Globalization;
//using System.Security.Cryptography;
//using UnityEngine;

//public static class GameConfig
//{
//    // ----- 프로젝트 고정 설정 -----------------------------------------------------------------------------------

//    private const string Passphrase = "change_me_to_project_secret_2025"; // 사람이 관리하는 비밀 문자열
//    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("GameConfig::Salt/v2"); // 키 유도시 보안 강화 + 고유성 보장
//    private const byte PACKET_VERSION = 1;
//    private static readonly IFormatProvider Inv = CultureInfo.InvariantCulture;

//    public const string PUSH = "PUSH";
//    public const string LEVEL = "LEVEL";
//    public const string COIN = "COIN";
//    public const string USE_UNDO = "USE_UNDO";
//    public const string USE_EXTRASLOT = "USE_EXTRASLOT";

//    public static readonly DateTime OldDateTime = new DateTime(2000, 1, 1);

//    public static void SetString(string key, string value)
//    {
//        string k = EncodeKeyName(key);
//        string enc = EncryptToBase64(value);
//        PlayerPrefs.SetString(k, enc);
//    }

//    public static bool TryGetString(string key, out string value)
//    {
//        value = null;
//        string k = EncodeKeyName(key);
//        if (!PlayerPrefs.HasKey(k)) return false;

//        string b64 = PlayerPrefs.GetString(k);
//        if (string.IsNullOrEmpty(b64)) return false;

//        try
//        {
//            value = DecryptFromBase64(b64);
//            return true;
//        }
//        catch (Exception e)
//        {
//            Debug.LogWarning($"[GameConfig] Decrypt error for '{key}': {e.Message}");
//            return false;
//        }
//    }

//    public static bool HasKey(string key) => PlayerPrefs.HasKey(EncodeKeyName(key));
//    public static void Delete(string key) 
//    { 
//        var k = EncodeKeyName(key); if (PlayerPrefs.HasKey(k)) PlayerPrefs.DeleteKey(k); 
//    }
//    public static void Save() => PlayerPrefs.Save();
//    public static void Clear() => PlayerPrefs.DeleteAll();

//    // ----- Set -----

//    public static void Set(string key, int v) => SetString(key, v.ToString(Inv));
//    public static void Set(string key, uint v) => SetString(key, v.ToString(Inv));
//    public static void Set(string key, long v) => SetString(key, v.ToString(Inv));
//    public static void Set(string key, ulong v) => SetString(key, v.ToString(Inv));
//    public static void Set(string key, float v) => SetString(key, v.ToString(Inv));
//    public static void Set(string key, double v) => SetString(key, v.ToString(Inv));
//    public static void Set(string key, bool v) => SetString(key, v ? "true" : "false");

//    public static bool TryGet(string key, out int v)
//    {
//        v = default;
//        return TryGetString(key, out var s) && int.TryParse(s, System.Globalization.NumberStyles.Any, Inv, out v);
//    }

//    public static bool TryGet(string key, out uint v)
//    { 
//        v = default; return TryGetString(key, out var s) && uint.TryParse(s, System.Globalization.NumberStyles.Any, Inv, out v); 
//    }

//    public static bool TryGet(string key, out long v)
//    { 
//        v = default; return TryGetString(key, out var s) && long.TryParse(s, System.Globalization.NumberStyles.Any, Inv, out v); 
//    }

//    public static bool TryGet(string key, out ulong v)
//    { 
//        v = default; return TryGetString(key, out var s) && ulong.TryParse(s, System.Globalization.NumberStyles.Any, Inv, out v); 
//    }

//    public static bool TryGet(string key, out float v)
//    { 
//        v = default; return TryGetString(key, out var s) && float.TryParse(s, System.Globalization.NumberStyles.Any, Inv, out v); 
//    }

//    public static bool TryGet(string key, out double v)
//    { 
//        v = default; return TryGetString(key, out var s) && double.TryParse(s, System.Globalization.NumberStyles.Any, Inv, out v); 
//    }

//    public static bool TryGet(string key, out bool v)
//    { 
//        v = default; return TryGetString(key, out var s) && bool.TryParse(s, out v); 
//    }

//    public static void SetObject<T>(string key, T instance)
//    {
//        var json = JsonUtility.ToJson(instance);

//        SetString(key, json);
//    }

//    public static bool TryGetObject<T>(string key, out T instance)
//    {
//        instance = default;
//        if (!TryGetString(key, out var json) || string.IsNullOrEmpty(json)) 
//            return false;

//        try
//        {
//            instance = JsonUtility.FromJson<T>(json);
//            return true;
//        }
//        catch (Exception e)
//        {
//            Debug.LogWarning($"[GameConfig] JSON parse fail for '{key}': {e.Message}");
//            return false;
//        }
//    }

//    // ===== 내부: 키 이름 난독화 =====
//    private static string EncodeKeyName(string raw)
//    {
//        using var sha = SHA256.Create();
//        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
//        var sb = new StringBuilder(hash.Length * 2);

//        foreach (var b in hash) 
//            sb.Append(b.ToString("x2"));

//        return sb.ToString();
//    }

//    // ===== 내부: 암복호화 (AES-CBC + HMAC-SHA256) =====
//    private static string EncryptToBase64(string plaintext)
//    {
//        DeriveKeys(Passphrase, out var keyEnc, out var keyMac);

//        byte[] iv = RandomBytes(16);
//        byte[] cipher;
//        using (var aes = Aes.Create())
//        {
//            aes.Mode = CipherMode.CBC;
//            aes.Padding = PaddingMode.PKCS7;
//            aes.Key = keyEnc;
//            aes.IV = iv;
//            using var enc = aes.CreateEncryptor();
//            var plain = Encoding.UTF8.GetBytes(plaintext);
//            cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
//        }

//        byte[] packetNoTag = Concat(new byte[] { PACKET_VERSION }, iv, cipher);
//        byte[] tag;
//        using (var hmac = new HMACSHA256(keyMac))
//            tag = hmac.ComputeHash(packetNoTag);

//        var final = Concat(packetNoTag, tag);
//        return Convert.ToBase64String(final);
//    }

//    private static string DecryptFromBase64(string base64)
//    {
//        var data = Convert.FromBase64String(base64);
//        if (data.Length < 1 + 16 + 32) 
//            throw new CryptographicException("Packet too small");

//        byte version = data[0];
//        if (version != PACKET_VERSION) 
//            throw new CryptographicException($"Unsupported version: {version}");

//        var iv = data.AsSpan(1, 16).ToArray();
//        var tag = data[^32..];
//        var cipher = data.AsSpan(17, data.Length - 17 - 32).ToArray();

//        DeriveKeys(Passphrase, out var keyEnc, out var keyMac);

//        byte[] packetNoTag = Concat(new byte[] { version }, iv, cipher);
//        using (var hmac = new HMACSHA256(keyMac))
//        {
//            var calc = hmac.ComputeHash(packetNoTag);
//            if (!CryptographicOperations.FixedTimeEquals(calc, tag))
//                throw new CryptographicException("HMAC mismatch");
//        }

//        using var aes = Aes.Create();
//        aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
//        aes.Key = keyEnc; 
//        aes.IV = iv;
//        using var dec = aes.CreateDecryptor();
//        var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
//        return Encoding.UTF8.GetString(plain);
//    }

//    private static void DeriveKeys(string passphrase, out byte[] keyEnc, out byte[] keyMac)
//    {
//        using var pbkdf2 = new Rfc2898DeriveBytes(passphrase, Salt, 100_000, HashAlgorithmName.SHA256);
//        var bytes = pbkdf2.GetBytes(64);
//        keyEnc = new byte[32];
//        keyMac = new byte[32];
//        Buffer.BlockCopy(bytes, 0, keyEnc, 0, 32);
//        Buffer.BlockCopy(bytes, 32, keyMac, 0, 32);
//    }

//    private static byte[] RandomBytes(int len)
//    {
//        var buf = new byte[len];
//        RandomNumberGenerator.Fill(buf);
//        return buf;
//    }

//    private static byte[] Concat(params byte[][] arrays)
//    {
//        int total = 0;

//        foreach (var a in arrays) 
//            total += a.Length;

//        var r = new byte[total];
//        int o = 0;

//        foreach (var a in arrays) 
//        { 
//            Buffer.BlockCopy(a, 0, r, o, a.Length); 
//            o += a.Length; 
//        }

//        return r;
//    }
//}
