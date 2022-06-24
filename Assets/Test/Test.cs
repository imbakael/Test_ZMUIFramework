using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Test : MonoBehaviour {

    private void Start() {
        //List<Student> list = new List<Student> {
        //    new Student { name = "张三", order = 33, index = 1},
        //    new Student { name = "李四", order = 2, index = 3},
        //    new Student { name = "王五", order = 67, index = 1},
        //    new Student { name = "罗翔", order = 6, index = 5},
        //    new Student { name = "孙继海", order = 67, index = 2},
        //};
        //int maxOrder = list.Max(t => t.order);
        //var ss =
        //    list
        //    .Where(t => t.order == maxOrder)
        //    .OrderByDescending(t => t.index);
        //foreach (var item in ss) {
        //    Debug.Log("name = " + item.name);
        //}
        //Debug.Log("first = " + ss.First().name);
    }
}

public class Student {
    public string name;
    public int order;
    public int index;
}
