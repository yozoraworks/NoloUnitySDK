using UnityEngine;
using System.Threading.Tasks;
using ZXing;

public class qrcode : MonoBehaviour
{
    public delegate void QRScanFinished(string str); //declare a delegate to deal with the QRcode decode complete

    public RenderTexture qrcamTexture = null;
    public event QRScanFinished onQRScanFinished; //declare a event with the delegate to trigger the complete event
    public ObjectManager objManager = null;
    public Renderer renderer = null;
    static BarcodeReader codeReader = new BarcodeReader();

    string lastResult = null;

    static string result = null;

    void Start()
    {
        codeReader.AutoRotate = false;
        codeReader.TryInverted = false;
        
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
        if (result != null)
        {
            onQRScanFinished?.Invoke(result);
            result = null;
        }

        timer += Time.deltaTime;
        if (timer > 0.1f && !processing)
        {
            timer = 0;
            
            if (processing)
                return;

            //prepare use task
            //get pixels from qrcamTexture
            var tex = new Texture2D(qrcamTexture.width, qrcamTexture.height);
            RenderTexture.active = qrcamTexture;
            tex.ReadPixels(new Rect(0, 0, qrcamTexture.width, qrcamTexture.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            
            DecodeByStaticPic(tex);
        }
    }

    private static bool processing = false;

    public static async Task DecodeByStaticPic(Texture2D tex)
    {
        if (processing)
            return;

        //prepare use task
        var pixels = tex.GetPixels32();
        var width = tex.width;
        var height = tex.height;

        processing = true;
        
        await Task.Run(() =>
        {
            Result data = codeReader.Decode(pixels, width, height);
            if (data != null)
            {
                result = data.Text;
            }

            processing = false;
        });
    }

    public static async Task DecodeByStaticPic(WebCamTexture tex)
    {
        if (processing)
            return;

        //prepare use task
        var pixels = tex.GetPixels32();
        var width = tex.width;
        var height = tex.height;

        processing = true;

        await Task.Run(() =>
        {
            Result data = codeReader.Decode(pixels, width, height);
            if (data != null)
            {
                result = data.Text;
            }

            processing = false;
        });
    }
}