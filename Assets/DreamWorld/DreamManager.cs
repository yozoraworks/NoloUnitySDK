using System.Collections;
using UnityEngine;

public class DreamManager : MonoBehaviour
{
    public bool be3D = true;

    private static AndroidJavaObject TheDreamBridge = null;
    private AndroidJavaObject unityContext = null;

    private void OnApplicationFocus(bool focus)
    {
        if (TheDreamBridge == null)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            unityContext = currentActivity.Call<AndroidJavaObject>("getApplicationContext");

            TheDreamBridge = new AndroidJavaObject("com.unity3d.player.DreamGlassBridge");

            StartCoroutine(StartUSB());

            unityPlayer.Dispose();
            currentActivity.Dispose();
        }

        TheDreamBridge.Call("Set3DMode", focus && be3D ? 2 : 1);
    }

    public bool GetIMU(out Vector3 pos, out Vector3 rot)
    {
        string data = TheDreamBridge.Call<string>("GetIMU");
        if (string.IsNullOrEmpty(data))
        {
            pos = Vector3.zero;
            rot = Vector3.zero;
            return false;
        }
        string[] values = data.Split(',');

        pos = new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
        rot = new Vector3(float.Parse(values[3]), float.Parse(values[4]), float.Parse(values[5]));
        return true;
    }

    private IEnumerator StartUSB()
    {
        while (true)
        {
            if (TheDreamBridge.Call<bool>("Init", unityContext))
                break;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void OnApplicationQuit()
    {
        TheDreamBridge.Call("Set3DMode", 1);

        TheDreamBridge.Call("Close");
        TheDreamBridge.Dispose();
        unityContext.Dispose();
    }
}