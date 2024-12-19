using System;
using System.Collections;
using UnityEngine;
using Defective.JSON;

public class DreamManager : MonoBehaviour
{
    public bool be3D = true;

    private static AndroidJavaObject TheDreamBridge = null;
    private AndroidJavaObject unityContext = null;

    public static Vector3 angle = Vector3.zero;

    private void Update()
    {
        if (GetIMU(out angle))
        {
            transform.rotation = Quaternion.Euler(new Vector3(-angle.x, -angle.z, -angle.y));
        }
    }

    private bool inited = false;

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

        if (inited)
            TheDreamBridge.Call("Set3DMode", focus && be3D ? 2 : 1);
    }

    public bool GetIMU(out Vector3 angle)
    {
        angle = Vector3.zero;

        if (!inited)
            return false;
        try
        {
            string data = TheDreamBridge.Call<string>("GetIMU");
            string[] splits = data.Split(new[] { ' ' });

            angle = new Vector3(float.Parse(splits[0]), float.Parse(splits[1]), float.Parse(splits[2]));

            return true;
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return false;
        }
    }

    private IEnumerator StartUSB()
    {
        while (true)
        {
            if (TheDreamBridge.Call<bool>("Init", unityContext))
                break;
            yield return new WaitForSeconds(0.5f);
        }

        inited = true;
        TheDreamBridge.Call("Set3DMode", be3D ? 2 : 1);
    }

    private void OnApplicationQuit()
    {
        TheDreamBridge.Call("Set3DMode", 1);

        TheDreamBridge.Call("Close");
        TheDreamBridge.Dispose();
        unityContext.Dispose();
    }
}