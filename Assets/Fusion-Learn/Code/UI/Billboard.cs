using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera theCam;          // Cam handler
    public bool useStaticBillboard; // When we get close, don't rotate

    void Start()
    {
        // Get the Player Camera object tagged "LocalPlayerCamera" in Unity
        theCam = GameObject.FindGameObjectWithTag("LocalPlayerCamera").GetComponent<Camera>();
    }

    void Update()
    {
        // When this script is attached, this will force the object to look at the main cam
        transform.LookAt(theCam.transform);
        // Stops the text from showing as reversed
        transform.rotation = theCam.transform.rotation;
        // X+Y axis -- Not the Z
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0f);
    }
}
