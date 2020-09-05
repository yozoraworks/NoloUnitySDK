using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectControl : MonoBehaviour
{
    public static ObjectControl instance = null;

    public GameObject tracker;

    public bool isSplit = false;

    public GameObject[] parts;
    Vector3[] poss;
    Quaternion[] rots;
    
    bool animating = false;

    void Awake()
    {
        instance = this;

        poss = new Vector3[parts.Length];
        rots = new Quaternion[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            poss[i] = parts[i].transform.localPosition;
            rots[i] = parts[i].transform.localRotation;
        }
    }

    public void DoAnimation()
    {
        if (animating)
            return;

        isSplit = !isSplit;

        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = isSplit;
        }
        GetComponent<Collider>().enabled = !isSplit;

        StartCoroutine(anim());
    }

    IEnumerator anim()
    {
        animating = true;

        Vector3[] _poss = new Vector3[parts.Length];
        Quaternion[] _rots = new Quaternion[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            _poss[i] = parts[i].transform.localPosition;
            _rots[i] = parts[i].transform.localRotation;
        }

        for (float t = 0f; t < 1f; t += 0.05f)
        {
            if (isSplit)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    parts[i].transform.localPosition = Vector3.Lerp(_poss[i], poss[i] * 100f, Mathf.Sin(t * Mathf.PI / 2f));
                    parts[i].transform.localRotation = Quaternion.Lerp(_rots[i], rots[i], Mathf.Sin(t * Mathf.PI / 2f));
                }
            }
            else
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    parts[i].transform.localPosition = Vector3.Lerp(_poss[i], poss[i], Mathf.Sin(t * Mathf.PI / 2f));
                    parts[i].transform.localRotation = Quaternion.Lerp(_rots[i], rots[i], Mathf.Sin(t * Mathf.PI / 2f));
                }
            }

            yield return new WaitForSecondsRealtime(0.01f);
        }

        animating = false;
    }
}
