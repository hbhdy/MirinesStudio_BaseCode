using System;
using System.IO;
using UnityEngine;

namespace MirinesStudio
{
    // 중복 컬러 및 흐릿한 색상 제외 
    public enum LogColor
	{
        none,
        //aqua,
        //black,
        blue,
        //brown,
        cyan,
        //darkblue,
        //fuchsia,
        green,
        //grey,
        lightblue,
        //lime,
        magenta,
        maroon,
        //navy,
        //olive,
        orange,
        purple,
        red,
        //silver,
        teal,
        white,
        yellow,
    }

    public static class MSLog
    {
        [System.Diagnostics.Conditional("SHOW_LOG")]
        public static void Log(string log, LogColor color = LogColor.none)
        {
            if (color == LogColor.none)
                Debug.Log($"{log}");
            else
                Debug.Log($"<color={color}>{log}</color>");

            //AppendToFile(log);
        }

        [System.Diagnostics.Conditional("SHOW_LOG")]
        public static void LogWarning(string log, LogColor color = LogColor.none)
        {
            if (color == LogColor.none)
                Debug.LogWarning($"{log}");
            else
                Debug.LogWarning($"<color={color}>{log}</color>");

            //AppendToFile(log);
        }

        [System.Diagnostics.Conditional("SHOW_LOG")]
        public static void LogError(string log, LogColor color = LogColor.none)
        {
            if (color == LogColor.none)
                Debug.LogError($"{log}");
            else
                Debug.LogError($"<color={color}>{log}</color>");

            //AppendToFile(log);
        }

        /// <summary>
        /// 오늘 날짜 기준 로그 파일 경로를 생성합니다.
        /// </summary>
        private static string GetDailyLogFilePath()
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            return Path.Combine(Application.persistentDataPath, $"log_{date}.txt");
        }

        /// <summary>
        /// 로그 파일에 메시지를 추가합니다. (날짜별로 저장)
        /// </summary>
        private static void AppendToFile(string message)
        {
            string path = GetDailyLogFilePath();
            try
            {
                File.AppendAllText(path, message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.LogError("로그 파일 저장 실패: " + ex.Message);
            }
        }

        /// <summary>
        /// 오늘의 로그 파일을 초기화합니다.
        /// </summary>
        public static void ClearTodayLogFile()
        {
            if (!Debug.isDebugBuild) return;

            string path = GetDailyLogFilePath();
            try
            {
                File.WriteAllText(path, string.Empty);
            }
            catch (Exception ex)
            {
                Debug.LogError("로그 파일 초기화 실패: " + ex.Message);
            }
        }

        /// <summary>
        /// 오늘의 로그 파일 경로를 반환합니다.
        /// </summary>
        public static string GetTodayLogFilePath()
        {
            return GetDailyLogFilePath();
        }
    }
}
