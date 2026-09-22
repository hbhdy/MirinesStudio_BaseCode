using System;
using System.Collections.Generic;

namespace MirinesStudio
{

    [Serializable]
    public class LocalizationMetaData
    {
        // ----- Param -----

        public string Key;
        public string Korean;
        public string English;

        // ----- Init -----

        public static List<LocalizationMetaData> Create(List<Dictionary<string, object>> data)
        {
            List<LocalizationMetaData> list = new();
            int cnt = data.Count;
            int index = 0;

            for (int i = 0; i < cnt; i++)
            {
                index = 0;
                List<object> values = new List<object>(data[i].Values);
                list.Add(new LocalizationMetaData
                {
                    Key = values[index++].ToString(),
                    Korean = values[index++].ToString(),
                    English = values[index++].ToString(),

                });
            }

            return list;
        }

        // ----- Set -----

        // ----- Get -----

        // ----- Main -----

        // ----- Logic -----

    }

}