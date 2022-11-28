using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.UI;

public class HPHandler : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnHPChanged))]
    byte HP { get; set; }

    [Networked(OnChanged = nameof(OnStateChanged))]
    public bool isDead { get; set; }

    bool isInitialized = false;

    // Screen flash on damage taken
    public Color uiOnHitColor;
    public Image uiOnHitImage;
    public MeshRenderer bodyMeshRenderer;
    Color defaultMeshBodyColor;

    public GameObject playerModel;
    public GameObject deathGameObjectPrefab;    // Death particle system

    const byte startingHP = 5;

    // Other Components
    CharacterMovementHandler characterMovementHandler;
    HitboxRoot hitboxRoot;      // Used to prevent hitting the player twice - when dead we'll just disable it 
                                // Could use a despawn / respawn setup but for now we'll just use this



    private void Awake()
    {
        characterMovementHandler = GetComponent<CharacterMovementHandler>();
        hitboxRoot = GetComponentInChildren<HitboxRoot>();
    }

    // Start is called before the first frame update
    void Start()
    {
        HP = startingHP;
        isDead = false;
        defaultMeshBodyColor = bodyMeshRenderer.material.color;

        isInitialized = true;
    }

    IEnumerator OnHitCoRoutine()
    {
        bodyMeshRenderer.material.color = Color.white;

        // Flash white when we're hit - check we have input authority (this means it's our player)
        if (Object.HasInputAuthority)
            uiOnHitImage.color = uiOnHitColor;

        yield return new WaitForSeconds(0.1f);                      // Wait 100ms
        bodyMeshRenderer.material.color = defaultMeshBodyColor;     // Set back to default color (end the flash)

        // If we're dead just reset the colour
        if (Object.HasInputAuthority && !isDead)
            uiOnHitImage.color = new Color(0, 0, 0, 0);
    }

    IEnumerator ServerReviveCoRoutine()
    {
        yield return new WaitForSeconds(2.0f);
        characterMovementHandler.RequestRespawn();
    }

    // NOTE:  Function should only be called by server
    public void OnTakeDamage()
    {
        // Only take damage if we're alive
        if (isDead)
            return;

        HP -= 1;
        Debug.Log($"{Time.time}:  {transform.name} took damage!  HP:  {HP}");

        // Player Died
        if (HP <= 0)
        {
            isDead = true;              
            StartCoroutine(ServerReviveCoRoutine());    // Call the respawn coroutine

            Debug.Log($"{Time.time}:  {transform.name} DIED!");
        }
    }

    // REMEMBER:  Can't access static variables (OnHPChanged / OnStateChanged) so make local accessible functions to change them
    static void OnHPChanged(Changed<HPHandler> changed)
    {
        Debug.Log($"{Time.time}  OnHPChanged value:  {changed.Behaviour.HP}");

        // Get our new HP value
        byte newHP = changed.Behaviour.HP;

        // Load our OLD HP value
        changed.LoadOld();
        byte oldHP = changed.Behaviour.HP;

        // Check if our HP has been decreased
        if (newHP < oldHP)
            changed.Behaviour.OnHPReduced();
    }

    private void OnHPReduced()
    {
        if (!isInitialized)
            return;

        StartCoroutine(OnHitCoRoutine());   // Calls our player / screen flash
    }


    // HANDLES PLAYER STATE  -  DEAD or ALIVE
    static void OnStateChanged(Changed<HPHandler> changed)
    {
        Debug.Log($"{Time.time}  OnStateChanged isDead:  {changed.Behaviour.isDead}");

        // Get current player state (alive/dead)
        bool isCurrentlyDead = changed.Behaviour.isDead;

        // Load OLD player state (alive/dead) - from last update
        changed.LoadOld();
        bool isCurrentlyDeadOld = changed.Behaviour.isDead;

        
        if (isCurrentlyDead)                                // Check if we're currently dead and tell server
            changed.Behaviour.OnDeath();
        else if (!isCurrentlyDead && isCurrentlyDeadOld)    // We've been revived - we're no longer dead - update server
            changed.Behaviour.OnRevive();
    }

    private void OnDeath()
    {
        Debug.Log($"{Time.time}:  OnDeath");
        playerModel.gameObject.SetActive(false);    
        hitboxRoot.HitboxRootActive = false;                                // Prevent being hit when dead
        characterMovementHandler.SetCharacterControllerEnabled(false);      // Disable player controls

        Instantiate(deathGameObjectPrefab, transform.position, Quaternion.identity);    // Spawn our death particle effect
    }

    private void OnRevive()
    {
        Debug.Log($"{Time.time}:  OnRevive");

        // If its our player
        if (Object.HasInputAuthority)
            uiOnHitImage.color = new Color(0, 0, 0, 0);

        playerModel.gameObject.SetActive(true);
        hitboxRoot.HitboxRootActive = true;
        characterMovementHandler.SetCharacterControllerEnabled(true);   // Enable player controls
    }

    public void OnRespawned()
    {
        // Reset vars
        HP = startingHP;
        isDead = false;
    }
}
