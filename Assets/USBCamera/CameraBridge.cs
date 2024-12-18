using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class CameraBridge : MonoBehaviour
{
    [DllImport("rgbbuf")]
    protected static extern System.IntPtr GetBuffer();

    //these resolution will change to the max supperted resolution of camera
    public bool ready = false;
    public int rgbWidth = 1920;
    public int rgbHeight = 1080;

    private AndroidJavaObject plugin;

    private Texture2D RGBImage = null;
    public Material material;
    private byte[] rgbBytes;
    
    // Start is called before the first frame update
    void Start()
    {
        AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
        plugin = new AndroidJavaObject("com.dreamworldvision.dreamworldunityplugin.RGBCamera");
        plugin.Call("Init", jo);
        plugin.Call("start");
        StartCoroutine(TryGetResolution());
    }

    void OnApplicationQuit()
    {
        plugin.Call("stop");
    }

    IEnumerator TryGetResolution()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            string s = plugin.Call<string>("GetVideoInfo");

            string[] ss = s.Split(' ');
            if (ss[0] == "1") //ready?
            {
                rgbWidth = int.Parse(ss[1]);
                rgbHeight = int.Parse(ss[2]);

                Debug.Log("Ready with resolution: " + rgbWidth + "x" + rgbHeight);

                ready = true;

                break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (ready)
        {
            try
            {
                if (RGBImage == null)
                {
                    RGBImage = new Texture2D(rgbWidth, rgbHeight, TextureFormat.RGBA32, false);
                    if (material != null)
                    {
                        material.mainTexture = RGBImage;
                    }
                }

                System.IntPtr bufferPointer = GetBuffer();
                if (bufferPointer != System.IntPtr.Zero)
                {
                    RGBImage.LoadRawTextureData(bufferPointer, rgbWidth * rgbHeight * 4);
                    RGBImage.Apply();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
