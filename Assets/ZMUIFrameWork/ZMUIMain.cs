using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZMUIMain : MonoBehaviour {

    private void Awake() {
        UIModule.Instance.Initialize();
    }

    private void Start() {
        UIModule.Instance.PopUpWindow<LoginWIndow>();
        UIModule.Instance.PreLoadWindow<HallWindow>();
        UIModule.Instance.PreLoadWindow<UserInfoWIndow>();
        UIModule.Instance.PreLoadWindow<SettingWIndow>();
        UIModule.Instance.PreLoadWindow<ChatWIndow>();
        UIModule.Instance.PreLoadWindow<FriendWIndow>();
    }

    private void Update() {
        //if (Input.GetKeyDown(KeyCode.Q))
        //{
        //    UIModule.Instance.PopUpWindow<PassWIndow>();
        //}

        if (Input.GetKeyDown(KeyCode.W)) {
            Debug.Log("PushAndPopStackWindow<UserInfoWIndow>");
            UIModule.Instance.PushAndPopStackWindow<UserInfoWIndow>();
        }
        if (Input.GetKeyDown(KeyCode.E)) {
            Debug.Log("PushAndPopStackWindow<SettingWIndow>");
            UIModule.Instance.PushAndPopStackWindow<SettingWIndow>();
        }
    }

}
