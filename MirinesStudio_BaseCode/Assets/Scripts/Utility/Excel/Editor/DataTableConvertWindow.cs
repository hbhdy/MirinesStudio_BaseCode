using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MirinesStudio
{
    public class DataTableConvertWindow : EditorWindow
    {
        // ----- Fixed paths (Asset paths) -----
        private const string DataTableAssetRoot = "Assets/DataTable";
        private static readonly string ExcelAssetPath = $"{DataTableAssetRoot}";
        private const string CsvOutAssetPath = "Assets/Resources/CSV";
        private const string TsvOutAssetPath = "Assets/Resources/TSV";

        // 절대 경로 변환 헬퍼 (Assets → OS 절대 경로)
        private static string AbsPathFromAsset(string assetPath)
            => Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

        // 기존 필드 중 경로 관련은 정리
        private static readonly StringBuilder sbSuccess = new StringBuilder();
        private static readonly StringBuilder sbFail = new StringBuilder();
        private readonly StringBuilder sbProgress = new StringBuilder();
        private static readonly Regex regexComma = new Regex(",");

        [MenuItem("MirinesStudio/Convert All DataTable")]
        private static void ShowWindow()
        {
            var window = GetWindow<DataTableConvertWindow>();
            window.titleContent = new GUIContent("[ Convert DataTable ]");
            window.Show();
        }

        [MenuItem("MirinesStudio/GameConfig Clear")]
        private static void GameConfigClear()
        {
            GameConfig.Clear();
            MSLog.Log("로컬 세이브 파일 삭제!!", LogColor.cyan);
        }

        private void OnGUI()
        {
            GUILayout.Space(6);
            EditorGUILayout.LabelField("Source (fixed):", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(ExcelAssetPath);

            EditorGUILayout.LabelField("Output (fixed):", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(CsvOutAssetPath);

            GUILayout.Space(8);
            if (GUILayout.Button("Convert All (DataTable/Excel → Resources/CSV)", GUILayout.Height(36)))
                ConvertProcess_DataTable_All();

            if (GUILayout.Button("Convert All TSV (DataTable/Excel → Resources/TSV)", GUILayout.Height(36)))
                ConvertProcess_DataTable_All_TSV();

            GUILayout.Space(10);
            if (GUILayout.Button("Open Source Folder", GUILayout.Height(36)))
            {
                var path = AbsPathFromAsset(ExcelAssetPath);

                // 1) 경로 검증 (폴더 or 파일)
                if (!Directory.Exists(path) && !File.Exists(path))
                {
                    Debug.LogWarning($"[Open Source Folder] Path not found: {path}");
                    return;
                }

                // 2) 다음 Editor 틱에서 실행 (OnGUI 재진입 방지)
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        EditorUtility.RevealInFinder(path);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                };

                GUIUtility.ExitGUI();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Open Output Folder", GUILayout.Height(36)))
            {
                var path = AbsPathFromAsset(CsvOutAssetPath);

                // 1) 경로 검증 (폴더 or 파일)
                if (!Directory.Exists(path) && !File.Exists(path))
                {
                    Debug.LogWarning($"[Open Output Folder] Path not found: {path}");
                    return;
                }

                // 2) 다음 Editor 틱에서 실행 (OnGUI 재진입 방지)
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        EditorUtility.RevealInFinder(path);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                };

                GUIUtility.ExitGUI();
            }

            GUILayout.Space(10);
            GUILayout.Label(sbProgress.ToString());
            GUILayout.FlexibleSpace();
        }

        // ----- Main -----

        public void ConvertProcess_DataTable_All()
        {
            sbSuccess.Clear();
            sbFail.Clear();
            sbProgress.Clear();
            sbSuccess.AppendLine("# File List #");

            string excelAbs = AbsPathFromAsset(ExcelAssetPath);
            string csvAbs = AbsPathFromAsset(CsvOutAssetPath);

            var di = new DirectoryInfo(excelAbs);
            if (!di.Exists)
            {
                EditorUtility.DisplayDialog("Path Error",
                    $"Excel folder not found:\n{excelAbs}\n\n(확인: {ExcelAssetPath})", "OK");
                return;
            }

            // 루트 폴더의 .xlsx
            foreach (var file in di.GetFiles("*.xlsx"))
            {
                ConvertToFile(file.FullName, csvAbs);
                Debug.Log("File.Name : " + file.Name);
            }

            // 하위 폴더의 .xlsx
            foreach (var folder in di.GetDirectories())
            {
                Debug.Log("Folder.Name : " + folder.Name);
                foreach (var file in folder.GetFiles("*.xlsx"))
                {
                    ConvertToFile(file.FullName, csvAbs);
                    Debug.Log("File.Name : " + file.Name);
                }
            }

            EditorUtility.DisplayDialog("Data Convert & Save Success.", sbSuccess.ToString(), "OK");
            if (sbFail.Length != 0)
                EditorUtility.DisplayDialog("Data Convert Fail", sbFail.ToString(), "OK");

            AssetDatabase.Refresh();
        }

        public void ConvertProcess_DataTable_All_TSV()
        {
            sbSuccess.Clear();
            sbFail.Clear();
            sbProgress.Clear();
            sbSuccess.AppendLine("# TSV File List #");

            string excelAbs = AbsPathFromAsset(ExcelAssetPath);
            string tsvAbs = AbsPathFromAsset(TsvOutAssetPath);

            var di = new DirectoryInfo(excelAbs);
            if (!di.Exists)
            {
                EditorUtility.DisplayDialog("Path Error",
                    $"Excel folder not found:\n{excelAbs}\n\n(확인: {ExcelAssetPath})", "OK");
                return;
            }

            foreach (var file in di.GetFiles("*.xlsx"))
                ConvertToFileTSV(file.FullName, tsvAbs);

            foreach (var folder in di.GetDirectories())
            {
                foreach (var file in folder.GetFiles("*.xlsx"))
                    ConvertToFileTSV(file.FullName, tsvAbs);
            }

            EditorUtility.DisplayDialog("TSV Convert & Save Success.", sbSuccess.ToString(), "OK");
            if (sbFail.Length != 0)
                EditorUtility.DisplayDialog("TSV Convert Fail", sbFail.ToString(), "OK");

            AssetDatabase.Refresh();
        }

        // 기존: Application.dataPath + "/DataTable/Excel" 방식 → 절대경로 직접 전달
        private void ConvertToFile(string absFilePath, string absSaveDir, int startRow = 2)
        {
            Debug.Log($" From => {absFilePath} \n To => {absSaveDir}");
            try
            {
                using (var stream = File.Open(absFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    // 파일명만 추출
                    var fileNameNoExt = Path.GetFileNameWithoutExtension(absFilePath);
                    ConvertProcess(reader.AsDataSet(), absSaveDir, fileNameNoExt, startRow);
                }
            }
            catch (Exception ex)
            {
                sbFail.AppendLine($"{absFilePath} 변환 실패: {ex.Message}");
            }
        }

        private void ConvertProcess(DataSet result, string absSaveDir, string fileName, int startRow)
        {
            try
            {
                int columns = result.Tables[0].Columns.Count;
                int rows = result.Tables[0].Rows.Count;
                var sb = new StringBuilder();

                for (int x = startRow; x < rows; x++)
                {
                    for (int y = 0; y < columns; y++)
                    {
                        string str = result.Tables[0].Rows[x][y].ToString();

                        // CSV 안전 처리
                        if (str.Contains(','))
                            str = regexComma.Replace(str, "u002c");

                        if (str.StartsWith("{") && str.EndsWith("}"))
                            str = $"\"{str}\"";

                        sb.Append(str);

                        if (y < columns - 1)
                            sb.Append(",");
                    }
                    sb.AppendLine();
                }

                // 저장
                Directory.CreateDirectory(absSaveDir);
                string csvPath = Path.Combine(absSaveDir, $"{fileName}.csv");
                if (File.Exists(csvPath)) File.Delete(csvPath);
                File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);

                // meta 제거(리로드)
                string meta = csvPath + ".meta";
                if (File.Exists(meta)) File.Delete(meta);

                sbSuccess.AppendLine(fileName);
            }
            catch (Exception e)
            {
                sbSuccess.Append(e.Message + "\n");
            }
        }

        private void ConvertToFileTSV(string absFilePath, string absSaveDir, int startRow = 2)
        {
            try
            {
                using (var stream = File.Open(absFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var fileNameNoExt = Path.GetFileNameWithoutExtension(absFilePath);
                    ConvertProcessTSV(reader.AsDataSet(), absSaveDir, fileNameNoExt, startRow);
                }
            }
            catch (Exception ex)
            {
                sbFail.AppendLine($"{absFilePath} TSV 변환 실패: {ex.Message}");
            }
        }

        private void ConvertProcessTSV(DataSet result, string absSaveDir, string fileName, int startRow)
        {
            try
            {
                int columns = result.Tables[0].Columns.Count;
                int rows = result.Tables[0].Rows.Count;
                var sb = new StringBuilder();

                for (int x = startRow; x < rows; x++)
                {
                    for (int y = 0; y < columns; y++)
                    {
                        string str = result.Tables[0].Rows[x][y].ToString();

                        if (str.Contains('\t'))
                            str = str.Replace("\t", "\\t");

                        sb.Append(str);

                        if (y < columns - 1)
                            sb.Append("\t");
                    }

                    sb.AppendLine();
                }

                Directory.CreateDirectory(absSaveDir);
                string tsvPath = Path.Combine(absSaveDir, $"{fileName}.tsv");
                if (File.Exists(tsvPath)) File.Delete(tsvPath);

                File.WriteAllText(tsvPath, sb.ToString(), Encoding.UTF8);

                string meta = tsvPath + ".meta";
                if (File.Exists(meta)) File.Delete(meta);

                sbSuccess.AppendLine(fileName);
            }
            catch (Exception e)
            {
                sbSuccess.Append(e.Message + "\n");
            }
        }
    }
}
