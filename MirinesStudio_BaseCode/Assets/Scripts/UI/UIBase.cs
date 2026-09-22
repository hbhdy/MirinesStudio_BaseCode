using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

namespace MirinesStudio
{
    [AttributeUsage(AttributeTargets.All)]
    public class UIAttrTypeAttribute : Attribute
    {
        public string uiResourceName { get; private set; } = string.Empty;

        public UIAttrTypeAttribute(string uiResourceName)
        {
            this.uiResourceName = uiResourceName;
        }
    }

    public class UIAttrUtil
    {
        static T GetTypeAttribute<T>(Type type, string enumString)
        {
            System.Reflection.MemberInfo[] infos = type.GetMember(enumString);
            if (infos.Length == 0)
                return default;

            object[] attributes = infos[0].GetCustomAttributes(typeof(T), false);
            if (attributes.Length == 0)
                return default;

            return (T)attributes[0];
        }

        public static string GetUIAttributeResourceName<T>(T t)
        {
            UIAttrTypeAttribute customAttr = GetTypeAttribute<UIAttrTypeAttribute>(t.GetType(), t.ToString());
            return customAttr == null ? string.Empty : customAttr.uiResourceName;
        }
    }

    public abstract class UIBase : MonoBehaviour
    {
        // ----- Param -----

        [Header("OpenCloseAnim")]
        public GameObject objOpenAnim;

        public abstract UIType UIType { get; }
        public Canvas_SortOrder sortOrder;

        private Canvas baseCanvas;

        public Action callOpenAfter;
        public Action callOpenBefore;
        public Action callCloseAfter;
        public Action callCloseBefore;

        private DOTweenAnimation[] openCloseAnim = null;


        // ----- Init -----

        private void Awake()
        {
            OnAwake();

            if (objOpenAnim)
                openCloseAnim = objOpenAnim.GetComponents<DOTweenAnimation>();
        }

        protected virtual void OnAwake()
        {
            baseCanvas = GetComponent<Canvas>();
        }

        public virtual void OpenUI()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (objOpenAnim)
            {
                OnAnimation("0", CallOpenAfter);
            }
            else
                CallOpenAfter();
        }

        public virtual void CloseUI()
        {
            if (objOpenAnim)
            {
                OnAnimation("1", CallCloseAfter_Anim);
            }
            else
            {
                this.gameObject.SetActive(false);
                baseCanvas.sortingOrder = 0;
                CallCloseAfter();
            }
        }

        private void OnAnimation(string id, UnityAction callback = null)
        {
            if (openCloseAnim != null)
            {
                for (int i = 0; i < openCloseAnim.Length; ++i)
                {
                    if (openCloseAnim[i].id == id)
                    {
                        if (callback != null)
                            SetAnimCompleteCallback(openCloseAnim[i], callback);
                        openCloseAnim[i].DOPlayById(id);
                        return;
                    }
                }
            }
            callback?.Invoke();
        }

        public void DoRestartAnimation(string id)
        {
            if (openCloseAnim != null)
            {
                for (int i = 0; i < openCloseAnim.Length; ++i)
                {
                    if (openCloseAnim[i].id == id)
                    {
                        switch (openCloseAnim[i].id)
                        {
                            case "0":
                                SetAnimCompleteCallback(openCloseAnim[i], CallOpenAfter);
                                break;
                            case "1":
                                SetAnimCompleteCallback(openCloseAnim[i], CallCloseAfter_Anim);
                                break;
                        }
                        openCloseAnim[i].DORestart();
                        return;
                    }
                }
            }
        }

        private void SetAnimCompleteCallback(DOTweenAnimation anim, UnityAction callback)
        {
            if (anim.onComplete != null)
            {
                anim.onComplete.RemoveAllListeners();
                anim.onComplete.AddListener(callback);
            }
        }

        public virtual void CallOpenAfter()
        {
            callOpenAfter?.Invoke();
            callOpenAfter = null;
        }

        public virtual void CallCloseAfter()
        {
            callCloseAfter?.Invoke();
            callCloseAfter = null;
        }

        public void CallCloseAfter_Anim()
        {
            gameObject.SetActive(false);
            callCloseAfter?.Invoke();
            callCloseAfter = null;
        }


        // ----- Get -----

        public Canvas GetCanvas()
        {
            return baseCanvas;
        }
    }
}
