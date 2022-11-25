using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponHandler : NetworkBehaviour
{
    // This is a FUSION network function which updates all clients
    [Networked(OnChanged = nameof(OnFireChanged))]
    public bool isFiring { get; set; }
    public ParticleSystem fireParticleSystem;

    [SerializeField] private float fireDelay = 0.15f;
    float lastTimeFired = 0;        // Fire Rate


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Fusion refers to this as FUN -- the Fixed Update Network func - works like local Update but for the network
    // STEP 1: CHECK IF WE'RE FIRING
    public override void FixedUpdateNetwork()
    {
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

        StartCoroutine(FireEffectCoRoutine());      // Fire (tells the server we're firing + muzzle flash)
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
        Debug.Log($"{Time.time} OnFireChanged value:  {changed.Behaviour.isFiring}");

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
