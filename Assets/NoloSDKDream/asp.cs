using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class asp : MonoBehaviour
{
    public bool UpDown = true;

    // Start is called before the first frame update
    void Start()
    {
        if (UpDown)
            GetComponent<Camera>().aspect /= 2f;
        else GetComponent<Camera>().aspect *= 2f;
    }

    // Update is called once per frame
    void Update()
    {
    }
}