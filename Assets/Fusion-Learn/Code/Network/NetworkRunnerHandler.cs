using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System;
using System.Linq;

public class NetworkRunnerHandler : MonoBehaviour
{
    public NetworkRunner networkRunnerPrefab;
    NetworkRunner networkRunner;

    private void Awake()
    {
        // Check if we already have a network runner handler running in the scene and get it
        NetworkRunner networkRunnerInScene = FindObjectOfType<NetworkRunner>();

        // If we already have one, use the existing one and don't create another
        if (networkRunnerInScene != null)
            networkRunner = networkRunnerInScene;
    }


    private void Start()
    {
        // If we don't have a network runner, make a new one
        if (networkRunner == null)
        {
            // Create our network runner
            networkRunner = Instantiate(networkRunnerPrefab);
            networkRunner.name = "Network Runner";

            // We want to be able to handle clientTasks in the Game world so don't run it if we're calling this from the Main Menu
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                // Initialize the Network Runner - AutoHost (if no host, first client connected willbe host)
                var clientTask = InitializeNetworkRunner(networkRunner, GameMode.AutoHostOrClient, "TestSession", GameManager.instance.GetConnectionToken(), NetAddress.Any(), SceneManager.GetActiveScene().buildIndex, null);
            }

            Debug.Log($"Server NetworkRunner started.");
        }
    }

    public void StartHostMigration(HostMigrationToken hostMigrationToken)
    {
        // Create new network runner, the old one will be shut down
        networkRunner = Instantiate(networkRunnerPrefab);
        networkRunner.name = "Network Runner - Migrated";

        // Initiliaze the new network runner and provide old host information
        var clientTask = InitializeNetworkRunnerHostMigration(networkRunner, hostMigrationToken);
        Debug.Log($"Host migration started.");
    }

    INetworkSceneManager GetSceneManager(NetworkRunner runner)
    {
        // Get the scene manager component - or create one
        var sceneManager = runner.GetComponents(typeof(MonoBehaviour)).OfType<INetworkSceneManager>().FirstOrDefault();
        if (sceneManager == null)
            sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        return sceneManager;
    }


    // Check if there are any unity objects we need to consider (objects with colliders on them)
    protected virtual Task InitializeNetworkRunner(NetworkRunner runner, GameMode gameMode, string sessionName, byte[] connectionToken, NetAddress address, SceneRef scene, Action<NetworkRunner> initialized)
    {
        // Get the scene manager component - or create one
        var sceneManager = GetSceneManager(runner);
        runner.ProvideInput = true;

        /* IMPORTANT NOTE:
         * The StartGame call can have various arguments which can be found 
         * here:  https://doc.photonengine.com/en-us/fusion/current/manual/matchmaking#introduction
         * 
        */
        return runner.StartGame(new StartGameArgs 
        { 
            GameMode = gameMode,
            Address = address,
            Scene = scene,
            PlayerCount = 20,
            SessionName = sessionName,
            CustomLobbyName = "OurLobbyID",         // This will show all custom lobbies with this ID to us, can be used for (PREMIUM, VIP) style hidden lobbies
            Initialized = initialized,
            SceneManager = sceneManager,
            ConnectionToken = connectionToken
        });
    }

    // Check if there are any unity objects we need to consider (objects with colliders on them)
    protected virtual Task InitializeNetworkRunnerHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        // Get the scene manager component - or create one
        var sceneManager = GetSceneManager(runner);
        runner.ProvideInput = true;

        /* IMPORTANT NOTE:
         * The StartGame call can have various arguments which can be found 
         * here:  https://doc.photonengine.com/en-us/fusion/current/manual/matchmaking#introduction
         * 
        */
        return runner.StartGame(new StartGameArgs
        {
            SceneManager = sceneManager,
            HostMigrationToken = hostMigrationToken,
            HostMigrationResume = HostMigrationResume,
            ConnectionToken = GameManager.instance.GetConnectionToken()
        });
    }

  
    // This code is executed when we start up our NEW runner
    // We need to recreate all of our old network objects and their state (this could be objects, projectiles, players etc)
    // We're only interested in the networked players in this case
    void HostMigrationResume(NetworkRunner runner)
    {
        Debug.Log($"{Time.time}:  HostMigrationResume():  STARTED");

        // Get a reference for each Networked Object from the old host
        foreach (var resumeNetworkObject in runner.GetResumeSnapshotNetworkObjects())
        {
            // Grab all the PLAYER objects only, they each have a NetworkCharacterControllerPrototypeCustom so look for that
            if (resumeNetworkObject.TryGetBehaviour<NetworkCharacterControllerPrototypeCustom>(out var characterController))
            {
                // We found a PLAYER object, so now retrieve the old information (position, rotation) and SPAWN the newNetworkObject with this information
                runner.Spawn(resumeNetworkObject, position: characterController.ReadPosition(), rotation: characterController.ReadRotation(), onBeforeSpawned: (runner, newNetworkObject) =>
                {
                    newNetworkObject.CopyStateFrom(resumeNetworkObject);    // Get state from OLD networkObject and push it to the newNetworkObject
      
                    if (resumeNetworkObject.TryGetBehaviour<HPHandler>(out var oldHPHandler))   // Copy info state from old behaviour to the new behaviour (health (HP) etc)
                    {
                        HPHandler newHPHandler = newNetworkObject.GetComponent<HPHandler>();    // Restore previous HP values to the reconnected client
                        newHPHandler.CopyStateFrom(oldHPHandler);
                        newHPHandler.skipSettingStartValues = true;     // Tell the HPHandler.Start function that we DO NOT want to reset our HP values
                    }
      
                    if (resumeNetworkObject.TryGetBehaviour<NetworkPlayer>(out var oldNetworkPlayer))   // Map the connection token with the new Network Player     
                        FindObjectOfType<Spawner>().SetConnectionTokenMapping(oldNetworkPlayer.token, newNetworkObject.GetComponent<NetworkPlayer>());  // Store the player token for reconnection
                });
            }
        }

        StartCoroutine(CleanUpHostMigrationCoRoutine());    // Wait 10 seconds then call Spawner.OnHostMigrationCleanUp() which despawns dormant network players
        Debug.Log($"{Time.time}:  HostMigrationResume():  COMPLETED");
    }

    /// <summary>
    /// Waits for 10 seconds after migration has been completed and then calls Spawner.OnHostMigrationCleanUp() on any dormant clients and despawns them.
    /// </summary>
    IEnumerator CleanUpHostMigrationCoRoutine()
    {
        yield return new WaitForSeconds(10.0f);
        FindObjectOfType<Spawner>().OnHostMigrationCleanUp();
    }


    public void OnJoinLobby()
    {
        var clientTask = JoinLobby();
    }

    private async Task JoinLobby()
    {
        string lobbyID = "OurLobbyID";
        Debug.Log($"{Time.time}:  Join Lobby STARTED - Attempting to connect to Lobby: {lobbyID}");

        var result = await networkRunner.JoinSessionLobby(SessionLobby.Custom, lobbyID);        // Try join the lobby

        if (!result.Ok)
            Debug.LogError($"{Time.time}:  Unable to join lobby: {lobbyID}");
        else
            Debug.Log($"{Time.time}:  JoinLobby() OK: {lobbyID}");
    }

    public void CreateGame(string sessionName, string sceneName)
    {
        // Get the scene Build Index Number (it's pretty tricky so we do this)
        int sceneBuildIndexNumber = SceneUtility.GetBuildIndexByScenePath($"scenes/{sceneName}");

        Debug.Log($"{Time.time}:  Create Session: {sessionName} - Scene: {sceneName} - Build Index: {sceneBuildIndexNumber}");

        // Join existing game as a client
        var clientTask = InitializeNetworkRunner(networkRunner, 
                                                GameMode.Host, 
                                                sessionName, 
                                                GameManager.instance.GetConnectionToken(), 
                                                NetAddress.Any(), 
                                                sceneBuildIndexNumber, 
                                                null);
    }

    public void JoinGame(SessionInfo sessionInfo)
    {
        Debug.Log($"{Time.time}:  Joining Session: {sessionInfo.Name}");

        // Join existing game as a client
        var clientTask = InitializeNetworkRunner(networkRunner, 
                                                GameMode.Client, 
                                                sessionInfo.Name, 
                                                GameManager.instance.GetConnectionToken(), 
                                                NetAddress.Any(), 
                                                SceneManager.GetActiveScene().buildIndex,           // This doesn't matter really because we're connecting as client so it'll be overwritten by the host
                                                null);
    }
}
