using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DreamManager : MonoBehaviour
{
    public bool disableDistort = true, be3D = true;

    public static AndroidJavaObject TheDreamBridge = null;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnApplicationFocus(bool focus)
    {
        if (TheDreamBridge == null)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject unityContext = currentActivity.Call<AndroidJavaObject>("getApplicationContext");

            TheDreamBridge = new AndroidJavaObject("com.unity3d.player.DreamGlassBridge");
            TheDreamBridge.Call("InitDream", unityContext);

            unityPlayer.Dispose();
            currentActivity.Dispose();
            unityContext.Dispose();
        }

        TheDreamBridge.Call("SetDistortionEnabled", disableDistort && focus);
        TheDreamBridge.Call("Set3DMode", focus && be3D ? 2 : 0);
    }

    private void OnApplicationQuit()
    {
        TheDreamBridge.Call("SetDistortionEnabled", false);
        TheDreamBridge.Call("Set3DMode", 0);

        TheDreamBridge.Call("OnClose");
        TheDreamBridge.Dispose();
    }
}
