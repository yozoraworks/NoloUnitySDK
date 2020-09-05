using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoloDeviceTransform : MonoBehaviour
{
    public int deviceID = 0;

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
            transform.position = NoloBridge.devices[deviceID].position;
            transform.rotation = NoloBridge.devices[deviceID].rotation;
        }
    }
}
