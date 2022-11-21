using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovementHandler : NetworkBehaviour
{
    Vector2 viewInput;
    float cameraRotationX = 0;

    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    Camera localCamera;

    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponent<NetworkCharacterControllerPrototypeCustom>();
        localCamera = GetComponentInChildren<Camera>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is triggered locally only, don't use for network functions
    void Update()
    {
        //=== APPLY ROTATION TO CAMERA ===
        // Get the view rotation from input
        cameraRotationX += viewInput.y * Time.deltaTime * networkCharacterControllerPrototypeCustom.viewUpDownRotationSpeed;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -90, 90);    // Clamp the view range - simulates realistic human view angles
        localCamera.transform.localRotation = Quaternion.Euler(cameraRotationX, 0, 0);  // Apply
    }

    // Network Update
    public override void FixedUpdateNetwork()
    {
        // Get the network input data so we can move the character
        if (GetInput(out NetworkInputData networkInputData))
        {
            // Rotate
            networkCharacterControllerPrototypeCustom.Rotate(networkInputData.rotationInput);

            // Move
            Vector3 moveDirection = transform.forward * networkInputData.movementInput.y +  // Back n Forward
                                    transform.right * networkInputData.movementInput.x;     // Strafe
            
            moveDirection.Normalize();
            networkCharacterControllerPrototypeCustom.Move(moveDirection);

            // Jump
            if (networkInputData.isJumpPressed)
                networkCharacterControllerPrototypeCustom.Jump();
        }
    }

    public void SetViewInputVector(Vector2 viewInput)
    {
        this.viewInput = viewInput;
    }
}
