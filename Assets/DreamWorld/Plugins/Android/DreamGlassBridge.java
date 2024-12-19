package com.unity3d.player;

import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.BroadcastReceiver;

import android.util.Log;

import com.dreamworld.trlibrary.CallbackInterface;
import com.dreamworld.trlibrary.TRUsbHidUtil;

import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbManager;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

public class DreamGlassBridge implements CallbackInterface {
    private static final String ACTION_USB_PERMISSION = "com.dream.dgdemo.USB_PERMISSION";

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

    public float rx, ry, rz;
    public float ax, ay, az;

    private IntentFilter usbAttachIntentFilter = new IntentFilter();

    public boolean Init(Context ctx) {
        c = ctx;
        instance = this;

        usbAttachIntentFilter.addAction("android.hardware.usb.action.USB_STATE");
        usbAttachIntentFilter.addAction(UsbManager.ACTION_USB_ACCESSORY_ATTACHED);
        usbAttachIntentFilter.addAction(UsbManager.ACTION_USB_ACCESSORY_DETACHED);
        usbAttachIntentFilter.addAction(UsbManager.ACTION_USB_DEVICE_ATTACHED);
        usbAttachIntentFilter.addAction(UsbManager.ACTION_USB_DEVICE_DETACHED);
        usbAttachIntentFilter.addAction(ACTION_USB_PERMISSION);

        ctx.registerReceiver(usbReceiver, usbAttachIntentFilter, Context.RECEIVER_NOT_EXPORTED);

        int ret = checkDevice();
        if (ret < 0) {
            Log.i("Unity", "No device found");
            return false;
        }

        utils.initLibUsb(PRODUCT_VID, PRODUCT_PID, ret);
        utils.registerHmdCallbackData(this);
        
        Log.i("Unity", "Init success");

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
        var s = String.format("%f %f %f %f %f %f", rx, ry, rz, ax, ay, az);
        rx = ry = rz = 0;
        ax = ay = az = 0;
        return s;
    }

    public String GetSN() {
        return utils.getSN();
    }

    @Override
    public void onCmdEvent(String s, int i) {
        Log.i("Unity", "Cmd " + s + " " + i);
    }

    @Override
    public void onSensorChanged(String s, int i) {
        if (i == 0) {
            JSONObject json = null;
            try {
                json = new JSONObject(s);
                JSONArray arr = json.getJSONArray("imu_data");
                JSONObject data = arr.getJSONObject(0);
                rx += data.getDouble("gyro_x");
                ry += data.getDouble("gyro_y");
                rz += data.getDouble("gyro_z");

                ax += data.getDouble("acc_x");
                ay += data.getDouble("acc_y");
                az += data.getDouble("acc_z");
        } catch (JSONException e) {

        }
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

                    Log.i("Unity", "Request permission");
                    retval = -3;

                    break;
                }

                Log.i("Unity", "DEVICE: " + device.getVendorId() + " " + device.getProductId() + " " +
                 device.getManufacturerName() + " " + device.getProductName()  + " " + device.getDeviceName() + " " + device.getSerialNumber());

                //open device
                UsbDeviceConnection connection = mUsbManager.openDevice(device);
                if (connection == null) {
                    var mPermissionIntent = PendingIntent.getBroadcast(c, 0, new Intent(ACTION_USB_PERMISSION), PendingIntent.FLAG_IMMUTABLE);
                    mUsbManager.requestPermission(device, mPermissionIntent);
                    Log.i("Unity", "Request permission null connection");

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

    private final BroadcastReceiver usbReceiver = new BroadcastReceiver() {
        public void onReceive(Context context, Intent intent) {
            String action = intent.getAction();
            if (ACTION_USB_PERMISSION.equals(action)) {
                synchronized (this) {
                    UsbDevice device = (UsbDevice)intent.getParcelableExtra(UsbManager.EXTRA_DEVICE);

                    if (intent.getBooleanExtra(UsbManager.EXTRA_PERMISSION_GRANTED, false)) {
                        if(device != null){
                            // call method to set up device communication
                        }
                    }
                    else {
                        Log.d("Unity", "permission denied for device " + device);
                    }
                }
            }
        }
    };
}
