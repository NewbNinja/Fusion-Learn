using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;

public class Spawner : MonoBehaviour, INetworkRunnerCallbacks
{
    // We're gonna need a network player so create an instance for use later
    public NetworkPlayer playerprefab;

    // Add component
    CharacterInputHandler characterInputHandler;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Spawn players in the world
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) 
    { 
        if(runner.IsServer)
        {
            Debug.Log("OnPlayerJoined we are server.  Spawning player");
            runner.Spawn(playerprefab, Utils.GetRandomSpawnPoint(), Quaternion.identity, player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) 
    {
        // Check to make sure we're getting the correct character control from the right place
        // Get our Local players input handler
        if (characterInputHandler == null && NetworkPlayer.Local != null)
            characterInputHandler = NetworkPlayer.Local.GetComponent<CharacterInputHandler>();

        // Quick check to make sure we received the handler - the set our input handler - allows us to move our character
        if (characterInputHandler != null)
            input.Set(characterInputHandler.GetNetworkInput());
    }

    public void OnConnectedToServer(NetworkRunner runner) { Debug.Log("OnConnectedToServer"); }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { Debug.Log("OnConnectFailed"); }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { Debug.Log("OnConnectRequest"); }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { Debug.Log("OnDisconnectedFromServer"); }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { Debug.Log("OnShutdown"); }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

}
