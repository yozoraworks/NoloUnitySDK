using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager instance = null;
    float enterTime = 0f;
    float escTime = -1f;

    private bool rotated = false;

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

        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            if (!rotated)
                Next();
            rotated = false;
        }
        else if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            //rotate by screen axis
            var rotation = Input.GetTouch(0).deltaPosition * 0.3f;
            transform.RotateAround(transform.position, Vector3.right, rotation.x);
            transform.RotateAround(transform.position, Vector3.up, -rotation.y);

            rotated = true;
        }

        //zoom
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);
            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
            float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
            float touchDeltaMag = (touch0.position - touch1.position).magnitude;
            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
            var oldScale = transform.localScale;
            oldScale -= Vector3.one * (deltaMagnitudeDiff * 0.01f);
            //limit
            if (oldScale.x < 0.2f)
                oldScale = Vector3.one * 0.2f;
            if (oldScale.x > 3f)
                oldScale = Vector3.one * 3f;

            transform.localScale = oldScale;
        }
    }

    public void Next()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf)
            {
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