using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovementHandler : NetworkBehaviour
{
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
   // Camera localCamera;
    HPHandler hpHandler;
    NetworkInGameMessages networkInGameMessages;
    NetworkPlayer networkPlayer;

    bool isRespawnRequested = false;        // Detects if a respawn request has been called 


    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponent<NetworkCharacterControllerPrototypeCustom>();
        hpHandler = GetComponent<HPHandler>();
        networkInGameMessages = GetComponent<NetworkInGameMessages>();
        networkPlayer = GetComponent<NetworkPlayer>();
        //localCamera = GetComponentInChildren<Camera>();

    }

    // Update is triggered locally only, don't use for network functions
    void Update()
    {
    }

    // Network Update
    public override void FixedUpdateNetwork()
    {
        // Don't update our position if we're dead
        if (Object.HasStateAuthority)
        {
            if (isRespawnRequested)
            {
                Respawn();
                return;
            }

            if (hpHandler.isDead)
                return;
        }

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
            if (networkInputData.isJumpButtonPressed)
                networkCharacterControllerPrototypeCustom.Jump();

            // Check if we've fallen of the map
            CheckFallRespawn();
        }
    }


    // Checks to see if the player has fell off the map and respawns it
    private void CheckFallRespawn()
    {
        if (transform.position.y < -12)
        {
            if (Object.HasStateAuthority)
            {
                Debug.Log($"{Time.time}:  CheckFallRespawn() called:   Fell off map at position:  {transform.position}");
                networkInGameMessages.SendInGameRPCMessage(networkPlayer.nickName.ToString(), "fell to their death...");
                Respawn();
            }
        }

    }

    // For external respawn request
    public void RequestRespawn()
    {
        Debug.Log($"{Time.time}:  RequestRespawn() called by:   {transform.root.name}");
        isRespawnRequested = true;
    }


    // Called when dead
    private void Respawn()
    {
        networkCharacterControllerPrototypeCustom.TeleportToPosition(Utils.GetRandomSpawnPoint());
        hpHandler.OnRespawned();        // Resets player values to default ready for next life
        isRespawnRequested = false;
    }

    public void SetCharacterControllerEnabled(bool isEnabled)
    {
        networkCharacterControllerPrototypeCustom.Controller.enabled = isEnabled;
    }
}
