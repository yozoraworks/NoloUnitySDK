/// <summary>
/// write by 52cwalk,if you have some question ,please contract lycwalk@gmail.com
/// </summary>

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using System.IO;

public class qrcode : MonoBehaviour
{
    public delegate void QRScanFinished(string str); //declare a delegate to deal with the QRcode decode complete

    public event QRScanFinished onQRScanFinished; //declare a event with the delegate to trigger the complete event
    public ObjectManager objManager = null;
    public Renderer renderer = null;
    bool decoding = false;
    bool tempDecodeing = false;
    string dataText = null;
    private Color[] orginalc; //the colors of the camera data.
    private Color32[] targetColorARR; //the colors of the camera data.
    private byte[] targetbyte; //the pixels of the camera image.
    private int W, H, WxH; //width/height of the camera image			
    int framerate = 0;

#if UNITY_IOS
	int blockWidth = 450;
#elif UNITY_ANDROID
    int blockWidth = 350;
#else
	int blockWidth = 350;
#endif
    bool isInit = false;
    BarcodeReader barReader;
    string lastResult = null;

    void Start()
    {
        barReader = new BarcodeReader();
        barReader.AutoRotate = true;
        barReader.TryInverted = true;

        onQRScanFinished += (str) =>
        {
            Debug.Log("QRCode: " + str);
            if (lastResult != str)
            {
                if (objManager.gameObject.activeSelf)
                    objManager.Next();
                else
                    objManager.gameObject.SetActive(true);
            }

            lastResult = str;
        };
    }

    float timer = 0;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 0.3f)
        {
            timer = 0;

            var tex = renderer.material.mainTexture;
            //check type
            if (tex is WebCamTexture)
            {
                string data = DecodeByStaticPic((WebCamTexture)tex);
                if (!string.IsNullOrEmpty(data))
                {
                    onQRScanFinished(data);
                }
            }
            else if (tex is Texture2D)
            {
                string data = DecodeByStaticPic((Texture2D)tex);
                if (!string.IsNullOrEmpty(data))
                {
                    onQRScanFinished(data);
                }
            }
        }
    }
    
    public static string DecodeByStaticPic(Texture2D tex)
    {
        BarcodeReader codeReader = new BarcodeReader();
        codeReader.AutoRotate = true;
        codeReader.TryInverted = true;

        Result data = codeReader.Decode(tex.GetPixels32(), tex.width, tex.height);
        if (data != null)
        {
            return data.Text;
        }
        else
        {
            return null;
        }
    }

    public static string DecodeByStaticPic(WebCamTexture tex)
    {
        BarcodeReader codeReader = new BarcodeReader();
        codeReader.AutoRotate = true;
        codeReader.TryInverted = true;

        Result data = codeReader.Decode(tex.GetPixels32(), tex.width, tex.height);
        if (data != null)
        {
            return data.Text;
        }
        else
        {
            return null;
        }
    }
}