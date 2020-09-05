using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoController : MonoBehaviour
{
    public GameObject collided = null;
    public GameObject attached = null;
    public GameObject lastPickedObject = null;

    public GameObject root;

    Transform originalParent = null;

    public int deviceID = 1;

    Vector2 lastTouch = Vector2.zero;
    bool lastTouchpadPress = false;

    // Update is called once per frame
    void Update()
    {
        if (NoloBridge.devices[deviceID].trigger && attached == null)
        {
            attached = collided;
            originalParent = attached.transform.parent;
            attached.transform.parent = transform;
            lastPickedObject = attached;
        }
        else if (!NoloBridge.devices[deviceID].trigger && attached != null)
        {
            attached.transform.parent = originalParent;
            attached = null;
        }

        if (NoloBridge.devices[deviceID].touched)
        {
            if (lastTouch != Vector2.zero)
            {
                Vector2 delta = NoloBridge.devices[deviceID].touchAxis - lastTouch;
                if (ObjectControl.instance.isSplit && lastPickedObject != null)
                {
                    lastPickedObject.transform.RotateAround(lastPickedObject.transform.position, Vector3.up, -delta.x * 30f);
                }
                else
                {
                    root.transform.RotateAround(root.transform.position, Vector3.up, -delta.x * 30f);
                }
            }

            lastTouch = NoloBridge.devices[deviceID].touchAxis;
        } else
        {
            lastTouch = Vector2.zero;
        }

        if (NoloBridge.devices[deviceID].touchpadPressed && !lastTouchpadPress)
        {
            if (attached != null)
            {
                attached.transform.parent = originalParent;
                attached = null;
            }

            lastPickedObject = null;

            //animation
            ObjectControl.instance.DoAnimation();
        }

        lastTouchpadPress = NoloBridge.devices[deviceID].touchpadPressed;

        if (NoloBridge.devices[deviceID].system)
        {
            //restart
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        collided = other.gameObject;
    }

    private void OnTriggerExit(Collider other)
    {
        collided = null;
    }
}
