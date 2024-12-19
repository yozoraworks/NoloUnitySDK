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
    static AHRS.MahonyAHRS AHRS = new AHRS.MahonyAHRS(1f / 60f, 5f);

    private void FixedUpdate()
    {
        Vector3 acc, gyro, mag;
        if (GetIMU(out acc, out gyro, out mag))
        {
            AHRS.Update(Mathf.Deg2Rad * gyro.x, Mathf.Deg2Rad * gyro.y, Mathf.Deg2Rad * gyro.z, acc.x, acc.y, acc.z,
                mag.x, mag.y, mag.z);
            angle = new Quaternion(AHRS.Quaternion[1], AHRS.Quaternion[2], AHRS.Quaternion[3], AHRS.Quaternion[0])
                .eulerAngles;
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

    public bool GetIMU(out Vector3 acc, out Vector3 gyro, out Vector3 mag)
    {
        acc = Vector3.zero;
        gyro = Vector3.zero;
        mag = Vector3.zero;

        if (!inited)
            return false;
        try
        {
            string data = TheDreamBridge.Call<string>("GetIMU");
            string[] splits = data.Split(new[] { ' ' });

            gyro = new Vector3(float.Parse(splits[0]), float.Parse(splits[1]), float.Parse(splits[2]));
            acc = new Vector3(float.Parse(splits[3]), float.Parse(splits[4]), float.Parse(splits[5]));
            mag = new Vector3(float.Parse(splits[6]), float.Parse(splits[7]), float.Parse(splits[8]));

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