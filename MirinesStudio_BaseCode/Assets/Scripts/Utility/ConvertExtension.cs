using System;
using System.Collections.Generic;
using System.Linq;

namespace MirinesStudio
{
	public static class ConvertExtension
	{
        // ----- Param -----

        // ----- Init -----

        // ----- Set -----

        // ----- Get -----

        public static List<int> ToIntListFromEscapedCommas(this object obj)
        {
            if (obj == null)
                return new List<int>();
            var s = obj.ToString();
            if (string.IsNullOrWhiteSpace(s))
                return new List<int>();

            s = s.Replace("u002c", ","); // CSV 변환기로 치환된 콤마 복원

            return s.Split(',')
                    .Select(token => token.Trim())
                    .Where(token => token.Length > 0)
                    .Select(token => int.Parse(token))
                    .ToList();
        }

        private static bool IsEmpty(object obj) =>string.IsNullOrEmpty(obj.ToString());

		public static bool ToBool(this object obj) => !IsEmpty(obj)&& Convert.ToBoolean(obj);

        public static int ToInt(this object obj) => IsEmpty(obj) ? -1 : Convert.ToInt32(obj);

        public static float ToFloat(this object obj)
        {
            if(IsEmpty(obj)) 
                return -1f;

            if (float.TryParse(obj.ToString(), out var result))
                return (float)result;

            return -1f;
        }

        public static double ToDouble(this object obj)
        {
            if (IsEmpty(obj))
                return -1;

            if(double.TryParse(obj.ToString(),out var result))
                return (double)result;

            return -1;
        }

        public static double ToDoublePrecise(this object obj)
        {
            if (IsEmpty(obj))
                return -1;

            if (decimal.TryParse(obj.ToString(), out var result))
                return (double)result;

            return -1;
        }

        public static List<int> ToIntList(this object obj)
        {
            var ids = obj.ToIntListFromEscapedCommas();
            var list = new List<int>(ids.Count);
            foreach (var id in ids)
                list.Add(id);
            return list;
        }

        //public static Enumtype ToEnumType(this object obj) => IsEmpty(obj) ? EnumType.??? : (EnumType)Enum.Parse(typeof(EnumType), obj.ToString());


        // ----- Main -----

        // ----- Logic -----

    }
}