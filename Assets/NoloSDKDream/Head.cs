using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Head : MonoBehaviour
{
    private void Awake()
    {
        Input.gyro.enabled = true;
        Input.gyro.updateInterval = 1f / 120f;
    }

    public static bool reloaded = true;
    static float offsetAngle = 0f;

    // Start is called before the first frame update
    void Start()
    {

    }

    public static void ResetCamera(float angle = 0f)
    {
        offsetAngle = angle;
        reloaded = true;
    }

    void GyroModifyCamera()
    {
        //Apply offset
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f) * GyroToUnity(Input.gyro.attitude);
        if (reloaded)
        {
            if (offsetAngle == 0f)
                transform.parent.rotation = Quaternion.Euler(new Vector3(0f, -transform.localRotation.eulerAngles.y, 0f));
            else
                transform.parent.rotation = Quaternion.Euler(new Vector3(0f, offsetAngle, 0f));

            reloaded = false;
        }
    }

    private static Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }


    // Update is called once per frame
    void Update()
    {
        //transform.Rotate(-Input.gyro.rotationRate.x, -Input.gyro.rotationRate.y, Input.gyro.rotationRate.z);
        GyroModifyCamera();
    }
}