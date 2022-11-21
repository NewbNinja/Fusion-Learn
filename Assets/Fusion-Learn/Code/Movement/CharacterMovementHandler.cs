using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovementHandler : NetworkBehaviour
{
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    Camera localCamera;

    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponent<NetworkCharacterControllerPrototypeCustom>();
        localCamera = GetComponentInChildren<Camera>();
    }

    // Update is triggered locally only, don't use for network functions
    void Update()
    {
    }

    // Network Update
    public override void FixedUpdateNetwork()
    {
        // Get the network input data so we can move the character
        if (GetInput(out NetworkInputData networkInputData))
        {
            // Rotate
            transform.forward = networkInputData.aimForwardVector;      // May cause 180 degrees rotation snapping, just use network lerp if this is an issue later
            
            // Cancel Rotation on the X Axis!!  No tilting!
            Quaternion rotation = transform.rotation;
            rotation.eulerAngles = new Vector3(0f, rotation.eulerAngles.y, rotation.eulerAngles.z);
            transform.rotation = rotation;

            // Move
            Vector3 moveDirection = transform.forward * networkInputData.movementInput.y +  // Back n Forward
                                    transform.right * networkInputData.movementInput.x;     // Strafe

            moveDirection.Normalize();
            networkCharacterControllerPrototypeCustom.Move(moveDirection);

            // Jump
            if (networkInputData.isJumpPressed)
                networkCharacterControllerPrototypeCustom.Jump();

            // Check if we've fallen of the map
            CheckFallRespawn();
        }
    }


    // Checks to see if the player has fell off the map and respawns it
    void CheckFallRespawn()
    {
        if (transform.position.y < -12)
            transform.position = Utils.GetRandomSpawnPoint();
    }

}
