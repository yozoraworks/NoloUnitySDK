using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace ChaosIkaros
{
    public class StreamingClient : MonoBehaviour
    {
        public class Frame
        {
            public bool useUDP = false;
            public UdpClient udpClient;
            public TcpClient receiverClient;
            public int frameWidth = 640;
            public int frameHeight = 480;
            public int frameLength = 0;
            public int videoQuality = 100;//1-100
            public int compressFormat = 0;
            public int frameID = 0;
            public byte[] rawBytes = null;
            public bool emptyFrame = false;
            public bool downloaded = false;
            public bool used = false;
            public int threadCount = 0;
            private bool connected = false;
            private NetworkStream serverStream;
            private string rawMsg = "";
            private byte[] frameMsg = null;
            private IPEndPoint localIP = null;
            public Frame(int frameID)
            {
                frameID = this.frameID;
                receiverClient = new TcpClient();
            }
            public void StartReceive(string IP, int port)
            {
                try
                {
                    if(useUDP)
                    {
                        localIP = new IPEndPoint(IPAddress.Parse(IP), port + threadCount * 2 + 1);
                        CameraDebug.Log("Frame: " + (port + threadCount * 2 + 1));
                        try
                        {
                            udpClient = new UdpClient(localIP);
                        }
                        catch
                        {

                        }
                    }
                    else
                        receiverClient.Connect(IPAddress.Parse(IP), port);
                    connected = true;
                }
                catch (Exception e)
                {
                    CameraDebug.Log("Failed to start client: " + e);
                }
                if (connected)
                {
                    ReceiveFrameMsg();
                    if (!emptyFrame)
                    {
                        ReceiveFrame();
                        Feedback();
                    }
                }
            }
            public void ReceiveFrameMsg()
            {
                if (!useUDP)
                    serverStream = receiverClient.GetStream();
                frameMsg = new byte[frameMsgLength];
                var total = 0;
                if (useUDP)
                {
                    bool reached = false;
                    while (!reached)
                    {
                        try {
                            udpClient.Client.ReceiveTimeout = StreamingServer.timeoutUDP;
                            IPEndPoint tempIP = new IPEndPoint(localIP.Address, localIP.Port);
                            frameMsg = udpClient.Receive(ref tempIP);
                            reached = true;
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
                else
                do
                {
                    try
                    {
                        var read = serverStream.Read(frameMsg, total, frameMsgLength - total);
                        total += read;
                    }
                    catch
                    {
                        break;
                    }
                } while (total != frameMsgLength);
                rawMsg = Encoding.ASCII.GetString(frameMsg);
                string[] configs = rawMsg.Split(',');
                if (configs.Length == 5)
                {
                    frameWidth = int.Parse(configs[0]);
                    frameHeight = int.Parse(configs[1]);
                    frameLength = int.Parse(configs[2]);
                    compressFormat = int.Parse(configs[3]);
                    videoQuality = int.Parse(configs[4]) + 1;
                }
                else
                {
                    emptyFrame = true;
                }
                //CameraDebug.Log(rawMsg + ":" + frameID);
            }
            public void ReceiveFrame()
            {
                if (!useUDP)
                    serverStream = receiverClient.GetStream();
                rawBytes = new byte[frameLength]; 
                int total = 0;
                if (useUDP)
                {
                    bool reached = false;
                    while (!reached)
                    {
                        try { 
                            udpClient.Client.ReceiveTimeout = StreamingServer.timeoutUDP;
                            IPEndPoint tempIP = new IPEndPoint(localIP.Address, localIP.Port);
                            rawBytes= udpClient.Receive(ref tempIP);
                            reached = true;
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
                else
                    do
                    {
                        try
                        {
                            var read = serverStream.Read(rawBytes, total, frameLength - total);
                            total += read;
                        }
                        catch
                        {
                            emptyFrame = true;
                            break;
                        }
                    } while (total != frameLength);
            }
            public void Feedback()
            {
                if (!useUDP)
                    serverStream = receiverClient.GetStream();
                frameMsg = new byte[1];
                frameMsg = Encoding.ASCII.GetBytes(((int)streamingState).ToString());
                if (useUDP)
                {
                    bool reached = false;
                    while (!reached)
                    {
                        try
                        {
                            udpClient.Client.SendTimeout = StreamingServer.timeoutUDP;
                            IPEndPoint tempIP = new IPEndPoint(localIP.Address, localIP.Port - 1);
                            udpClient.Send(frameMsg, frameMsg.Length, tempIP);
                            reached = true;
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
                else
                    serverStream.Write(frameMsg, 0, 1);
                Close();
            }
            public void Close()
            {
                downloaded = true;
                if (!useUDP)
                    serverStream.Close();
                if (!useUDP)
                    receiverClient.Close();
                if (useUDP)
                    udpClient.Close();
            }
        }
        public enum DecompressMode
        {
            //
            // Summary:
            //     Point filtering - texture pixels become blocky up close.
            Point = 0,
            //
            // Summary:
            //     Bilinear filtering - texture samples are averaged.
            Bilinear = 1,
            //
            // Summary:
            //     Trilinear filtering - texture samples are averaged and also blended between mipmap
            //     levels.
            Trilinear = 2
        }
        public enum StreamingState
        {
            Contiune = 0,
            Stop = 1
        };
        public bool useUDP = false;
        public DecompressMode decompressMode = DecompressMode.Point;
        public static StreamingState streamingState;
        public RawImage screen;
        public string IP = "127.0.0.1";
        public int port = 8010;//same with server
        public int senderFPS = 30;
        public Text inputText;
        public Text FPS_text;
        public bool decompressFrame = false;
        public bool enableFPSDisplay = false;
        public bool stop = false;
        public int frameWidth = 640;
        public int frameHeight = 480;
        public int videoQuality = 100;//1-100
        public int compressFormat = 0;
        public int frameID = 0;
        public static int frameMsgLength = 100;
        //private Texture2D texture2DTemp = null;
        private List<Frame> cachedFrames = new List<Frame>();
        private Texture2D receivedTexture2D = null;
        private Texture2D receivedTexture2Dtemp = null;
        private RenderTexture currentRT = null;
        private RenderTexture renderTexture = null;
        private bool compressedFormat = false;
        private int cachedFrameCounter = 0;
        private float scale = 1.0f;
        private TextureFormat currentFormat = TextureFormat.RGB24;
        private bool restarting = false;
        // Start is called before the first frame update
        void Start()
        {
#if UNITY_ANDROID
            decompressFrame = false;
            CameraDebug.Log("Decompressed frame is unsupported for Android");
#endif
            if (useUDP)
                ThreadManager.maxThreads = 3;
            ThreadManager.InitThreadManager();
            stop = true;
            frameMsgLength = Encoding.ASCII.GetBytes("1234,1234,12345678910,99,99").Length;
        }

        public void InitClient(Text text = null)
        {
            frameID = 0;
            FPS_text.enabled = enableFPSDisplay;
            if (inputText.text == "")
                return;
            stop = !stop;
            if (text != null)
                text.text = stop ? "Start client" : "Stop client";
            OnApplicationQuit();
            if (enableFPSDisplay)
            {
                StopCoroutine("FPSCounter");
                StartCoroutine("FPSCounter");
            }
            IP = inputText.text;
            if (!stop)
            {
                streamingState = StreamingState.Contiune;
                StartCoroutine("ClientLoop");
            }
        }

        public void AutoFitScreen(GameObject screenObject, float weight = 1.0f)
        {
            Vector3 scale = Vector3.one * weight;
            screenObject.transform.localScale = new Vector3(scale.x * (float)frameWidth / (float)frameHeight, scale.y, scale.z);
        }

        public IEnumerator ClientLoop()
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            float runTime = 0;
            int threadCounter = 0;
            while (!stop)
            {
                stopwatch.Stop();
                runTime = (float)stopwatch.Elapsed.TotalSeconds;
                stopwatch.Reset();
                if (runTime < ((float)1 / senderFPS))
                    runTime = ((float)1 / senderFPS) - runTime;
                else
                    runTime = 0;
                yield return new WaitForSeconds(runTime);
                stopwatch.Start();
                yield return new WaitUntil(() => !restarting);
                if (cachedFrameCounter < ThreadManager.maxThreads)
                {
                    ThreadManager.RunOrginalAction(() =>
                    {
                        Frame frame = new Frame(frameID);
                        frame.useUDP = useUDP;
                        if (useUDP)
                            frame.threadCount = threadCounter;
                        cachedFrameCounter++;
                        threadCounter++;
                        if(threadCounter == ThreadManager.maxThreads)
                            threadCounter = 0;
                        cachedFrames.Add(frame);
                        frame.StartReceive(IP, port);
                        //CameraDebug.Log("Downloaded frame");
                    });
                }
                if (cachedFrames.Count > 0 && cachedFrames[0] != null && (cachedFrames[0].downloaded || cachedFrames[0].emptyFrame))
                {
                    frameID++;
                    if (cachedFrames[0].emptyFrame)
                    {
                        //CameraDebug.Log("Empty frame");
                        ClearCachedFrames();
                    }
                    else
                    {
                        //CameraDebug.Log("Connected with server");
                        DisplayFrame(cachedFrames[0]);
                    }
                }
                if (cachedFrameCounter > 0 && streamingState == StreamingState.Stop)
                    OnApplicationQuit();
            }
        }

        public IEnumerator FPSCounter()
        {
            int tempID = 0;
            while (!stop)
            {
                tempID = frameID;
                yield return new WaitForSeconds(1.0f);
                FPS_text.text = "Streaming FPS: " + (frameID - tempID) +
                    "\r\nThread counter: " + ThreadManager.threadCounter +
                     "\r\nCached frames: " + cachedFrameCounter;
            }
        }

        public void RestartClientLoop()
        {
            OnApplicationQuit();
            StartCoroutine("RestartClient");
        }

        public void DisplayFrame(Frame frame)
        {
            if (!frame.emptyFrame && frame.downloaded)
            {
                frameWidth = frame.frameWidth;
                frameHeight = frame.frameHeight;
                compressFormat = frame.compressFormat;
                videoQuality = frame.videoQuality;
                AutoFitScreen(screen.gameObject, 5.0f);
                scale = (float)100 / videoQuality;
                if (compressFormat != (int)TextureFormat.RGB24)
                {
                    compressedFormat = true;
                    currentFormat = (TextureFormat)compressFormat;
                }
                else
                {
                    compressedFormat = false;
                    currentFormat = TextureFormat.RGB24;
                }
                if (receivedTexture2Dtemp == null || receivedTexture2Dtemp.format != currentFormat
                    || receivedTexture2Dtemp.width != frameWidth || receivedTexture2Dtemp.height != frameHeight)
                {
                    Destroy(receivedTexture2Dtemp);
                    receivedTexture2Dtemp = new Texture2D(frameWidth, frameHeight, currentFormat, false);
                }
                //receivedTexture2Dtemp.LoadImage(frame.rawBytes);
                receivedTexture2Dtemp.LoadRawTextureData(frame.rawBytes);
                receivedTexture2Dtemp.Apply();
                if (compressedFormat)
                    DeCompressFormat(receivedTexture2Dtemp);
                if (decompressFrame && scale > 1.0f)
                    DeCompressTexture(receivedTexture2Dtemp, scale);
                if (compressedFormat || (decompressFrame && scale > 1.0f))
                    screen.texture = receivedTexture2D;
                else
                    screen.texture = receivedTexture2Dtemp;
            }
            //frame.used = true;
            cachedFrames[0] = null;
            cachedFrames.Remove(cachedFrames[0]);
            cachedFrameCounter--;
        }

        public Texture2D DeCompressFormat(Texture2D texture)
        {
            renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, renderTexture);
            currentRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            if (receivedTexture2D == null || (receivedTexture2D.width != renderTexture.width || receivedTexture2D.height != renderTexture.height))
            {
                Destroy(receivedTexture2D);
                receivedTexture2D = new Texture2D(renderTexture.width, renderTexture.height);
            }
            receivedTexture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            receivedTexture2D.Apply();
            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(renderTexture);
            return receivedTexture2D;
        }
        public Texture2D DeCompressTexture(Texture2D texture, float scale = 1.0f)
        {
            //CameraDebug.Log(scale);
            if (receivedTexture2D == null || (receivedTexture2D.width != (int)(frameWidth * scale) || receivedTexture2D.height != (int)(frameHeight * scale)))
            {
                Destroy(receivedTexture2D);
                receivedTexture2D = new Texture2D((int)(frameWidth * scale), (int)(frameHeight * scale), TextureFormat.RGB24, false);
            }
            texture.filterMode = (FilterMode)decompressMode;
            renderTexture = RenderTexture.GetTemporary((int)(frameWidth * scale), (int)(frameHeight * scale), 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            renderTexture.filterMode = (FilterMode)decompressMode;
            currentRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Graphics.Blit(texture, renderTexture);
            receivedTexture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            receivedTexture2D.Apply();
            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(renderTexture);
            //CameraDebug.Log("Rescale: " + receivedTexture2D.width + "," + receivedTexture2D.height);
            return receivedTexture2D;
        }
        // Update is called once per frame
        void Update()
        {

        }

        public void ClearCachedFrames()
        {
            restarting = true;
            try
            {
                for (int i = 0; i < cachedFrames.Count; i++)
                {
                    if (cachedFrames[i] != null && cachedFrames[i].receiverClient != null)
                        cachedFrames[i].Close();
                    cachedFrames[i] = null;
                }
            }
            catch
            {
            }
            cachedFrames.Clear();
            cachedFrameCounter = 0;
            restarting = false;
        }
        public void OnApplicationQuit()
        {
            StopAllCoroutines();
            ClearCachedFrames();
        }
    }
}
