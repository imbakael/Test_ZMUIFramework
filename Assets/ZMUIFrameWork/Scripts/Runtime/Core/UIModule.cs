using System;
using System.Collections.Generic;
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
        T windowBase = new T();
        GameObject nWnd = TempLoadWindow(wndName);
        if (nWnd != null) {
            windowBase.gameObject = nWnd;
            windowBase.transform = nWnd.transform;
            windowBase.Canvas = nWnd.GetComponent<Canvas>();
            windowBase.Canvas.worldCamera = mUICamera;
            windowBase.Name = nWnd.name;
            windowBase.OnAwake();
            windowBase.SetVisible(false);
            RectTransform rectTrans = nWnd.GetComponent<RectTransform>();
            rectTrans.anchorMax = Vector2.one;
            rectTrans.offsetMax = Vector2.zero;
            rectTrans.offsetMin = Vector2.zero;
            mAllWindowDic.Add(wndName, windowBase);
        }
        Debug.Log("预加载窗口 窗口名字：" + wndName);
    }

    private GameObject TempLoadWindow(string wndName) {
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
        return InitializeWindow(t, wndName) as T;
    }

    private WindowBase PopUpWindow(WindowBase window) {
        string wndName = window.GetType().Name;
        WindowBase wnd = GetWindow(wndName);
        if (wnd != null) {
            ShowWindow(wnd);
            return wnd;
        }
        return InitializeWindow(window, wndName);
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

    private WindowBase InitializeWindow(WindowBase windowBase, string wndName) {
        //1.生成对应的窗口预制体
        GameObject nWnd = TempLoadWindow(wndName);
        //2.初始出对应管理类
        if (nWnd != null) {
            windowBase.gameObject = nWnd;
            windowBase.transform = nWnd.transform;
            windowBase.Canvas = nWnd.GetComponent<Canvas>();
            windowBase.Canvas.worldCamera = mUICamera;
            windowBase.transform.SetAsLastSibling();
            windowBase.Name = nWnd.name;
            windowBase.OnAwake();
            windowBase.SetVisible(true);
            windowBase.OnShow();
            RectTransform rectTrans = nWnd.GetComponent<RectTransform>();
            rectTrans.anchorMax = Vector2.one;
            rectTrans.offsetMax = Vector2.zero;
            rectTrans.offsetMin = Vector2.zero;
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
        //在出栈的情况下，上一个界面隐藏时，自动打开栈中的下一个界面
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
        DestoryWindow(window);
    }

    private void DestoryWindow(WindowBase window) {
        if (window == null) {
            return;
        }
        if (mAllWindowDic.ContainsKey(window.Name)) {
            mAllWindowDic.Remove(window.Name);
            mVisibleWindowList.Remove(window);
        }
        window.SetVisible(false);
        SetWidnowMaskVisible();
        window.OnHide();
        window.OnDestroy();
        UnityEngine.Object.Destroy(window.gameObject);
        //在出栈的情况下，上一个界面销毁时，自动打开栈种的下一个界面
        PopNextStackWindow(window);
    }

    private void SetWidnowMaskVisible() {
        WindowBase maxOrderWndBase = null;
        int maxOrder = 0;
        int maxIndex = 0;

        //1.关闭所有窗口的Mask 设置为不可见
        //2.从所有可见窗口中找到一个层级最大的窗口，把Mask设置为可见
        foreach (WindowBase window in mVisibleWindowList) {
            Debug.Assert(window != null && window.gameObject != null, "window = null!");
            int currentOrder = window.Canvas.sortingOrder;
            int currentIndex = window.transform.GetSiblingIndex();
            if (maxOrderWndBase == null) {
                maxOrderWndBase = window;
                maxOrder = currentOrder;
                maxIndex = currentIndex;
            } else {
                if (maxOrder < currentOrder) {
                    maxOrder = currentOrder;
                    maxOrderWndBase = window;
                } else if (maxOrder == currentOrder && maxIndex < currentIndex) {
                    maxOrderWndBase = window;
                    maxIndex = currentIndex;
                }
            }
        }
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
        if (mStartPopStackWndStatus) 
            return;
        mStartPopStackWndStatus = true;//已经开始进行堆栈弹出的流程，
        PopStackWindow();
    }

    private bool PopStackWindow() {
        if (mWindowStack.Count > 0) {
            WindowBase window = mWindowStack.Dequeue();
            WindowBase popWindow = PopUpWindow(window);
            popWindow.PopStackListener = window.PopStackListener;
            popWindow.PopStack = true;
            popWindow.PopStackListener?.Invoke(popWindow);
            popWindow.PopStackListener = null;
            return true;
        } else {
            mStartPopStackWndStatus = false;
            return false;
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

