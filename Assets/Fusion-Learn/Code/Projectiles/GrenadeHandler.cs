using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;


public class GrenadeHandler : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject explosionParticleSystemPrefab;

    [Header("Collision detection")]
    public LayerMask collisionLayers;

    [Header("Thrown By Info")]
    PlayerRef thrownByPlayerRef;
    string thrownByPlayerName;

    // Grenade info
    [SerializeField] byte damageAmount = 80;
    [SerializeField] int explosionRadius = 20;


    // Timing
    TickTimer explodeTickTimer = TickTimer.None;

    //Hit info
    List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

    //Other components
    NetworkObject networkObject;
    NetworkRigidbody networkRigidbody;

    public void Throw(Vector3 throwForce, PlayerRef thrownByPlayerRef, string thrownByPlayerName)
    {
        networkObject = GetComponent<NetworkObject>();
        networkRigidbody = GetComponent<NetworkRigidbody>();

        // Throw force
        networkRigidbody.Rigidbody.AddForce(throwForce, ForceMode.Impulse);

        // Record who threw the grenade (used for kill feed etc)
        this.thrownByPlayerRef = thrownByPlayerRef;
        this.thrownByPlayerName = thrownByPlayerName;

        // The CORRECT way to use timers using networking (don't use CoRoutines)
        explodeTickTimer = TickTimer.CreateFromSeconds(Runner, 2);
    }

    //Network update
    public override void FixedUpdateNetwork()
    {

        // SERVER:  Check for explosion and process it
        if (Object.HasStateAuthority)
        {
            if (explodeTickTimer.Expired(Runner))
            {
                int hitCount = Runner.LagCompensation.OverlapSphere(transform.position,         // On explode - check position
                                                                    8f,                        // Check for hits within 4 units
                                                                    thrownByPlayerRef,          // Lag compensation for the player who threw it
                                                                    hits,                       // Return valid hits as a list of List<LagCompensatedHit>
                                                                    collisionLayers);           // On what layers?

                // Loop through our list of hits and see what was hit
                for (int i = 0; i < hitCount; i++)
                {
                    HPHandler hpHandler = hits[i].Hitbox.transform.root.GetComponent<HPHandler>();  // Check to see if what we hit has a hitbox + HPHandler component

                    if (hpHandler != null)                                  // If we had a HPHandler it's a valid damageable object
                        hpHandler.OnTakeDamage(thrownByPlayerName, damageAmount);    // Process damage
                }

                // Despawn the grenade body
                Runner.Despawn(networkObject);

                //Stop the explode timer from being triggered again
                explodeTickTimer = TickTimer.None;
            }
        }
    }

    //When despawning the object we want to create a visual explosion - DOES NOT have be a networked object
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Use the grenades mesh to get its world position then instantiate the explosion
        // We grab the mesh as it may be being interpolated so we need the EXACT position 
        // when the timer elapses so we can spawn the explosion in the correct place
        MeshRenderer grenadeMesh = GetComponentInChildren<MeshRenderer>();
        Instantiate(explosionParticleSystemPrefab, grenadeMesh.transform.position, Quaternion.identity);
    }

}
