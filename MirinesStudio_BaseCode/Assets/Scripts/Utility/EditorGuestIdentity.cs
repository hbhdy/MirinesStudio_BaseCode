using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MirinesStudio
{
    public static class EditorGuestIdentity
    {
        // 프로젝트 로컬(개발자별) 저장 위치: UserSettings는 보통 gitignore됨
        private static readonly string FilePath =
            Path.Combine(Directory.GetParent(Application.dataPath)!.FullName,
                         "UserSettings", "MirinesStudio_EditorGuestId.txt");

        private const string EditorPrefsKey = "MirinesStudio_EditorGuestId";

        /// <summary>에디터에서 사용할 안정적인 Guest ID (예: gst_8fa31c2e)</summary>
        public static string GetGuestId()
        {
#if UNITY_EDITOR
            // 1) 파일 캐시 우선 (프로젝트 단위, 개발자별)
            var id = ReadFromFile();
            if (!string.IsNullOrEmpty(id))
                return id;

            // 2) EditorPrefs (에디터 사용자 계정 단위)
            id = ReadFromEditorPrefs();
            if (!string.IsNullOrEmpty(id))
            {
                WriteToFile(id); // 파일에도 기록해 두면 팀/브랜치 이동에도 안정적
                return id;
            }

            // 3) 결정적 해시 기반 생성 (기기/OS사용자/프로젝트 경로)
            var root = GetStableFingerprintRaw();
            var hash = ShortHash(root, 10);
#if UNITY_ANDROID
            id = $"gst_Android_{hash}";
#elif UNITY_IOS
            id = $"gst_IOS_{hash}";
#endif

            // 캐시 저장
            WriteToEditorPrefs(id);
            WriteToFile(id);

            return id;
#else
            return "gst_runtime"; // 런타임/빌드에선 쓰지 않음
#endif
        }

        /// <summary>UI에 표시할 기본 이름 (예: Editor-홍길동@DEVPC)</summary>
        public static string GetDisplayName()
        {
            var uname = Environment.UserName;
            var mname = Environment.MachineName;
            return $"Editor-{uname}@{mname}";
        }

#if UNITY_EDITOR
        private static string ReadFromEditorPrefs()
            => EditorPrefs.GetString(EditorPrefsKey, string.Empty);

        private static void WriteToEditorPrefs(string id)
            => EditorPrefs.SetString(EditorPrefsKey, id);

        private static string ReadFromFile()
        {
            try
            {
                if (File.Exists(FilePath))
                    return File.ReadAllText(FilePath).Trim();
            }
            catch { /* ignore */ }
            return string.Empty;
        }

        private static void WriteToFile(string id)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, id);
            }
            catch { /* ignore */ }
        }

        // 충돌 확률을 낮추기 위해 여러 요소를 결합
        private static string GetStableFingerprintRaw()
        {
            var uname = Environment.UserName;         // OS 사용자명
            var mname = Environment.MachineName;      // 컴퓨터 이름
            var proj = Application.dataPath;         // 프로젝트 경로(팀이 공유해도 로컬경로가 달라짐)
            var nic = GetFirstMacAddress();         // 가능한 경우 MAC (없으면 빈 문자열)
            return $"{uname}|{mname}|{proj}|{nic}";
        }

        private static string ShortHash(string s, int len)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString().Substring(0, Mathf.Clamp(len, 6, 32));
        }

        private static string GetFirstMacAddress()
        {
            try
            {
                var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                var mac = nics.FirstOrDefault(n =>
                    n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                    n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback);
                return mac?.GetPhysicalAddress().ToString() ?? "";
            }
            catch { return ""; }
        }
#endif
    }
}
