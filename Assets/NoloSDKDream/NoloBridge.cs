using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Threading;

public class NoloBridge : MonoBehaviour
{
    AndroidJavaObject noloClass = null;

    public float eyeDistance = 0.0315f;
    public float step = 0.0005f;
    
    public class deviceInfo
    {
        public int deviceID; //0 head, 1 controller1, 2 controller2, 3 tracking base
        public Vector3 position;
        public Quaternion rotation;

        public int button; //bit code

        //buttons
        public bool trigger, touched, touchpadPressed, grip, menu, system;

        public int touch; //0 or 1
        public Vector2 touchAxis;
    }

    public static deviceInfo[] devices = new deviceInfo[4];
    
    void InitNolo()
    {
        if (noloClass == null)
        {
            noloClass = new AndroidJavaObject("com.unity3d.player.NoloBridge");
        }

        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                using (AndroidJavaObject unityContext = currentActivity.Call<AndroidJavaObject>("getApplicationContext"))
                {
                    string r = noloClass.Call<string>("InitNolo", unityContext);
                    Debug.Log("Init: " + r);
                }
            }
        }
    }

    void GetDevicesData()
    {
        if (noloClass == null)
        {
            return;
        }

        try
        {
            string r = noloClass.Call<string>("GetDevicesData");

            JObject json = JObject.Parse(r);

            if (json["success"].ToString() != "true")
            {
                Debug.Log(json["error"].ToString());
            }

            for (int deviceID = 0; deviceID < 4; deviceID++)
            {
                JObject deviceJson = (JObject)json["D" + deviceID];
                int status = int.Parse(deviceJson["status"].ToString());

                Vector3 pos = new Vector3(float.Parse(deviceJson["x"].ToString()), float.Parse(deviceJson["y"].ToString()), float.Parse(deviceJson["z"].ToString()));
                Quaternion rot = new Quaternion(float.Parse(deviceJson["rx"].ToString()), float.Parse(deviceJson["ry"].ToString()), float.Parse(deviceJson["rz"].ToString()), float.Parse(deviceJson["rw"].ToString()));

                devices[deviceID].deviceID = deviceID;
                devices[deviceID].position = pos;
                devices[deviceID].rotation = rot;

                int button = int.Parse(deviceJson["button"].ToString());
                devices[deviceID].button = button;

                devices[deviceID].trigger = (button >> 1 & 0x1) == 1;
                devices[deviceID].touched = (button >> 5 & 0x1) == 1;
                devices[deviceID].touchpadPressed = (button >> 0 & 0x1) == 1;
                devices[deviceID].grip = (button >> 4 & 0x1) == 1;
                devices[deviceID].menu = (button >> 2 & 0x1) == 1;
                devices[deviceID].system = (button >> 3 & 0x1) == 1;

                devices[deviceID].touch = int.Parse(deviceJson["touch"].ToString()); ;
                devices[deviceID].touchAxis = new Vector2(float.Parse(deviceJson["touchx"].ToString()), float.Parse(deviceJson["touchy"].ToString()));
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message + "\n" + e.StackTrace);
        }
    }

    void OnDestroy()
    {
        if (noloClass != null)
        {
            noloClass.Call("OnClose");
            noloClass.Dispose();
        }
    }

    void Test()
    {
        if (noloClass == null)
        {
            noloClass = new AndroidJavaObject("com.unity3d.player.NoloBridge");
        }

        int r = noloClass.Call<int>("Add", 1, 2);
        Debug.Log("test: " + r.ToString());
    }
    
    void Awake()
    {
        //Test();
        for (int i = 0; i < 4; i++)
        {
            devices[i] = new deviceInfo();
        }
    }

    void Start()
    {
        InitNolo();

        StartCoroutine(GetDeviceLoop());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            eyeDistance -= step;
            eyeDistance = Mathf.Clamp(eyeDistance, 0.01f, 0.05f);
            transform.GetChild(0).localPosition = new Vector3(-eyeDistance, transform.GetChild(0).localPosition.y, transform.GetChild(0).localPosition.z);
            transform.GetChild(1).localPosition = new Vector3(eyeDistance, transform.GetChild(1).localPosition.y, transform.GetChild(1).localPosition.z);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            eyeDistance += step;
            eyeDistance = Mathf.Clamp(eyeDistance, 0.01f, 0.05f);
            transform.GetChild(0).localPosition = new Vector3(-eyeDistance, transform.GetChild(0).localPosition.y, transform.GetChild(0).localPosition.z);
            transform.GetChild(1).localPosition = new Vector3(eyeDistance, transform.GetChild(1).localPosition.y, transform.GetChild(1).localPosition.z);
        }
    }

    IEnumerator GetDeviceLoop()
    {
        while (true)
        {
            GetDevicesData();
            yield return new WaitForSecondsRealtime(0.015f);
        }
    }
}