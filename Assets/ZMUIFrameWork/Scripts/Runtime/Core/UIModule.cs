using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIModule {

    private static UIModule _instance;
    public static UIModule Instance { 
        get { 
            if (_instance == null) { 
                _instance = new UIModule(); 
            } 
            return _instance; 
        } 
    }

    private Camera mUICamera;
    private Transform mUIRoot;
    private WindowConfig mWindowConfig;

    private Dictionary<string, WindowBase> mAllWindowDic = new Dictionary<string, WindowBase>(); //所有窗口的Dic
    private List<WindowBase> mVisibleWindowList = new List<WindowBase>(); //所有可见窗口的列表 

    private Queue<WindowBase> mWindowStack = new Queue<WindowBase>(); //队列， 用来管理弹窗的循环弹出
    private bool mStartPopStackWndStatus = false; //开始弹出堆栈的标志，可以用来处理多种情况，比如：正在出栈中有其他界面弹出，可以直接放到栈内进行弹出 等

    public void Initialize() {
        mUICamera = GameObject.Find("UICamera").GetComponent<Camera>();
        mUIRoot = GameObject.Find("UIRoot").transform;
        mWindowConfig = Resources.Load<WindowConfig>("WindowConfig");
#if UNITY_EDITOR
        mWindowConfig.GeneratorWindowConfig();
#endif
    }

    /// <summary>
    /// 只加载物体，不调用生命周期
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void PreLoadWindow<T>() where T : WindowBase, new() {
        string wndName = typeof(T).Name;
        GameObject go = InstantiateWindow(wndName);
        T windowBase = new T();
        if (go != null) {
            windowBase.Init(go, go.transform, go.GetComponent<Canvas>(), mUICamera, go.name);
            windowBase.OnAwake();
            windowBase.SetVisible(false);
            ResetRectTransform(go.GetComponent<RectTransform>());
            mAllWindowDic.Add(wndName, windowBase);
        }
        Debug.Log("预加载窗口 窗口名字：" + wndName);
    }

    private void ResetRectTransform(RectTransform target) {
        target.anchorMax = Vector2.one;
        target.offsetMax = Vector2.zero;
        target.offsetMin = Vector2.zero;
    }

    private GameObject InstantiateWindow(string wndName) {
        GameObject window = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(mWindowConfig.GetWindowPath(wndName)), mUIRoot);
        window.transform.localScale = Vector3.one;
        window.transform.localPosition = Vector3.zero;
        window.transform.rotation = Quaternion.identity;
        window.name = wndName;
        return window;
    }

    public T PopUpWindow<T>() where T : WindowBase, new() {
        string wndName = typeof(T).Name;
        WindowBase wnd = GetWindow(wndName);
        if (wnd != null) {
            ShowWindow(wnd);
            return wnd as T;
        }
        T t = new T();
        return InitWindow(t, wndName) as T;
    }

    private WindowBase PopUpWindow(WindowBase window) {
        string wndName = window.GetType().Name;
        WindowBase wnd = GetWindow(wndName);
        if (wnd != null) {
            ShowWindow(wnd);
            return wnd;
        }
        return InitWindow(window, wndName);
    }

    private WindowBase GetWindow(string winName) {
        if (mAllWindowDic.TryGetValue(winName, out WindowBase result)) {
            return result;
        }
        return null;
    }

    private void ShowWindow(WindowBase window) {
        if (window.gameObject != null && window.Visible == false) {
            mVisibleWindowList.Add(window);
            window.transform.SetAsLastSibling();
            window.SetVisible(true);
            SetWidnowMaskVisible();
            window.OnShow();
        } else {
            Debug.LogError(window.Name + ".gameobject == null 或者 .visible == true");
        }
    }

    private WindowBase InitWindow(WindowBase windowBase, string wndName) {
        GameObject go = InstantiateWindow(wndName);
        if (go != null) {
            windowBase.Init(go, go.transform, go.GetComponent<Canvas>(), mUICamera, go.name);
            windowBase.transform.SetAsLastSibling();
            windowBase.OnAwake();
            windowBase.SetVisible(true);
            windowBase.OnShow();
            ResetRectTransform(go.GetComponent<RectTransform>());
            mAllWindowDic.Add(wndName, windowBase);
            mVisibleWindowList.Add(windowBase);
            SetWidnowMaskVisible();
            return windowBase;
        }
        Debug.LogError("没有加载到对应的窗口 窗口名字：" + wndName);
        return null;
    }

    public void HideWindow(string wndName) {
        WindowBase window = GetWindow(wndName);
        HideWindow(window);
    }
    
    private void HideWindow(WindowBase window) {
        if (window != null && window.Visible) {
            mVisibleWindowList.Remove(window);
            window.SetVisible(false);
            SetWidnowMaskVisible();
            window.OnHide();
        }
        PopNextStackWindow(window);
    }

    public void DestroyAllWindow(List<string> filterlist = null) {
        var all = new List<WindowBase>(mAllWindowDic.Values);
        for (int i = all.Count - 1; i >= 0; i--) {
            WindowBase window = all[i];
            if (window == null || (filterlist != null && filterlist.Contains(window.Name))) {
                continue;
            }
            DestroyWindow(window.Name);
        }
        Resources.UnloadUnusedAssets();
    }

    private void DestroyWindow(string wndName) {
        WindowBase window = GetWindow(wndName);
        if (window == null) {
            return;
        }
        DestoryWindow(window);
    }

    private void DestoryWindow(WindowBase window) {
        if (mAllWindowDic.ContainsKey(window.Name)) {
            mAllWindowDic.Remove(window.Name);
            mVisibleWindowList.Remove(window);
        }
        window.SetVisible(false);
        SetWidnowMaskVisible();
        window.OnHide();
        window.OnDestroy();
        UnityEngine.Object.Destroy(window.gameObject);
        PopNextStackWindow(window);
    }

    private void SetWidnowMaskVisible() {
        if (mVisibleWindowList.Count == 0) {
            return;
        }
        int maxOrder = mVisibleWindowList.Max(t => t.Canvas.sortingOrder);
        WindowBase maxOrderWndBase =
            mVisibleWindowList
            .Where(t => t.Canvas.sortingOrder == maxOrder)
            .OrderByDescending(t => t.transform.GetSiblingIndex())
            .First();
        foreach (WindowBase item in mVisibleWindowList) {
            item.SetMaskVisible(item == maxOrderWndBase);
        }
    }

    #region 堆栈系统

    public void PushAndPopStackWindow<T>(Action<WindowBase> popCallBack = null) where T : WindowBase, new() {
        PushWindowToStack<T>(popCallBack);
        StartPopFirstStackWindow();
    }

    public void PushWindowToStack<T>(Action<WindowBase> popCallBack = null) where T : WindowBase, new() {
        T wndBase = new T();
        wndBase.PopStackListener = popCallBack;
        mWindowStack.Enqueue(wndBase);
    }

    public void StartPopFirstStackWindow() {
        if (mStartPopStackWndStatus) {
            return;
        }
        mStartPopStackWndStatus = true;//已经开始进行堆栈弹出的流程，
        PopStackWindow();
    }

    private void PopStackWindow() {
        if (mWindowStack.Count > 0) {
            WindowBase window = mWindowStack.Dequeue();
            WindowBase popWindow = PopUpWindow(window);
            popWindow.PopStackListener = window.PopStackListener;
            popWindow.PopStack = true;
            popWindow.PopStackListener?.Invoke(popWindow);
            popWindow.PopStackListener = null;
        } else {
            mStartPopStackWndStatus = false;
        }
    }

    private void PopNextStackWindow(WindowBase windowBase) {
        if (windowBase != null && mStartPopStackWndStatus && windowBase.PopStack) {
            windowBase.PopStack = false;
            PopStackWindow();
        }
    }

    #endregion
}

