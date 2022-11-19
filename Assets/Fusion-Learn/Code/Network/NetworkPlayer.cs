using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    public static NetworkPlayer Local { get; set; }     // Save our local network player object in Local

    // Start is called before the first frame update
    void Start()
    {

    }

    public override void Spawned()
    {
        // If we have input authority - this is our object  NOTE:  Without this we would affected EVERY player object
        if (Object.HasInputAuthority)
        {
            Local = this;
            Debug.Log("Spawned local player");
        }
        else Debug.Log("Spawned remote player");
    }

    // If player leaves, if we have input authority then despawn the player object
    public void PlayerLeft(PlayerRef player)
    {
        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }
}
