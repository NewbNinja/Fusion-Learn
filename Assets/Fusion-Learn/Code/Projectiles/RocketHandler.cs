using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class RocketHandler : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject explosionParticleSystemPrefab;

    [Header("Collision detection")]
    public Transform checkForImpactPoint;
    public LayerMask collisionLayers;

    //Timing
    TickTimer maxLiveDurationTickTimer = TickTimer.None;
    [SerializeField] float timeToLive = 10f;

    //Rocket info
    //[SerializeField] float rocketSpeed = 50f;
    [SerializeField] byte damagePerRocket = 40;
    [SerializeField] float blastRadius = 1f;
    [SerializeField] float hitRadius = 0.1f;


    //Hit info
    List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

    //Fired by info
    PlayerRef firedByPlayerRef;
    string firedByPlayerName;
    NetworkObject firedByNetworkObject;

    //Other components
    NetworkObject networkObject;

    public void Fire(PlayerRef firedByPlayerRef, NetworkObject firedByNetworkObject, string firedByPlayerName)
    {
        this.firedByPlayerRef = firedByPlayerRef;
        this.firedByPlayerName = firedByPlayerName;
        this.firedByNetworkObject = firedByNetworkObject;

        networkObject = GetComponent<NetworkObject>();

        maxLiveDurationTickTimer = TickTimer.CreateFromSeconds(Runner, timeToLive);
    }

    public override void FixedUpdateNetwork()
    {
        transform.position += transform.forward * Runner.DeltaTime * 30f;

        // SERVER:   Handle ROCKET objects
        if (Object.HasStateAuthority)
        {
            // Check if the rocket has reached the end of its life
            if (maxLiveDurationTickTimer.Expired(Runner))
            {
                Runner.Despawn(networkObject);
                return;
            }

            // Check if the rocket has hit anything
            int hitCount = Runner.LagCompensation.OverlapSphere(checkForImpactPoint.position,       // On impact - check position
                                                                hitRadius,                        // THIS IS THE COLLIDER RADIUS
                                                                firedByPlayerRef,                   // Lag compensation for the player who fired it
                                                                hits,                               // Return valid hits as a list of List<LagCompensatedHit>
                                                                collisionLayers,                    // On what layers?
                                                                HitOptions.IncludePhysX |           // Check all colliders in the scene for the collision
                                                                HitOptions.IgnoreInputAuthority);   // Ignore hits on self    

            bool isValidHit = false;    // Reset value

            // We've hit something, so the hit could be valid
            if (hitCount > 0)
                isValidHit = true;

            //check what we've hit
            for (int i = 0; i < hitCount; i++)
            {
                // Check if we have hit a Hitbox
                if (hits[i].Hitbox != null)
                {
                    // Check that we didn't fire the rocket and hit ourselves. This can happen if the lag is a bit high.
                    if (hits[i].Hitbox.Root.GetBehaviour<NetworkObject>() == firedByNetworkObject)
                        isValidHit = false;
                }
            }

            // DID WE GET A VALID HIT?
            if (isValidHit)
            {
                //Now we need to figure out of anything was within the blast radius
                hitCount = Runner.LagCompensation.OverlapSphere(checkForImpactPoint.position,
                                                                blastRadius, 
                                                                firedByPlayerRef, 
                                                                hits, 
                                                                collisionLayers, HitOptions.None);

                // FIND DAMAGEABLE OBJECTS  -  Deal damage to anything within the hit radius
                for (int i = 0; i < hitCount; i++)
                {
                    HPHandler hpHandler = hits[i].Hitbox.transform.root.GetComponent<HPHandler>();

                    if (hpHandler != null)
                        hpHandler.OnTakeDamage(firedByPlayerName, damagePerRocket);
                }

                Runner.Despawn(networkObject);
            }
        }
    }

    //When despawning the object we want to create a visual explosion
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Instantiate(explosionParticleSystemPrefab, checkForImpactPoint.position, Quaternion.identity);
    }

}
