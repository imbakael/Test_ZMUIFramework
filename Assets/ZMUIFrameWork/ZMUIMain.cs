using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZMUIMain : MonoBehaviour {

    private void Awake() {
        UIModule.Instance.Initialize();
    }

    private void Start() {
        
    }

    private void Update() {
        //if (Input.GetKeyDown(KeyCode.Q))
        //{
        //    UIModule.Instance.PopUpWindow<PassWIndow>();
        //}

    }

}
