package com.unity3d.player;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.util.Log;
import android.widget.Toast;

import com.dreamworld.dreamglasssdk.DreamGlassSDK;
import com.dreamworld.dreamglasssdk.sdk.DGData;
import com.dreamworld.dreamglasssdk.sdk.IResult;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileOutputStream;

public class DreamGlassBridge {

    static Context c;
    static String sn = "";

    static DreamGlassBridge instance = null;

    static int installAppStatus = 0;

    static boolean convertingPNG = false;
    static boolean inForeground = false;

    public void OnClose() {
        DreamGlassSDK.getInstance().destory();
    }

    public static DreamGlassBridge GetInstance() {
        return instance;
    }

    public void InitDream(Context ctx) {
        c = ctx;
        DreamGlassSDK.getInstance().init(c);

        instance = this;

        DreamGlassSDK.getInstance().getSN(c, new IResult() {
            @Override
            public void onResult(boolean b, DGData dgData) {
                sn = dgData.getData();
            }
        });
    }

    public void Set3DMode(int mode) {
        DreamGlassSDK.getInstance().set3DScreenMode(c, mode, new IResult() {
            @Override
            public void onResult(boolean b, DGData dgData) {
            }
        });
    }

    public void SetHomeEnabled(boolean enabled) {
        DreamGlassSDK.getInstance().setHomeKeyDisable(c, !enabled, new IResult() {
            @Override
            public void onResult(boolean b, DGData dgData) {
            }
        });
    }

    public void SetDistortionEnabled(boolean enabled) {
        DreamGlassSDK.getInstance().setDisableDistortion(c, !enabled, new IResult() {
            @Override
            public void onResult(boolean b, DGData dgData) {
            }
        });
    }

    public void Shutdown(boolean reboot) {
        DreamGlassSDK.getInstance().shutdown(c, reboot, new IResult() {
            @Override
            public void onResult(boolean b, DGData dgData) {
            }
        });
    }

    public void InstallApp(String apkPath) {
        installAppStatus = 0;
        DreamGlassSDK.getInstance().installApp(c, apkPath, new IResult() {
            @Override
            public void onResult(boolean b, DGData dgData) {
                if(b){
                    installAppStatus = 1;
                }else{
                    installAppStatus = 2;
                }
            }
        });
    }

    public int GetInstallAppStatus() {
        return installAppStatus;
    }

    public void SetInForeground(boolean foreground) {
        inForeground = foreground;
    }

    public void CaptureScreen() {
        if (!convertingPNG && !inForeground) {
            convertingPNG = true;
            DreamGlassSDK.getInstance().captureScreen(c, new IResult() {
                @Override
                public void onResult(boolean b, DGData dgData) {
                    if (b) {
                        String screenshotPath = dgData.getData();
                        ConvertThread th = new ConvertThread(screenshotPath);
                        th.start();
                    } else {
                        convertingPNG = false;
                        Log.v("Unity", "Cannot take screenshot");
                    }
                }
            });
        }
    }

    public String GetSN() {
        return sn;
    }

    public int Add(int a, int b) {
        return a + b;
    }

    public class ConvertThread extends Thread {
        private String path;
        public ConvertThread(String p){
            path = p;
        }

        @Override
        public void run(){
            try {
                Bitmap screen = BitmapFactory.decodeFile(path);
                Bitmap resized = Bitmap.createScaledBitmap(screen, 320, 180, true);

                ByteArrayOutputStream bytes = new ByteArrayOutputStream();
                resized.compress(Bitmap.CompressFormat.JPEG, 75, bytes);

                File f = new File("/storage/emulated/0/screen.jpg");
                f.createNewFile();
                FileOutputStream fo = new FileOutputStream(f);
                fo.write(bytes.toByteArray());
                fo.close();

                screen.recycle();
                resized.recycle();

                File pngfile = new File(path);
                pngfile.delete();
            } catch (Exception e) {
                e.printStackTrace();
            }

            convertingPNG = false;
        }
    }
}
