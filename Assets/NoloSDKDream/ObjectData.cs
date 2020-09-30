using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectData : MonoBehaviour
{
    public Transform myParent;

    // Start is called before the first frame update
    void Start()
    {
        myParent = transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
