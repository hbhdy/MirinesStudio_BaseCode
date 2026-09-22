using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

public class CSVReader
{
    private static readonly string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    private static readonly string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    private static readonly char[] TRIM_CHARS = { '"' };

    public static List<Dictionary<string, object>> Read(TextAsset data)
    {
        const NumberStyles style = NumberStyles.Any;
        var invariantCulture = CultureInfo.InvariantCulture;

        var list = new List<Dictionary<string, object>>();

        var lines = Regex.Split(data.text, LINE_SPLIT_RE);
        if (lines.Length <= 1)
            return list;

        var header = Regex.Split(lines[0], SPLIT_RE);
        for (var i = 1; i < lines.Length; i++)
        {
            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || string.IsNullOrWhiteSpace(values[0]))
                continue;

            var entry = new Dictionary<string, object>();
            for (var j = 0; j < header.Length && j < values.Length; j++)
            {
                string value = values[j].TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS);

                value = value
                   .Replace("\\r\\n", "\n")   // CSV에 \r\n 있을 때
                   .Replace("\\n", "\n")      // CSV에 \n 있을 때
                   .Replace("\\t", "\t")      // 탭 문자도 지원
                   .Replace("\\r", "\n");

                object finalValue = value;
                if (int.TryParse(value, style, invariantCulture, out int n))
                    finalValue = n;
                else if (float.TryParse(value, style, invariantCulture, out float f))
                    finalValue = f;

                entry[header[j]] = finalValue;
            }

            list.Add(entry);
        }
        return list;
    }
}