package com.unity3d.player;

import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.dreamworld.trlibrary.CallbackInterface;
import com.dreamworld.trlibrary.TRUsbHidUtil;

import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbManager;

public class DreamGlassBridge implements CallbackInterface {
    private static final String ACTION_USB_PERMISSION = "com.unity3d.player.USB_PERMISSION";

    private static final int PRODUCT_VID = 0x483;
    private static final int PRODUCT_PID= 0x5740;

    private static final int XREAL_PRODUCT_VID = 0x3318;
    private static final int XREAL_PRODUCT_PID = 0x0432;

    static Context c;

    static DreamGlassBridge instance = null;


    public static DreamGlassBridge GetInstance() {
        return instance;
    }

    public static TRUsbHidUtil utils = new TRUsbHidUtil();

    public String IMUData = "";

    public boolean Init(Context ctx) {
        c = ctx;
        instance = this;

        int ret = checkDevice();
        if (ret < 0) {
            Log.i("Unity", "No device found");
            return false;
        }

        utils.initLibUsb(PRODUCT_VID, PRODUCT_VID, ret);
        utils.registerHmdCallbackData(this);

        return true;
    }

    public void Close() {
        utils.unRegisterHmdCallbackData(this);
    }

    public void Set3DMode(int mode) {
        utils.set2D3DState(mode);
    }

    public int Get3DMode() {
        return utils.get2D3DState();
    }

    public String GetIMU() {
        return IMUData;
    }

    public String GetSN() {
        return utils.getSN();
    }

    @Override
    public void onCmdEvent(@Nullable String s, int i) {
        Log.i("Unity", "Cmd " + s + " " + i);
    }

    @Override
    public void onSensorChanged(@NonNull String s, int i) {
        Log.i("Unity", "Sensor " + s + " " + i);
    }

    UsbManager mUsbManager = null;
    private int checkDevice() {
        mUsbManager = (UsbManager)c.getSystemService(Context.USB_SERVICE);

        var retval = -1;
        for (UsbDevice device : mUsbManager.getDeviceList().values()) {
            if (device.getVendorId() == PRODUCT_VID && device.getProductId() == PRODUCT_PID) {
                //check permission
                if (!mUsbManager.hasPermission(device)) {
                    //ask for permission
                    var mPermissionIntent = PendingIntent.getBroadcast(c, 0, new Intent(ACTION_USB_PERMISSION), PendingIntent.FLAG_IMMUTABLE);
                    mUsbManager.requestPermission(device, mPermissionIntent);
                    retval = -3;

                    break;
                }

                Log.i("Unity", "DEVICE: " + device.getManufacturerName() + " " + device.getProductName()  + " " + device.getDeviceName() + " " + device.getSerialNumber());

                //open device
                UsbDeviceConnection connection = mUsbManager.openDevice(device);
                if (connection != null) {
                    var mPermissionIntent = PendingIntent.getBroadcast(c, 0, new Intent(ACTION_USB_PERMISSION), PendingIntent.FLAG_IMMUTABLE);
                    mUsbManager.requestPermission(device, mPermissionIntent);

                    retval = -2;
                } else {
                    try {
                        retval = connection.getFileDescriptor();
                    } catch (Exception e) {
                        retval = -1;
                    }
                }

                break;
            }

        }

        return retval;
    }
}
