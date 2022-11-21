using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public static NetworkPlayer Local { get; set; }     // Save our local network player object in Local

    public Transform playerModel;

    public override void Spawned()
    {
        // If we have input authority - this is our object  NOTE:  Without this we would affected EVERY player object
        if (Object.HasInputAuthority)
        {
            Local = this;

            // USAGE:   Prevents player model blocking view of  the camera on player prefab by changing the render layer of the local players model 
            // INFO:    https://www.youtube.com/watch?v=ndL1siRSBg8&t=30s
            // NOTES:   We've set the camera on the NetworkPlayerPF (prefab) to ignore layer:6 ("LocalPlayerModel")
            //          We do this so that the camera ignores the local players prefab so that is doesn't block our view
            Utils.SetRenderLayerInChildren(playerModel, LayerMask.NameToLayer("LocalPlayerModel"));

            // Disable Main Camera
            //Camera.main.gameObject.SetActive(false);

            Debug.Log("Spawned local player");
        }
        else
        {
            // Disable the camera if we are not the local player
            Camera localCamera = GetComponentInChildren<Camera>();
            localCamera.enabled = false;

            // Disable all other audio listeners EXCEPT local players Audio Listener
            AudioListener audioListener = GetComponentInChildren<AudioListener>();
            audioListener.enabled = false;

            Debug.Log("Spawned remote player");
        }

        // Make it easier to tell which player is which
        transform.name = $"P_{Object.Id}";
    }

    // If player leaves, if we have input authority then despawn the player object
    public void PlayerLeft(PlayerRef player)
    {
        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }
}
