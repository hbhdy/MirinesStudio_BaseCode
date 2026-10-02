using System;
using System.Collections.Generic;
using System.Data;

namespace MirinesStudio
{
    internal enum DataTableValidationSeverity
    {
        Warning,
        Error,
    }

    internal sealed class DataTableValidationIssue
    {
        public DataTableValidationSeverity Severity { get; }
        public int Row { get; }
        public int Column { get; }
        public string Message { get; }

        public DataTableValidationIssue(DataTableValidationSeverity severity, string message, int row = 0, int column = 0)
        {
            Severity = severity;
            Message = message;
            Row = row;
            Column = column;
        }

        public override string ToString()
        {
            string location = Row > 0 ? Column > 0 ? $" ({Row}행 {Column}열)" : $" ({Row}행)" : string.Empty;
            return $"[{Severity}]{location} {Message}";
        }
    }

    internal sealed class DataTableValidationResult
    {
        public string FileName { get; }
        public List<DataTableValidationIssue> Issues { get; } = new List<DataTableValidationIssue>();
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public bool HasErrors => ErrorCount > 0;

        public DataTableValidationResult(string fileName)
        {
            FileName = fileName;
        }

        public void AddError(string message, int row = 0, int column = 0)
        {
            Issues.Add(new DataTableValidationIssue(DataTableValidationSeverity.Error, message, row, column));
            ErrorCount++;
        }

        public void AddWarning(string message, int row = 0, int column = 0)
        {
            Issues.Add(new DataTableValidationIssue(DataTableValidationSeverity.Warning, message, row, column));
            WarningCount++;
        }
    }

    internal static class DataTableValidator
    {
        public static DataTableValidationResult Validate(DataSet dataSet, string fileName, int headerRowIndex = 2)
        {
            var result = new DataTableValidationResult(fileName);

            if (!Enum.TryParse(fileName, false, out TableMetaDataType tableType) || tableType == TableMetaDataType.None || tableType == TableMetaDataType.Count)
                result.AddError($"'{fileName}'이 TableMetaDataType과 일치하지 않습니다.");

            if (dataSet == null || dataSet.Tables.Count == 0)
            {
                result.AddError("읽을 수 있는 시트가 없습니다.");
                return result;
            }

            DataTable table = dataSet.Tables[0];
            if (table.Rows.Count <= headerRowIndex)
            {
                result.AddError($"헤더로 사용할 {headerRowIndex + 1}행이 없습니다.");
                return result;
            }

            int columnCount = GetHeaderColumnCount(table, headerRowIndex);
            if (columnCount == 0)
            {
                result.AddError("헤더가 비어 있습니다.", headerRowIndex + 1);
                return result;
            }

            string[] headers = ValidateHeaders(table, headerRowIndex, columnCount, result);
            ValidateRows(table, headerRowIndex + 1, headers, result, tableType);
            return result;
        }

        private static int GetHeaderColumnCount(DataTable table, int headerRowIndex)
        {
            int columnCount = table.Columns.Count;
            while (columnCount > 0 && IsEmpty(table.Rows[headerRowIndex][columnCount - 1]))
                columnCount--;

            return columnCount;
        }

        private static string[] ValidateHeaders(DataTable table, int headerRowIndex, int columnCount, DataTableValidationResult result)
        {
            var headers = new string[columnCount];
            var usedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int column = 0; column < columnCount; column++)
            {
                string header = GetString(table.Rows[headerRowIndex][column]);
                headers[column] = header;

                if (string.IsNullOrWhiteSpace(header))
                    result.AddError("헤더가 비어 있습니다.", headerRowIndex + 1, column + 1);
                else if (!usedHeaders.Add(header))
                    result.AddError($"중복 헤더 '{header}'입니다.", headerRowIndex + 1, column + 1);
            }

            if (!string.Equals(headers[0], "Key", StringComparison.Ordinal))
                result.AddError("첫 번째 헤더는 'Key'여야 합니다.", headerRowIndex + 1, 1);

            return headers;
        }

        private static void ValidateRows(DataTable table, int dataStartRowIndex, string[] headers, DataTableValidationResult result, TableMetaDataType tableType)
        {
            var usedKeys = new HashSet<string>(StringComparer.Ordinal);

            for (int row = dataStartRowIndex; row < table.Rows.Count; row++)
            {
                if (IsEmptyRow(table.Rows[row], headers.Length))
                    continue;

                string rawKey = Convert.ToString(table.Rows[row][0]) ?? string.Empty;
                string key = rawKey.Trim();

                if (string.IsNullOrEmpty(key))
                {
                    result.AddError("Key가 비어 있습니다.", row + 1, 1);
                    continue;
                }

                if (!string.Equals(rawKey, key, StringComparison.Ordinal))
                    result.AddWarning($"Key '{rawKey}' 앞뒤에 공백이 있습니다.", row + 1, 1);

                if (!usedKeys.Add(key))
                    result.AddError($"중복 Key '{key}'입니다.", row + 1, 1);

                if (tableType == TableMetaDataType.LocalizationMetaData)
                    ValidateLocalizationRow(table.Rows[row], row, headers, result);
            }
        }

        private static void ValidateLocalizationRow(DataRow row, int rowIndex, string[] headers, DataTableValidationResult result)
        {
            var emptyColumns = new List<string>();
            for (int column = 1; column < headers.Length; column++)
            {
                if (IsEmpty(row[column]))
                    emptyColumns.Add(headers[column]);
            }

            if (emptyColumns.Count > 0)
                result.AddWarning($"번역 값이 빈 컬럼: {string.Join(", ", emptyColumns)}", rowIndex + 1);
        }

        private static bool IsEmptyRow(DataRow row, int columnCount)
        {
            for (int column = 0; column < columnCount; column++)
            {
                if (!IsEmpty(row[column]))
                    return false;
            }

            return true;
        }

        private static bool IsEmpty(object value) => value == null || value == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(value));
        private static string GetString(object value) => Convert.ToString(value)?.Trim() ?? string.Empty;
    }
}
