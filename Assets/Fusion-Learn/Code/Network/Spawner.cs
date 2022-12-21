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

    // Mapping between Token ID and Re-Created Players
    Dictionary<int, NetworkPlayer> mapTokenIDWithNetworkPlayer;


    // Other components
    CharacterInputHandler characterInputHandler;
    SessionListUIHandler sessionListUIHandler;


    private void Awake()
    {
        mapTokenIDWithNetworkPlayer = new Dictionary<int, NetworkPlayer>();
        sessionListUIHandler = FindObjectOfType<SessionListUIHandler>(true);        // Include TRUE to find objects that ARE NOT currently enabled but do exist
    }

    // Helper Function
    int GetPlayerToken(NetworkRunner runner, PlayerRef player)
    {
        // If we are the local player (server) - get our token, hash and return it
        if (runner.LocalPlayer == player)
            return ConnectionTokenUtils.HashToken(GameManager.instance.GetConnectionToken());
        else   // We are a remote player - get our token from our PlayerRef
        {
            var token = runner.GetPlayerConnectionToken(player);

            // Check to make sure it worked and return our hashed token
            if (token != null)
                return ConnectionTokenUtils.HashToken(token);

            Debug.LogError($"{Time.time}:  GetPlayerToken returned an invalid token");
            return 0;   // Invalid Token

        }
    }


    // Sets the token mapping up for the player.  Important as if we are the HOST when we disconnect all token mapping will be lost otherwise
    // This is called in our NetworkRunnerHandler.HostMigrationResume() function
    public void SetConnectionTokenMapping(int token, NetworkPlayer networkPlayer)
    {
        mapTokenIDWithNetworkPlayer.Add(token, networkPlayer);
    }


    // Spawn players in the world
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // HOST PLAYER (server)  -  using Peer-To-Peer network model - this client is also hosting the server
        if (runner.IsServer)
        {
            // Get the token for the player
            int playerToken = GetPlayerToken(runner, player);
            Debug.Log($"{Time.time}:  OnPlayerJoined:  We are the SERVER.  Connection token:  {playerToken}");

            // PLAYER RECONNECTED:   Check if the token is already known to the server - which means we've reconnected from a dropped/migrated session
            // IMPORTANT NOTE TO SELF:  mapTokenIDWithNetworkPlayer is a dictionary which holds multiple parameters (int, NetworkPlayer) in the same variable (similar to a struct)
            if (mapTokenIDWithNetworkPlayer.TryGetValue(playerToken, out NetworkPlayer networkPlayer))
            {
                Debug.Log($"{Time.time}:  OnPlayedJoined:  Found old connection token for: {playerToken}.  Assigning controls back to reconnected player");
                networkPlayer.GetComponent<NetworkObject>().AssignInputAuthority(player);

                networkPlayer.Spawned();    // Re-run our spawned code again to configure the nickname, audiolistener, camera etc.
            }
            else
            // FIRST TIME (NEW) PLAYER CONNECTED
            {
                Debug.Log($"{Time.time}:  Spawning new player for connection token: {playerToken}");
                NetworkPlayer spawnedNetworkPlayer = runner.Spawn(playerprefab, Utils.GetRandomSpawnPoint(), Quaternion.identity, player);

                // WE ARE SERVER:   We can now store the token for the player  (only server can set networked variables)
                spawnedNetworkPlayer.token = playerToken;

                // Store the mapping between the playerToken and the spawned network player
                mapTokenIDWithNetworkPlayer[playerToken] = spawnedNetworkPlayer;
            }
        }
        // CLIENT PLAYER (networked player)
        else Debug.Log($"{Time.time}:  OnPlayerJoined() - PlayerID: {player.PlayerId}  Connection Token: {ConnectionTokenUtils.TokenToString(runner.GetPlayerConnectionToken())}");
    }

    // NETWORK MOVEMENT INPUT
    public void OnInput(NetworkRunner runner, NetworkInput input) 
    {
        // Handle our LOCAL players input (my client only)
        if (characterInputHandler == null && NetworkPlayer.Local != null)
            characterInputHandler = NetworkPlayer.Local.GetComponent<CharacterInputHandler>();

        // Confirm we have our input handler - passed from CharacterInputHandler.cs
        if (characterInputHandler != null)
            input.Set(characterInputHandler.GetNetworkInput());
    }

    public void OnConnectedToServer(NetworkRunner runner) { Debug.Log("OnConnectedToServer"); }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { Debug.Log("OnConnectFailed"); }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { Debug.Log("OnConnectRequest"); }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { Debug.Log("OnDisconnectedFromServer"); }
    public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) 
    { 
        Debug.Log($"{Time.time}:  OnHostMigration");

        // Shutdown our current runner
        await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

        // Find NetworkRunnerHandler + begin Host Migration
        FindObjectOfType<NetworkRunnerHandler>().StartHostMigration(hostMigrationToken);
    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) 
    {
        // Only update the list of sessions when the session list UI handler is active
        if (sessionListUIHandler == null)
            return;

        if (sessionList.Count == 0)
        {
            Debug.Log($"{Time.time}:  Joined Lobby - No Sessions Found");
            sessionListUIHandler.OnNoSessionsFound();
        }
        else
        {
            sessionListUIHandler.ClearList();

            foreach (SessionInfo sessionInfo in sessionList)
            {
                sessionListUIHandler.AddToList(sessionInfo);
                Debug.Log($"{Time.time}:  Found session: {sessionInfo.Name}   Player Count: {sessionInfo.PlayerCount}");
            }
        }
    }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { Debug.Log("OnShutdown"); }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnHostMigrationCleanUp()
    {
        Debug.Log($"{Time.time}:  Spawner.OnHostMigrationCleanUp:  STARTED");

        // Finf all the players that have not reconnected and despawn them
        foreach (KeyValuePair<int, NetworkPlayer> entry in mapTokenIDWithNetworkPlayer)
        {            
            NetworkObject networkObjectInDictionary = entry.Value.GetComponent<NetworkObject>();    // Load the network object of the dictionary entry that we're currently querying

            if (networkObjectInDictionary.InputAuthority.IsNone)    // If there is no one controlling this player, we've found a disconnected player that didnt reconnect
            {
                Debug.Log($"{Time.time}:  Spawner.OnHostMigrationCleanUp:  Found a player that has not reconnected.  Despawning {entry.Value.nickName}");
                networkObjectInDictionary.Runner.Despawn(networkObjectInDictionary);

                // IMPORTANT NOTE:  We should remove this entry from the Dictionary if we're going to do this properly to
                // prevent issues with reconnecting clients after they've been despawned like this but for quickness we're
                // not doing that right now .... this is something to do later if necessary!
            }
        }

        Debug.Log($"{Time.time}:  Spawner.OnHostMigrationCleanUp:  COMPLETED");
    }

}
