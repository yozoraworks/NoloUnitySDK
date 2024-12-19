package com.unity3d.player;

import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.BroadcastReceiver;

import android.util.Log;

import com.dreamworld.trlibrary.CallbackInterface;
import com.dreamworld.trlibrary.MagSensorCalibrationDataCb;
import com.dreamworld.trlibrary.TRUsbHidUtil;

import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbManager;

import static java.lang.Math.asin;
import static java.lang.Math.atan2;
import static java.lang.Math.sqrt;

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

    double ax, ay, az;
    double rx, ry, rz;
    double mx, my, mz;

    public static DreamGlassBridge GetInstance() {
        return instance;
    }

    public static TRUsbHidUtil utils = new TRUsbHidUtil();

    static float q0 = 1.0f, q1 = 0.0f, q2 = 0.0f, q3 = 0.0f;	// quaternion of sensor frame relative to auxiliary frame
    static double Pitch = 0.0f, Roll = 0.0f, Yaw = 0.0f;

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
        var s = String.format("%f %f %f", Pitch, Roll, Yaw);
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
            try {
                JSONObject json = new JSONObject(s);
                JSONArray arr = json.getJSONArray("imu_data");
                JSONObject data = arr.getJSONObject(0);
                boolean hasAcc = data.getDouble("accSensor") > 0;
                boolean hasGyro = data.getDouble("gyrSensor") > 0;
                boolean hasMag = data.getDouble("magSensor") > 0;

                if (hasAcc) {
                    ax = data.getDouble("acc_x");
                    ay = data.getDouble("acc_y");
                    az = data.getDouble("acc_z");
                }

                if (hasGyro) {
                    rx = data.getDouble("gyro_x");
                    ry = data.getDouble("gyro_y");
                    rz = data.getDouble("gyro_z");
                }

                if (hasMag) {
                    mx = data.getDouble("mag_x");
                    my = data.getDouble("mag_y");
                    mz = data.getDouble("mag_z");

                    MadgwickAHRSupdate_9((float)rx * 0.017453292f, (float)ry * 0.017453292f, (float)rz * 0.017453292f, (float)ax, (float)ay, (float)az, (float)mx, (float)my, (float)mz);
                } else {
                    MadgwickAHRSupdate_6((float)rx, (float)ry, (float)rz, (float)ax, (float)ay, (float)az);
                }
            } catch (JSONException e) {
                e.printStackTrace();
            }
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


    float sampleFreq = 50;
    float GYRO_K = 1f/16.4f/57.3f;

    /*
    void MPU6050_Madgwick_Init(float loop_ms)
    {
        sampleFreq = 1000. / loop_ms;	//sample frequency in Hz
        switch((MPU_Read_Byte(MPU_GYRO_CFG_REG) >> 3) & 3)
        {
            case 0:
                GYRO_K = 1./131/57.3;
                break;
            case 1:
                GYRO_K = 1./65.5/57.3;
                break;
            case 2:
                GYRO_K = 1./32.8/57.3;
                break;
            case 3:
                GYRO_K = 1./16.4/57.3;
                break;
        }
    }
    */

    public static float invSqrt(float x) {
        float xhalf = 0.5f * x;
        int i = Float.floatToIntBits(x);
        i = 0x5f3759df - (i >> 1);
        x = Float.intBitsToFloat(i);
        x *= (1.5f - xhalf * x * x);
        return x;
    }

    static float beta = 1.0f;

    void MadgwickAHRSupdate_6(float gx, float gy, float gz, float ax, float ay, float az)
    {
        float recipNorm;
        float s0, s1, s2, s3;
        float qDot1, qDot2, qDot3, qDot4;
        float _2q0, _2q1, _2q2, _2q3, _4q0, _4q1, _4q2 ,_8q1, _8q2, q0q0, q1q1, q2q2, q3q3;

        //将陀螺仪AD值转换为 弧度/s
        gx = gx * GYRO_K;
        gy = gy * GYRO_K;
        gz = gz * GYRO_K;

        // Rate of change of quaternion from gyroscope
        qDot1 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
        qDot2 = 0.5f * (q0 * gx + q2 * gz - q3 * gy);
        qDot3 = 0.5f * (q0 * gy - q1 * gz + q3 * gx);
        qDot4 = 0.5f * (q0 * gz + q1 * gy - q2 * gx);

        // Compute feedback only if accelerometer measurement valid (avoids NaN in accelerometer normalisation)
        if(!((ax == 0.0f) && (ay == 0.0f) && (az == 0.0f))) {

            // Normalise accelerometer measurement
            recipNorm = invSqrt(ax * ax + ay * ay + az * az);
            ax *= recipNorm;
            ay *= recipNorm;
            az *= recipNorm;

            // Auxiliary variables to avoid repeated arithmetic
            _2q0 = 2.0f * q0;
            _2q1 = 2.0f * q1;
            _2q2 = 2.0f * q2;
            _2q3 = 2.0f * q3;
            _4q0 = 4.0f * q0;
            _4q1 = 4.0f * q1;
            _4q2 = 4.0f * q2;
            _8q1 = 8.0f * q1;
            _8q2 = 8.0f * q2;
            q0q0 = q0 * q0;
            q1q1 = q1 * q1;
            q2q2 = q2 * q2;
            q3q3 = q3 * q3;

            // Gradient decent algorithm corrective step
            s0 = _4q0 * q2q2 + _2q2 * ax + _4q0 * q1q1 - _2q1 * ay;
            s1 = _4q1 * q3q3 - _2q3 * ax + 4.0f * q0q0 * q1 - _2q0 * ay - _4q1 + _8q1 * q1q1 + _8q1 * q2q2 + _4q1 * az;
            s2 = 4.0f * q0q0 * q2 + _2q0 * ax + _4q2 * q3q3 - _2q3 * ay - _4q2 + _8q2 * q1q1 + _8q2 * q2q2 + _4q2 * az;
            s3 = 4.0f * q1q1 * q3 - _2q1 * ax + 4.0f * q2q2 * q3 - _2q2 * ay;
            recipNorm = invSqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3); // normalise step magnitude
            s0 *= recipNorm;
            s1 *= recipNorm;
            s2 *= recipNorm;
            s3 *= recipNorm;

            // Apply feedback step
            qDot1 -= beta * s0;
            qDot2 -= beta * s1;
            qDot3 -= beta * s2;
            qDot4 -= beta * s3;
        }

        // Integrate rate of change of quaternion to yield quaternion
        q0 += qDot1 * (1.0f / sampleFreq);
        q1 += qDot2 * (1.0f / sampleFreq);
        q2 += qDot3 * (1.0f / sampleFreq);
        q3 += qDot4 * (1.0f / sampleFreq);

        // Normalise quaternion
        recipNorm = invSqrt(q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3);
        q0 *= recipNorm;
        q1 *= recipNorm;
        q2 *= recipNorm;
        q3 *= recipNorm;

        Pitch = asin(-2.0f * (q1*q3 - q0*q2))* 57.3f;
        Roll = atan2(q0*q1 + q2*q3, 0.5f - q1*q1 - q2*q2) * 57.3f;
        Yaw = atan2(q1*q2 + q0*q3, 0.5f - q2*q2 - q3*q3)* 57.3f;
    }

    void MadgwickAHRSupdate_9(float gx, float gy, float gz, float ax, float ay, float az, float mx, float my, float mz)
    {
        float recipNorm;
        float s0, s1, s2, s3;
        float qDot1, qDot2, qDot3, qDot4;
        float hx, hy;
        float _2q0mx, _2q0my, _2q0mz, _2q1mx, _2bx, _2bz, _4bx, _4bz, _2q0, _2q1, _2q2, _2q3, _2q0q2, _2q2q3, q0q0, q0q1, q0q2, q0q3, q1q1, q1q2, q1q3, q2q2, q2q3, q3q3;

        // Use IMU algorithm if magnetometer measurement invalid (avoids NaN in magnetometer normalisation)
        if((mx == 0.0f) && (my == 0.0f) && (mz == 0.0f))
        {
            MadgwickAHRSupdate_6(gx, gy, gz, ax, ay, az);
            return;
        }

        //将陀螺仪AD值转换为 弧度/s
        gx = gx * GYRO_K;
        gy = gy * GYRO_K;
        gz = gz * GYRO_K;

        // Rate of change of quaternion from gyroscope
        qDot1 = 0.5f * (-q1 * gx - q2 * gy - q3 * gz);
        qDot2 = 0.5f * (q0 * gx + q2 * gz - q3 * gy);
        qDot3 = 0.5f * (q0 * gy - q1 * gz + q3 * gx);
        qDot4 = 0.5f * (q0 * gz + q1 * gy - q2 * gx);

        // Compute feedback only if accelerometer measurement valid (avoids NaN in accelerometer normalisation)
        if(!((ax == 0.0f) && (ay == 0.0f) && (az == 0.0f)))
        {

            // Normalise accelerometer measurement
            recipNorm = invSqrt(ax * ax + ay * ay + az * az);
            ax *= recipNorm;
            ay *= recipNorm;
            az *= recipNorm;

            // Normalise magnetometer measurement
            recipNorm = invSqrt(mx * mx + my * my + mz * mz);
            mx *= recipNorm;
            my *= recipNorm;
            mz *= recipNorm;

            // Auxiliary variables to avoid repeated arithmetic
            _2q0mx = 2.0f * q0 * mx;
            _2q0my = 2.0f * q0 * my;
            _2q0mz = 2.0f * q0 * mz;
            _2q1mx = 2.0f * q1 * mx;
            _2q0 = 2.0f * q0;
            _2q1 = 2.0f * q1;
            _2q2 = 2.0f * q2;
            _2q3 = 2.0f * q3;
            _2q0q2 = 2.0f * q0 * q2;
            _2q2q3 = 2.0f * q2 * q3;
            q0q0 = q0 * q0;
            q0q1 = q0 * q1;
            q0q2 = q0 * q2;
            q0q3 = q0 * q3;
            q1q1 = q1 * q1;
            q1q2 = q1 * q2;
            q1q3 = q1 * q3;
            q2q2 = q2 * q2;
            q2q3 = q2 * q3;
            q3q3 = q3 * q3;

            // Reference direction of Earth's magnetic field
            hx = mx * q0q0 - _2q0my * q3 + _2q0mz * q2 + mx * q1q1 + _2q1 * my * q2 + _2q1 * mz * q3 - mx * q2q2 - mx * q3q3;
            hy = _2q0mx * q3 + my * q0q0 - _2q0mz * q1 + _2q1mx * q2 - my * q1q1 + my * q2q2 + _2q2 * mz * q3 - my * q3q3;
            _2bx = (float) sqrt(hx * hx + hy * hy);
            _2bz = -_2q0mx * q2 + _2q0my * q1 + mz * q0q0 + _2q1mx * q3 - mz * q1q1 + _2q2 * my * q3 - mz * q2q2 + mz * q3q3;
            _4bx = 2.0f * _2bx;
            _4bz = 2.0f * _2bz;

            // Gradient decent algorithm corrective step
            s0 = -_2q2 * (2.0f * q1q3 - _2q0q2 - ax) + _2q1 * (2.0f * q0q1 + _2q2q3 - ay) - _2bz * q2 * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (-_2bx * q3 + _2bz * q1) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + _2bx * q2 * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
            s1 = _2q3 * (2.0f * q1q3 - _2q0q2 - ax) + _2q0 * (2.0f * q0q1 + _2q2q3 - ay) - 4.0f * q1 * (1 - 2.0f * q1q1 - 2.0f * q2q2 - az) + _2bz * q3 * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (_2bx * q2 + _2bz * q0) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + (_2bx * q3 - _4bz * q1) * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
            s2 = -_2q0 * (2.0f * q1q3 - _2q0q2 - ax) + _2q3 * (2.0f * q0q1 + _2q2q3 - ay) - 4.0f * q2 * (1 - 2.0f * q1q1 - 2.0f * q2q2 - az) + (-_4bx * q2 - _2bz * q0) * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (_2bx * q1 + _2bz * q3) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + (_2bx * q0 - _4bz * q2) * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
            s3 = _2q1 * (2.0f * q1q3 - _2q0q2 - ax) + _2q2 * (2.0f * q0q1 + _2q2q3 - ay) + (-_4bx * q3 + _2bz * q1) * (_2bx * (0.5f - q2q2 - q3q3) + _2bz * (q1q3 - q0q2) - mx) + (-_2bx * q0 + _2bz * q2) * (_2bx * (q1q2 - q0q3) + _2bz * (q0q1 + q2q3) - my) + _2bx * q1 * (_2bx * (q0q2 + q1q3) + _2bz * (0.5f - q1q1 - q2q2) - mz);
            recipNorm = invSqrt(s0 * s0 + s1 * s1 + s2 * s2 + s3 * s3); // normalise step magnitude
            s0 *= recipNorm;
            s1 *= recipNorm;
            s2 *= recipNorm;
            s3 *= recipNorm;

            // Apply feedback step
            qDot1 -= beta * s0;
            qDot2 -= beta * s1;
            qDot3 -= beta * s2;
            qDot4 -= beta * s3;
        }

        // Integrate rate of change of quaternion to yield quaternion
        q0 += qDot1 * (1.0f / sampleFreq);
        q1 += qDot2 * (1.0f / sampleFreq);
        q2 += qDot3 * (1.0f / sampleFreq);
        q3 += qDot4 * (1.0f / sampleFreq);

        // Normalise quaternion
        recipNorm = invSqrt(q0 * q0 + q1 * q1 + q2 * q2 + q3 * q3);
        q0 *= recipNorm;
        q1 *= recipNorm;
        q2 *= recipNorm;
        q3 *= recipNorm;

        Pitch = asin(-2.0f * (q1*q3 - q0*q2))* 57.3f;
        Roll = atan2(q0*q1 + q2*q3, 0.5f - q1*q1 - q2*q2) * 57.3f;
        Yaw = atan2(q1*q2 + q0*q3, 0.5f - q2*q2 - q3*q3)* 57.3f;
    }

}
