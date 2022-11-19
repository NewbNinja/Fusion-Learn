using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovementHandler : NetworkBehaviour
{
    NetworkCharacterControllerPrototypeCustom networkCharacterControllerPrototypeCustom;
    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponent<NetworkCharacterControllerPrototypeCustom>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is triggered locally only, don't use for network functions
    void Update()
    {
        
    }

    // Network update
    public override void FixedUpdateNetwork()
    {
        // Get the network input data so we can move the character
        if (GetInput(out NetworkInputData networkInputData))
        {
            // Move
            Vector3 moveDirection = transform.forward * networkInputData.movementInput.y +  // Back n Forward
                                    transform.right * networkInputData.movementInput.x;     // Strafe
            
            moveDirection.Normalize();
            networkCharacterControllerPrototypeCustom.Move(moveDirection);
        }
    }
}
