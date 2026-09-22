using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace MirinesStudio
{
    public class DataManager : BaseManager
    {
        // ----- Param -----

        // ≈ÿΩ∫∆Æ
        public static List<LocalizationMetaData> DB_LocalizationMetaData = new();

        private Dictionary<string, LocalizationMetaData> dicLocalizationMetaData = new();

        // ----- Init -----

        public override async UniTask Task_Init()
        {
            LoadDB();
            await base.Task_Init();
        }

        public override void Init()
        {
            dicLocalizationMetaData.Clear();
            foreach (var data in DB_LocalizationMetaData)
                dicLocalizationMetaData.Add(data.Key, data);

            base.Init();
        }

        // ----- Set -----

        // ----- Get -----

        public LocalizationMetaData GetLocalizationMetaData(string key) => dicLocalizationMetaData.ContainsKey(key) ? dicLocalizationMetaData[key] : null;

        // ----- Main -----

        // ----- Logic -----

        public void LoadDB()
        {
            for (TableMetaDataType type = TableMetaDataType.None; type < TableMetaDataType.Count; type++)
            {
                if (IsCheckNoneSetupDataType(type))
                {
                    MSLog.Log($"NoneSetup File : {type}");
                    continue;
                }

                SetDataDB(type, Resources.Load<TextAsset>($"CSV/{type}"));
            }
        }

        private bool IsCheckNoneSetupDataType(TableMetaDataType type)
        {
            return type switch
            {
                TableMetaDataType.None => true,
                _ => false,
            };
        }

        private void SetDataDB(TableMetaDataType type, TextAsset data)
        {
            if (data == null)
            {
                MSLog.LogError("TextAsset data is Null, Type : " + type.ToString());
                return;
            }

            List<Dictionary<string, object>> obj = CSVReader.Read(data);

            switch (type)
            {
                case TableMetaDataType.LocalizationMetaData:
                    DB_LocalizationMetaData = LocalizationMetaData.Create(obj);
                    break;
            }

            MSLog.Log($"{type} Set DB");
        }
    }
}