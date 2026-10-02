using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
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

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            value = value.Replace("\r\n", "\\n").Replace("\r", "\\n").Replace("\n", "\\n");
            bool requiresQuotes = value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0;
            return requiresQuotes ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
        }

        [MenuItem("MirinesStudio/Convert All DataTable")]
        private static void ShowWindow()
        {
            var window = GetWindow<DataTableConvertWindow>();
            window.titleContent = new GUIContent("[ Convert DataTable ]");
            window.Show();
        }

        [MenuItem("MirinesStudio/Validate All DataTable")]
        private static void ValidateAllDataTable()
        {
            var window = GetWindow<DataTableConvertWindow>();
            window.titleContent = new GUIContent("[ Convert DataTable ]");
            window.ValidateProcess_DataTable_All();
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
            if (GUILayout.Button("Validate All (DataTable/Excel)", GUILayout.Height(36)))
                ValidateProcess_DataTable_All();

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

        public void ValidateProcess_DataTable_All()
        {
            sbProgress.Clear();
            string excelAbs = AbsPathFromAsset(ExcelAssetPath);
            var di = new DirectoryInfo(excelAbs);

            if (!di.Exists)
            {
                EditorUtility.DisplayDialog("Path Error", $"Excel folder not found:\n{excelAbs}\n\n(확인: {ExcelAssetPath})", "OK");
                return;
            }

            int errorCount = 0;
            int warningCount = 0;
            FileInfo[] files = GetExcelFiles(di);

            foreach (var file in files)
            {
                DataTableValidationResult validation = ValidateFile(file.FullName);
                AppendValidationResult(validation);
                errorCount += validation.ErrorCount;
                warningCount += validation.WarningCount;
            }

            if (files.Length == 0)
                sbProgress.AppendLine("Excel 파일이 없습니다.");

            Repaint();
            EditorUtility.DisplayDialog("데이터 테이블 검증", $"파일 {files.Length}개\n오류 {errorCount}개\n경고 {warningCount}개\n\n상세 내용은 변환 창과 Console을 확인해주세요.", "OK");
        }

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

            foreach (var file in GetExcelFiles(di))
            {
                ConvertToFile(file.FullName, csvAbs);
                Debug.Log("File.Name : " + file.Name);
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

            foreach (var file in GetExcelFiles(di))
                ConvertToFileTSV(file.FullName, tsvAbs);

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
                    DataSet dataSet = reader.AsDataSet();
                    DataTableValidationResult validation = DataTableValidator.Validate(dataSet, fileNameNoExt, startRow);
                    AppendValidationResult(validation);

                    if (validation.HasErrors)
                    {
                        sbFail.AppendLine($"{fileNameNoExt}: 검증 오류로 변환 중단");
                        return;
                    }

                    ConvertProcess(dataSet, absSaveDir, fileNameNoExt, startRow);
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
                        string value = result.Tables[0].Rows[x][y].ToString();
                        sb.Append(EscapeCsv(value));

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
                sbFail.Append(e.Message + "\n");
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
                    DataSet dataSet = reader.AsDataSet();
                    DataTableValidationResult validation = DataTableValidator.Validate(dataSet, fileNameNoExt, startRow);
                    AppendValidationResult(validation);

                    if (validation.HasErrors)
                    {
                        sbFail.AppendLine($"{fileNameNoExt}: 검증 오류로 변환 중단");
                        return;
                    }

                    ConvertProcessTSV(dataSet, absSaveDir, fileNameNoExt, startRow);
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
                sbFail.Append(e.Message + "\n");
            }
        }

        private static FileInfo[] GetExcelFiles(DirectoryInfo directory)
        {
            FileInfo[] files = directory.GetFiles("*.xlsx", SearchOption.AllDirectories);
            return Array.FindAll(files, file => !file.Name.StartsWith("~$", StringComparison.Ordinal));
        }

        private DataTableValidationResult ValidateFile(string absFilePath, int startRow = 2)
        {
            string fileName = Path.GetFileNameWithoutExtension(absFilePath);
            try
            {
                using var stream = File.Open(absFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = ExcelReaderFactory.CreateReader(stream);
                return DataTableValidator.Validate(reader.AsDataSet(), fileName, startRow);
            }
            catch (Exception e)
            {
                var result = new DataTableValidationResult(fileName);
                result.AddError($"파일을 읽지 못했습니다: {e.Message}");
                return result;
            }
        }

        private void AppendValidationResult(DataTableValidationResult result)
        {
            sbProgress.AppendLine($"[{result.FileName}] 오류 {result.ErrorCount}, 경고 {result.WarningCount}");
            foreach (DataTableValidationIssue issue in result.Issues)
            {
                sbProgress.AppendLine($"  {issue}");
                if (issue.Severity == DataTableValidationSeverity.Error)
                    Debug.LogError($"[{result.FileName}] {issue}");
                else
                    Debug.LogWarning($"[{result.FileName}] {issue}");
            }

            sbProgress.AppendLine();
        }
    }
}
