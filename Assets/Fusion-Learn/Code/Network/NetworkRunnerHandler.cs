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




    private void Start()
    {
        // Create our network runner
        networkRunner = Instantiate(networkRunnerPrefab);
        networkRunner.name = "Network Runner";

        // Initialize the Network Runner - AutoHost (if no host, first client connected willbe host)
        var clientTask = InitializeNetworkRunner(networkRunner, GameMode.AutoHostOrClient, NetAddress.Any(), SceneManager.GetActiveScene().buildIndex, null);
        Debug.Log($"Server NetworkRunner started.");
    }

    // Check if there are any unity objects we need to consider (objects with colliders on them)
    protected virtual Task InitializeNetworkRunner(NetworkRunner runner, GameMode gameMode, NetAddress address, SceneRef scene, Action<NetworkRunner> initialized)
    {
        // Get the scene manager component - or create one
        var sceneManager = runner.GetComponents(typeof(MonoBehaviour)).OfType<INetworkSceneManager>().FirstOrDefault();
        if (sceneManager == null)
            sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

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
            SessionName = "TestRoom",
            Initialized = initialized,
            SceneManager = sceneManager
        });
    }
}
