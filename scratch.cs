using UnityEngine;
public class Test {
    public void Run() {
        var objs = Resources.FindObjectsOfTypeAll(typeof(ScriptableObject));
        var objs2 = Object.FindObjectsOfType(typeof(MonoBehaviour));
    }
}
