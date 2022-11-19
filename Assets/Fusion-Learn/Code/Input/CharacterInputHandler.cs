using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInputHandler : MonoBehaviour
{
    Vector2 moveInputVector = Vector2.zero;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Move Input
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");
    }


    // The public void OnInput() function is handled in SPAWNER.cs and should only be handled in ONE PLACE!
    // For this reason we are going to expose the data here in this class so we can still
    public NetworkInputData GetNetworkInput()
    {
        // HOW IT WORKS:
        // Update collects all the frames input data and when the Network Update function is
        // ready for the data we simply pass it along by returning the networkInputData

        NetworkInputData networkInputData = new NetworkInputData();
        networkInputData.movementInput = moveInputVector;
        return networkInputData;

    }
}
