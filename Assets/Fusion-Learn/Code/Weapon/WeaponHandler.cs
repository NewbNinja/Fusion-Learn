using Fusion;
using System.Collections;
using UnityEngine;

public class WeaponHandler : NetworkBehaviour
{
    // This is a FUSION network function which updates all clients
    [Networked(OnChanged = nameof(OnFireChanged))]
    public bool isFiring { get; set; }

    public ParticleSystem fireParticleSystem;   // Muzzle flash particles
    public Transform aimPoint;                  // Position we're firing from
    public LayerMask collisionLayers;           // Holds all collision layers 

    [SerializeField] private float fireDelay = 0.15f;
    float lastTimeFired = 0;        // Fire Rate

    // Other Components
    HPHandler hpHandler;
    NetworkPlayer networkPlayer;


    void Awake()
    {
        hpHandler = GetComponent<HPHandler>();
        networkPlayer = GetComponent<NetworkPlayer>();
    }

    // Fusion refers to this as FUN -- the Fixed Update Network func - works like local Update but for the network
    // STEP 1: CHECK IF WE'RE FIRING
    public override void FixedUpdateNetwork()
    {
        // Don't allow us to fire if we're dead
        if (hpHandler.isDead)
            return;

        Debug.Log("WeaponHandler.FUN() called");
        // Get the input from the network
        if (GetInput(out NetworkInputData networkInputData))
        {
            if (networkInputData.isFirePressed)
                Fire(networkInputData.aimForwardVector);        
        }
    }


    // STEP 2:  FIRE + CALL COROUTINE TO UPDATE SERVER
    // Only being called by the client who pressed the button, only this client and server will know about this
    void Fire(Vector3 aimForwardVector)
    {
        // Fire Rate Limiter - if fire delay has NOT elapsed, dont fire
        if (Time.time - lastTimeFired < fireDelay)
            return;

        StartCoroutine(FireEffectCoRoutine());      // Fire (tells server we're firing + Plays the muzzle flash particle system)

        // SHOOTING using Raycasts and Lag Compensation
        Runner.LagCompensation.Raycast(aimPoint.position,              // Our origin - where we are firing from
                                        aimForwardVector,               // Aim direction
                                        100,                            // How long can we fire - distance value
                                        Object.InputAuthority,          // Who has authority for this raycast (our player authority)
                                        out var hitInfo,                // Receive some hit information
                                        collisionLayers,                // Choose which colliders we want to process
                                        HitOptions.IncludePhysX |       // Will consider environmental colliders from Unity (so we can hide behind boxes etc)
                                        HitOptions.IgnoreInputAuthority // Will prevent an error where we can hit ourselves        
                                        );                              // and stops us shooting ourselves when we move backwards

        float hitDistance = 100;
        bool isHitOtherPlayer = false;


        // ####  COLLISION DETECTION  ####
        //-----------------------------------------------------------------------------
        // Stop ray when we hit something
        if (hitInfo.Distance > 0)
            hitDistance = hitInfo.Distance;

        // Fusion: Handling Network Hits - We've hit another networked player!
        if (hitInfo.Hitbox != null)
        {
            Debug.Log($"{Time.time} {transform.name} hit Fusion HitBox:  {hitInfo.Hitbox.transform.root.name}");

            // IMPORTANT NOTE:   Only update the health changes on the server (if we have state authority)
            // Here we assume our network objects have a HPHandler script attached (REQUIRED)
            if (Object.HasStateAuthority)
                hitInfo.Hitbox.transform.root.GetComponent<HPHandler>().OnTakeDamage(networkPlayer.nickName.ToString());

            isHitOtherPlayer = true;
        }

        // Unity: Handle Unity PhysX Colliders (usually walls, scenery, non-networked objects)
        else if (hitInfo.Collider != null)
            Debug.Log($"{Time.time} {transform.name} hit Unity PhysX Collider:  {hitInfo.Collider.transform.name}");


        // ### DEBUGGING  - remove later - hit enemy = draw red, else draw green
        if (isHitOtherPlayer)
                Debug.DrawRay(aimPoint.position, aimForwardVector * hitDistance, Color.red, 1);
            else Debug.DrawRay(aimPoint.position, aimForwardVector * hitDistance, Color.green, 1);

        //-----------------------------------------------------------------------------


        lastTimeFired = Time.time;                  // Record the time we fired for fire rate rule
    }


    // STEP 3:  CALLS COROUTINE TO CHANGE NETWORK VARIABLE isFiring TO TRUE
    // Coroutine - Tell the server we're firing
    // Server Authoritative - if a client tries to change this the server will just change it back
    IEnumerator FireEffectCoRoutine()
    {
        isFiring = true;                        // Change network variable
        fireParticleSystem.Play();              // Fire the muzzle flash (particle system)
        yield return new WaitForSeconds(0.09f); // Wait very short amount of time 9ms - just neough time for clients to update
                                                // This should be using TickTimer??? - ???
        isFiring = false;                       // Tell server we've stopped firing
    }

    // IMPORTANT:  Static function cannot access anything that is not static
    // This function is only to manage visuals (muzzle flash - particle effect)
    // Not really a problem if it's slightly delayed
    static void OnFireChanged(Changed<WeaponHandler> changed)
    {
        //Debug.Log($"{Time.time} OnFireChanged value:  {changed.Behaviour.isFiring}");

        // NOTE:  We can't use this as it is a STATIC function
        // isFiring = true;
        // So we have to do it the following way below:

        bool isCurrentlyFiring = changed.Behaviour.isFiring;    // Check whether we're firing now
        changed.LoadOld();                                      // Now check the OLD variable from last update
        bool isFiringOld = changed.Behaviour.isFiring;          // Now assign the result of our check

        // Checks that there has been a change in firing state
        // equivalent to:   isFiring = true  OR  isFiring = false;
        if (isCurrentlyFiring && !isFiringOld)
            changed.Behaviour.OnFireRemote();
    }


    void OnFireRemote()
    {
        // Plays the particle muzzle flash on all OTHER network clients but our own
        if (!Object.HasInputAuthority)
            fireParticleSystem.Play();
    }

}
