using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundsFollow : MonoBehaviour
{
    public GameObject bounds;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bounds.transform.position = transform.position;
    }
}
