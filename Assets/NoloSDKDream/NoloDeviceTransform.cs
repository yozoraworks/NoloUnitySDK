using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoloDeviceTransform : MonoBehaviour
{
    public int deviceID = 0;

    public bool pos = true, rot = true;

    // Update is called once per frame
    void Update()
    {
        if (deviceID != 3 && NoloBridge.devices[deviceID].position == Vector3.zero)
        {
            transform.position = Vector3.down * 10000f;
            transform.rotation = NoloBridge.devices[deviceID].rotation;
        }
        else
        {
            /*
            if (deviceID == 1 || deviceID == 2)
            {
                GetComponentInChildren<UnityEngine.UI.Text>().text = "Y: " + NoloBridge.devices[deviceID].position.y.ToString("F3") + " 瞳距" + Mathf.Abs(L.transform.localPosition.x - R.transform.localPosition.x).ToString("F4");
            }
             */

            if (pos)
                transform.localPosition = NoloBridge.devices[deviceID].position;
            if (rot)
                transform.localRotation = NoloBridge.devices[deviceID].rotation;
        }
    }
}
