package com.unity3d.player;

import android.content.Context;
import android.util.Log;

import com.nolovr.androidsdkclient.NoloVR;
import com.nolovr.androidsdkclient.callback.ControllerChangeCallback;
import com.nolovr.bean.NControllerData;
import com.nolovr.bean.NControllerState;
import com.nolovr.bean.NTrackedDevicePose;

import org.json.JSONException;
import org.json.JSONObject;

import java.nio.ByteBuffer;

public class NoloBridge {

    NoloVR noloVR;

    public void OnClose() {
        noloVR.closeServer();
    }

    public String InitNolo(Context ctx) {
        try {
            noloVR = NoloVR.getInstance(ctx);
            boolean isInstall = noloVR.isStallServer();
            if (isInstall) {
                noloVR.openServer();
                noloVR.setAppKey("4e4f4c4f484f4d457eff82725bc694a5");
            } else {
                return "Not Installed";
            }
        } catch (Exception e) {
            return e.getMessage();
        }

        return "OK";
    }

    public String GetDevicesData() {
        JSONObject json = new JSONObject();

        try {
            for (int deviceID = 0; deviceID < 4; deviceID++) {
                NTrackedDevicePose noloPose = noloVR.getPoseByDeviceType(deviceID);
                JSONObject deviceJson = new JSONObject();
                deviceJson.put("status", noloPose.getStatus());

                deviceJson.put("x", noloPose.getPos().getX());
                deviceJson.put("y", noloPose.getPos().getY());
                deviceJson.put("z", noloPose.getPos().getZ());

                deviceJson.put("rx", noloPose.getRot().getX());
                deviceJson.put("ry", noloPose.getRot().getY());
                deviceJson.put("rz", noloPose.getRot().getZ());
                deviceJson.put("rw", noloPose.getRot().getW());

                NControllerState c = noloVR.getControllerStatesByDeviceType(deviceID);
                deviceJson.put("button", c.getButtons());
                deviceJson.put("touch", c.getTouches());
                deviceJson.put("touchx", c.getTouchpadAxis().getX());
                deviceJson.put("touchy", c.getTouchpadAxis().getY());

                int battery = noloVR.getElectricityValueByDeviceType(deviceID);
                deviceJson.put("battery", battery);

                json.put("D" + deviceID, deviceJson);
            }

            json.put("success", "true");
        } catch (Exception e) {
            Log.v("Unity", e.getMessage());
            try {
                json.put("success", "false");
                json.put("error", e.toString());
            } catch (JSONException ex) {
                ex.printStackTrace();
            }
        }

        return json.toString();
    }

    public int Add(int a, int b) {
        return a + b;
    }
}
