using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ChaosIkaros
{
    public class StreamingServer : MonoBehaviour
    {
        public enum CompressMode
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
        //https://www.sciencepubco.com/index.php/ijet/article/download/23739/11888#:~:text=The%20results%20have%20shown%20that%20the%20behavior%20of%20TCP%20is,throughput%20and%20UDP%20has%20302.67.&text=Thus%2C%20TCP%20performance%20is%20faster,send%20and%20receive%20the%20data.
        //udp has a smaller throughput and higher End-to-end delay
        public USBCamera usbCamera;
        public CompressMode compressMode = CompressMode.Point;
        public StreamingState streamingState;
        public RawImage screen;
        public string IP = "127.0.0.1";
        public int port = 8010;//same with client
        public int senderFPS = 30;
        public TcpListener serverListener;
        public List<UdpClient> udpClient = new List<UdpClient>();
        public Dropdown IPSelector;
        public Text FPS_text;
        public bool enableFPSDisplay = false;
        public bool stop = false;
        public bool compressdFormat = false;
        public List<string> IPList = new List<string>();
        public int videoQuality = 100;//1-100
        public int compressFormat = 0;
        public int frameID = 0;
        public int broadcastingRate = 10;//1-1000
        private Texture2D sentTexture2D = null;
        private RenderTexture currentRT = null;
        private RenderTexture renderTexture = null;
        private byte[] frameMsg = null;
        private byte[] rawBytes = null;
        private Thread connectThread;
        private List<Thread> connectThreadUDP = new List<Thread>();
        private TcpClient receiverClient = null;
        //private NetworkStream receiverStream = null;
        public static int frameMsgLength = 100;
        public static int timeoutUDP = 10;
        // Start is called before the first frame update
        void Start()
        {
#if UNITY_ANDROID || UNITY_IOS
            compressdFormat = false;
            CameraDebug.Log("Compressed format is only supported for Windows, Linux, macOS, PS4, XBox One due to performance issues");   
            //https://docs.unity3d.com/Manual/class-TextureImporterOverride.html
#endif
            ThreadManager.InitThreadManager();
            IPSelector.onValueChanged.AddListener(OnIPChanged);
            stop = true;
            frameMsgLength = Encoding.ASCII.GetBytes("1234,1234,12345678910,99,99").Length;
            RefreshIP();
            OnIPChanged(0);
        }

        public void SetVideoQuality(float i)
        {
            videoQuality = (int)i;
        }

        public void InitServer(Text text = null)
        {
            FPS_text.enabled = enableFPSDisplay;
            stop = !stop;
            if (text != null)
                text.text = stop ? "Start server" : "Stop server";
            OnApplicationQuit();
            if (enableFPSDisplay)
            {
                StopCoroutine("FPSCounter");
                StartCoroutine("FPSCounter");
            }
            RefreshIP();
            if (!stop)
            {
                streamingState = StreamingState.Contiune;
                if (useUDP)
                {
                    for (int i = 0; i < ThreadManager.maxThreads; i++)
                    {
                        connectThreadUDP.Add(new Thread(() => ConnectLoopUDP(udpClient.Count - 1)));
                        CameraDebug.Log("udpClient.Add :" + (port + 2 * udpClient.Count));
                        udpClient.Add(new UdpClient(new IPEndPoint(IPAddress.Parse(IP), port + 2 * udpClient.Count)));
                        connectThreadUDP[i].Start();
                    }
                }
                else if (serverListener == null)
                {
                    connectThread = new Thread(new ThreadStart(ConnectLoop));
                    serverListener = new TcpListener(IPAddress.Parse(IP), port);
                    serverListener.Start();
                    connectThread.Start();
                }
                StartCoroutine("ServerLoop");
            }
        }

        public async void ConnectLoopUDP(int threadCounter)
        {
            CameraDebug.Log("ConnectLoopUDP() :" + (port + threadCounter * 2));
            try
            {
                while (!stop)
                {
                    await Task.Run(async () =>
                    {
                        udpClient[threadCounter].Client.SendTimeout = StreamingServer.timeoutUDP;
                        udpClient[threadCounter].Client.ReceiveTimeout = StreamingServer.timeoutUDP;
                        try
                        {
                            udpClient[threadCounter].Send(frameMsg, frameMsg.Length, new IPEndPoint(IPAddress.Parse(IP), port + threadCounter * 2 + 1));
                            udpClient[threadCounter].Send(rawBytes, rawBytes.Length, new IPEndPoint(IPAddress.Parse(IP), port + threadCounter * 2 + 1));
                            IPEndPoint tempIP = new IPEndPoint(IPAddress.Parse(IP), port + threadCounter * 2);
                            frameMsg = udpClient[threadCounter].Receive(ref tempIP);
                            streamingState = (StreamingState)int.Parse(Encoding.ASCII.GetString(frameMsg));
                        }
                        catch
                        {
                        }
                    });
                }
            }
            catch
            {
                udpClient[threadCounter].Close();
            }
        }

        public async void ConnectLoop()
        {
            try
            {
                while (!stop)
                {
                    await Accept(await serverListener.AcceptTcpClientAsync());
                    Thread.Sleep(broadcastingRate);
                    //CameraDebug.Log("Connected with client");
                    //receiverClient = serverListener.AcceptTcpClient();
                    //receiverStream = receiverClient.GetStream();
                    //SendFrameMsg();
                    //SendFrameData();
                    //Feedback();
                }
            }
            catch
            {
                serverListener.Stop();
            }
        }

        public void RefreshIP()
        {
            List<string> options = IPManager.GetIPList().Split(',').ToList();
            IPList.Clear();
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] != "")
                    IPList.Add(options[i]);
            }
            IPList.Reverse();
            IPSelector.ClearOptions();
            IPSelector.AddOptions(IPList);
            //OnIPChanged(0);
        }
        private async Task Accept(TcpClient client)
        {
            await Task.Yield();
            try
            {
                using (client)
                using (NetworkStream networkStream = client.GetStream())
                {
                    //CameraDebug.Log("Connected with client");
                    SendFrameMsg(networkStream);
                    SendFrameData(networkStream);
                    Feedback(networkStream);
                }
            }
            catch
            {
            }
        }

        public void OnIPChanged(int value)
        {
            IP = IPList[value];
            CameraDebug.Log("Server IP: " + IP);
        }

        public IEnumerator ServerLoop()
        {

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            float runTime = 0;
            frameID = 0;
            while (!stop)
            {
                frameID++;
                stopwatch.Stop();
                runTime = (float)stopwatch.Elapsed.TotalSeconds;
                stopwatch.Reset();
                if (runTime < ((float)1 / senderFPS))
                    runTime = ((float)1 / senderFPS) - runTime;
                else
                    runTime = 0;
                yield return new WaitForSeconds(runTime);
                stopwatch.Start();

                RefreshFrameData();
           
                if (streamingState == StreamingState.Stop && frameID != 1)
                    OnApplicationQuit();
            }
        }

        public IEnumerator FPSCounter()
        {
            int tempID = 0;
            string frameMsgTemp = "0";
            string frameMsgString = "0";
            while (!stop)
            {
                tempID = frameID;
                yield return new WaitForSeconds(1.0f);
                frameMsgTemp = Encoding.ASCII.GetString(frameMsg);
                if (frameMsgTemp != "0")
                    frameMsgString = frameMsgTemp;
                FPS_text.text = "Source FPS: " + (frameID - tempID) +
                    "\r\nFrame msg: " + frameMsgString + "——" + frameID;
            }
        }
        public void RestartServerLoop()
        {
            OnApplicationQuit();
            stop = true;
            InitServer();
        }
        public void Feedback(NetworkStream networkStream)
        {
            frameMsg = new byte[1];
            try
            {
                networkStream.Read(frameMsg, 0, 1);
                streamingState = (StreamingState)int.Parse(Encoding.ASCII.GetString(frameMsg));
            }
            catch
            {
            }
        }

        public void SendFrameMsg(NetworkStream networkStream)
        {
            try
            {
                networkStream.Write(frameMsg, 0, frameMsg.Length);
            }
            catch
            {
            }
        }

        public void SendFrameData(NetworkStream networkStream)
        {
            try
            {
                networkStream.Write(rawBytes, 0, rawBytes.Length);
            }
            catch
            {
            }
        }

        public void RefreshFrameData()
        {
            videoQuality = Mathf.Clamp(videoQuality, 1, 100);
            //rawBytes = TextureToTexture2D(screen.texture).EncodeToJPG(videoQuality);
            //low upload bandwidth with poor performance
            TextureToTexture2D(screen.texture, (float)videoQuality / 100, compressdFormat);
            //high upload bandwidth with good performance
            compressFormat = (int)sentTexture2D.format;
            rawBytes = sentTexture2D.GetRawTextureData();
            frameMsg = new byte[frameMsgLength];
            frameMsg = Encoding.ASCII.GetBytes(
                sentTexture2D.width.ToString().PadLeft(4, '0') + "," +
                sentTexture2D.height.ToString().PadLeft(4, '0') + "," +
                rawBytes.Length.ToString().PadLeft(11, '0') + "," +
                compressFormat.ToString().PadLeft(2, '0') + "," +
                (videoQuality - 1).ToString().PadLeft(2, '0')
                );
        }
        public Texture2D TextureToTexture2D(Texture texture, float scale = 1.0f, bool compress = false)
        {
            if (sentTexture2D == null || (sentTexture2D.width != (int)(usbCamera.width * scale) || sentTexture2D.height != (int)(usbCamera.width * scale)))
            {
                Destroy(sentTexture2D);
                sentTexture2D = new Texture2D((int)(usbCamera.width * scale), (int)(usbCamera.height * scale), TextureFormat.RGB24, false);
            }
            texture.filterMode = (FilterMode)compressMode;
            renderTexture = RenderTexture.GetTemporary(sentTexture2D.width, sentTexture2D.height);
            renderTexture.filterMode = (FilterMode)compressMode;
            currentRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Graphics.Blit(texture, renderTexture);
            sentTexture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            sentTexture2D.Apply();
            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(renderTexture);
            if (compress)
                sentTexture2D.Compress(false);
            return sentTexture2D;
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnApplicationQuit()
        {
            StopAllCoroutines();
            if (receiverClient != null)
                receiverClient.Close();
            if (connectThread != null)
            {
                connectThread.Interrupt();
                connectThread.Abort();
            }
            for (int i = 0; i < connectThreadUDP.Count; i++)
            {
                connectThreadUDP[i].Interrupt();
                connectThreadUDP[i].Abort();
            }
            if (serverListener != null)
                serverListener.Stop();
            serverListener = null;
            for (int i = 0; i < udpClient.Count; i++)
                udpClient[i].Close();
            udpClient.Clear();
        }
    }
}
