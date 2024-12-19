using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager instance = null;
    float enterTime = 0f;
    float escTime = -1f;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            transform.position += Vector3.forward * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            transform.position += Vector3.back * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(Vector3.up * Time.deltaTime * 60f);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(-Vector3.up * Time.deltaTime * 60f);
        }

        float now = Time.unscaledTime;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (now - escTime < 0.5f)
                Application.Quit();

            escTime = now;
        }


        if (Input.GetKeyUp(KeyCode.Return) && enterTime < 0.5f)
        {
            ObjectControl.instance.DoAnimation();
        }

        if (Input.GetKey(KeyCode.Return))
        {
            enterTime += Time.unscaledDeltaTime;
            if (enterTime > 0.5f)
            {
                Head.ResetCamera();
                transform.position = new Vector3(0f, -0.12f, -0.4f);
                enterTime = 0f;

                Next();
            }
        }
        else
            enterTime = 0f;
        
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Next();
        }
    }

    public void Next()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf) {
                transform.GetChild(i).gameObject.SetActive(false);
                int a = i + 1;
                if (a == transform.childCount)
                    a = 0;
                transform.GetChild(a).gameObject.SetActive(true);
                break;
            }
        }
    }
}
