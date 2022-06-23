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
    private List<WindowBase> mAllWindowList = new List<WindowBase>(); //所有窗口的列表
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

    #region 窗口管理

    /// <summary>
    /// 只加载物体，不调用生命周期
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void PreLoadWindow<T>() where T : WindowBase, new() {
        System.Type type = typeof(T);
        string wndName = type.Name;
        T windowBase = new T();
        //克隆界面，初始化界面信息
        //1.生成对应的窗口预制体
        GameObject nWnd = TempLoadWindow(wndName);
        //2.初始出对应管理类
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
            mAllWindowList.Add(windowBase);
        }
        Debug.Log("预加载窗口 窗口名字：" + wndName);
    }

    /// <summary>
    /// 弹出一个弹窗
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T PopUpWindow<T>() where T : WindowBase, new() {
        string wndName = typeof(T).Name;
        WindowBase wnd = GetWindow(wndName);
        if (wnd != null) {
            return ShowWindow(wndName) as T;
        }
        T t = new T();
        return InitializeWindow(t, wndName) as T;
    }

    private WindowBase PopUpWindow(WindowBase window) {
        string wndName = window.GetType().Name;
        WindowBase wnd = GetWindow(wndName);
        if (wnd != null) {
            return ShowWindow(wndName);
        }
        return InitializeWindow(window, wndName);
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
            mAllWindowList.Add(windowBase);
            mVisibleWindowList.Add(windowBase);
            SetWidnowMaskVisible();
            return windowBase;
        }
        Debug.LogError("没有加载到对应的窗口 窗口名字：" + wndName);
        return null;
    }
    
    private WindowBase ShowWindow(string winName) {
        if (mAllWindowDic.TryGetValue(winName, out WindowBase window)) {
            if (window.gameObject != null && window.Visible == false) {
                mVisibleWindowList.Add(window);
                window.transform.SetAsLastSibling();
                window.SetVisible(true);
                SetWidnowMaskVisible();
                window.OnShow();
            }
            return window;
        }
        Debug.LogError(winName + " 窗口不存在，请调用PopUpWindow 进行弹出");
        return null;
    }
    private WindowBase GetWindow(string winName) {
        if (mAllWindowDic.TryGetValue(winName, out WindowBase result)) {
            return result;
        }
        return null;
    }

    /// <summary>
    /// 获取已经弹出的弹窗
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetVisibleWindow<T>() where T : WindowBase {
        Type type = typeof(T);
        foreach (WindowBase item in mVisibleWindowList) {
            if (item.Name == type.Name) {
                return (T)item;
            }
        }
        Debug.LogError("该窗口没有获取到：" + type.Name);
        return null;
    }

    public void HideWindow<T>() where T : WindowBase {
        HideWindow(typeof(T).Name);
    }

    public void HideWindow(string wndName) {
        WindowBase window = GetWindow(wndName);
        HideWindow(window);
    }
    
    private void HideWindow(WindowBase window) {
        if (window != null && window.Visible) {
            mVisibleWindowList.Remove(window);
            window.SetVisible(false);//隐藏弹窗物体
            SetWidnowMaskVisible();
            window.OnHide();
        }
        //在出栈的情况下，上一个界面隐藏时，自动打开栈中的下一个界面
        PopNextStackWindow(window);
    }

    public void DestroyWinodw<T>() where T : WindowBase {
        DestroyWindow(typeof(T).Name);
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
            mAllWindowList.Remove(window);
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

    public void DestroyAllWindow(List<string> filterlist = null) {
        for (int i = mAllWindowList.Count - 1; i >= 0; i--) {
            WindowBase window = mAllWindowList[i];
            if (window == null || (filterlist != null && filterlist.Contains(window.Name))) {
                continue;
            }
            DestroyWindow(window.Name);
        }
        Resources.UnloadUnusedAssets();
    }

    private void SetWidnowMaskVisible() {
        if (!UISetting.Instance.SINGMASK_SYSTEM) {
            return;
        }
        WindowBase maxOrderWndBase = null;//最大渲染层级的窗口
        int maxOrder = 0;//最大渲染层级
        int maxIndex = 0;//最大排序下标 在相同父节点下的位置下标
        //1.关闭所有窗口的Mask 设置为不可见
        //2.从所有可见窗口中找到一个层级最大的窗口，把Mask设置为可见
        for (int i = 0; i < mVisibleWindowList.Count; i++) {
            WindowBase window = mVisibleWindowList[i];
            if (window != null && window.gameObject != null) {
                window.SetMaskVisible(false);
                if (maxOrderWndBase == null) {
                    maxOrderWndBase = window;
                    maxOrder = window.Canvas.sortingOrder;
                    maxIndex = window.transform.GetSiblingIndex();
                } else {
                    //找到最大渲染层级的窗口，拿到它
                    if (maxOrder < window.Canvas.sortingOrder) {
                        maxOrderWndBase = window;
                        maxOrder = window.Canvas.sortingOrder;
                    }
                    //如果两个窗口的渲染层级相同，就找到同节点下最靠下一个物体，优先渲染Mask
                    else if (maxOrder == window.Canvas.sortingOrder && maxIndex < window.transform.GetSiblingIndex()) {
                        maxOrderWndBase = window;
                        maxIndex = window.transform.GetSiblingIndex();
                    }
                }
            }
        }
        if (maxOrderWndBase != null) {
            maxOrderWndBase.SetMaskVisible(true);
        }
    }

    private GameObject TempLoadWindow(string wndName) {
        GameObject window = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(mWindowConfig.GetWindowPath(wndName)), mUIRoot);
        window.transform.localScale = Vector3.one;
        window.transform.localPosition = Vector3.zero;
        window.transform.rotation = Quaternion.identity;
        window.name = wndName;
        return window;
    }
    #endregion

    #region 堆栈系统

    /// <summary>
    /// 压入并且弹出堆栈弹窗
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="popCallBack"></param>
    public void PushAndPopStackWindow<T>(Action<WindowBase> popCallBack = null) where T : WindowBase, new() {
        PushWindowToStack<T>(popCallBack);
        StartPopFirstStackWindow();
    }

    /// <summary>
    /// 进栈一个界面
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="popCallBack"></param>
    public void PushWindowToStack<T>(Action<WindowBase> popCallBack = null) where T : WindowBase, new() {
        T wndBase = new T();
        wndBase.PopStackListener = popCallBack;
        mWindowStack.Enqueue(wndBase);
    }

    /// <summary>
    /// 弹出堆栈中第一个弹窗
    /// </summary>
    public void StartPopFirstStackWindow() {
        if (mStartPopStackWndStatus) 
            return;
        mStartPopStackWndStatus = true;//已经开始进行堆栈弹出的流程，
        PopStackWindow();
    }

    /// <summary>
    /// 弹出堆栈中的下一个窗口
    /// </summary>
    /// <param name="windowBase"></param>
    private void PopNextStackWindow(WindowBase windowBase) {
        if (windowBase != null && mStartPopStackWndStatus && windowBase.PopStack) {
            windowBase.PopStack = false;
            PopStackWindow();
        }
    }

    /// <summary>
    /// 弹出堆栈弹窗
    /// </summary>
    /// <returns></returns>
    public bool PopStackWindow() {
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

    public void ClearStackWindows() {
        mWindowStack.Clear();
    }

    #endregion
}

