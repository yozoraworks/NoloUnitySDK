using UnityEngine;

namespace ChaosIkaros
{
    public class UVCManager : MonoBehaviour
    {
        public static bool exist = false;
        public static UVCManager uvcManagerHolder;
        public static AndroidJavaObject androidJavaObject;
        public static UVCManager uvcManager
        {
            get
            {
                InitUVCManager();
                return uvcManagerHolder;
            }
        }

        public static void InitUVCManager()
        {
            if (!exist)
            {
                exist = true;
                GameObject managerHolder = new GameObject("UVCManager");
                DontDestroyOnLoad(managerHolder);
                uvcManagerHolder = managerHolder.AddComponent<UVCManager>();
                androidJavaObject = new AndroidJavaObject("com.chaosikaros.unityplugin.Plugin");
            }
        }
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {

        }
        private void OnApplicationQuit()
        {
            androidJavaObject.Call<bool>("OnDestroyAPP");
        }
    }
}
