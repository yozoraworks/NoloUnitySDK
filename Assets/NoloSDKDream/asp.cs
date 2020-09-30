using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class asp : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Camera>().aspect /= 2f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
