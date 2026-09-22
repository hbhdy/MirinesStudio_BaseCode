using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MirinesStudio
{
    public class ResourceManager : BaseManager
    {
        // ----- Param -----

        public ResourcesData resourcesData;

        // ----- Init -----

        public override async UniTask Task_Init()
        {
            await base.Task_Init();
        }

        public override void Init()
        {
            base.Init();

            resourcesData.Init();
            CreateObjectPool();
        }

        // ----- Set -----

        // ----- Get -----

        // ----- Main -----

        // ----- Logic -----

        public void CreateObjectPool()
        {
            //ObjectPool.CreatePool(resourcesData., 100);
        }
    }
}