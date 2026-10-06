using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MirinesStudio
{
	public class BaseManager : MonoBehaviour
	{
		// ----- Param -----

		protected bool isReady = false;

		// ----- Init -----

		public virtual async UniTask Task_Init()
		{
			await UniTask.Yield();

			isReady = true;
			MSLog.Log($"{name} Task Init Success");
		}

		public virtual void Init()
		{
            MSLog.Log($"{name} Init Success");
        }

		public virtual void Shutdown()
		{

		}

		// ----- Set -----

		// ----- Get -----

		// ----- Main -----

		// ----- Logic -----

	}
}
