using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInputHandler : MonoBehaviour
{
    Vector2 moveInputVector = Vector2.zero;
    Vector2 viewInputVector = Vector2.zero;
    bool isJumpButtonPressed = false;

    // Reference our character movement handler
    LocalCameraHandler localCameraHandler;

    private void Start()
    {
        // We're using mouse - so set it up
        Cursor.lockState = CursorLockMode.Locked;   // Stops cursor from leaving our window
        Cursor.visible = false;                     // Hides our cursor
    }

    private void Awake()
    {
        localCameraHandler = GetComponentInChildren<LocalCameraHandler>();
    }


    // Update is called once per frame
    void Update()
    {
        // View Input
        viewInputVector.x = Input.GetAxis("Mouse X");
        viewInputVector.y = Input.GetAxis("Mouse Y") * -1;      // INVERT the mouse look with "* -1"

        // Move Input
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");

        // Handle Jump
        if (Input.GetButtonDown("Jump"))
            isJumpButtonPressed = true;

        // Set View
        localCameraHandler.SetViewInputVector(viewInputVector);
    }


    // The public void OnInput() function is handled in SPAWNER.cs and should only be handled in ONE PLACE!
    // For this reason we are going to expose the data here in this class so we can still
    // CALLED BY THE SPAWNER class, OnInput() function
    public NetworkInputData GetNetworkInput()
    {
        // HOW IT WORKS:
        // Update() collects all the frames input data and when the Network Update function is
        // ready for the data we simply pass it along by returning the networkInputData

        NetworkInputData networkInputData = new NetworkInputData();
        networkInputData.aimForwardVector = localCameraHandler.transform.forward;   // Aim data
        networkInputData.movementInput = moveInputVector;       // Move data
        networkInputData.isJumpPressed = isJumpButtonPressed;   // Jump data
        isJumpButtonPressed = false;                            // RESET the jump trigger 
        return networkInputData;
    }
}
