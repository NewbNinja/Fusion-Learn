using UnityEngine;
using Fusion;
using TMPro;

public class NetworkPlayer : NetworkBehaviour, IPlayerLeft
{
    bool isPublicJoinMessageSent = false;


    // Cached Objects
    public TextMeshProUGUI playerNickNameTM;
    public Transform playerModel;
    public LocalCameraHandler localCameraHandler;
    public GameObject localUI;


    // Other Components
    NetworkInGameMessages networkInGameMessages;


    public static NetworkPlayer Local { get; set; }     // Save our local network player object in Local


    // NETWORK VARIABLES
    [Networked(OnChanged = nameof(OnNickNameChanged))]      // React to the name change on the network by listening for the call OnNickNameChanged
    public NetworkString<_16> nickName { get; set; }        // MAX SIZE of 16 characters


    // REMOTE Client Token Hash
    [Networked] public int token { get; set; }

    void Awake()
    {
        networkInGameMessages = GetComponent<NetworkInGameMessages>();
        //localCameraHandler = GetComponent<LocalCameraHandler>();
    }

    public override void Spawned()
    {
        // If we have input authority - this is our object
        // NOTE:  Without performing this check we would affect EVERY player object, not what we want
        if (Object.HasInputAuthority)
        {
            // WE ARE THE LOCAL PLAYER

            Local = this;

            // USAGE:   Prevents player model blocking view of  the camera on player prefab by changing the render layer of the local players model 
            // INFO:    https://www.youtube.com/watch?v=ndL1siRSBg8&t=30s
            // NOTES:   We've set the camera on the NetworkPlayerPF (prefab) to ignore layer:6 ("LocalPlayerModel")
            //          We do this so that the camera ignores the local players prefab so that is doesn't block our view
            Utils.SetRenderLayerInChildren(playerModel, LayerMask.NameToLayer("LocalPlayerModel"));

            // Disable Main Camera
            if (Camera.main != null)
                Camera.main.gameObject.SetActive(false);

            // Enable ONE audio listener
            AudioListener audioListener = GetComponentInChildren<AudioListener>(true);
            audioListener.enabled = true;


            localCameraHandler.localCamera.enabled = true;  // Enable the local camera
            localCameraHandler.transform.parent = null;     // Detach local camera from our model
            localUI.SetActive(true);                        // Enable UI for local player


            RPC_SetNickName(GameManager.instance.playerNickname);   // Get our chosen nickname

            Debug.Log($"{Time.time}:  NetworkPlayer.Spawned():   Spawned local player");
        }
        else
        {
            // WE ARE NOT THE LOCAL PLAYER

            localCameraHandler.localCamera.enabled = false;     // Disable the local camera        
            localUI.SetActive(false);                           // Disable UI for remote player

            // Disable all other audio listeners EXCEPT local players Audio Listener
            AudioListener audioListener = GetComponentInChildren<AudioListener>();
            audioListener.enabled = false;



            Debug.Log($"{Time.time}:  NetworkPlayer.Spawned():   Spawned remote player");
        }

        // Set the Network Object associated with this player
        Runner.SetPlayerObject(Object.InputAuthority, Object);

        // Make it easier to tell which player is which
        transform.name = $"P_{Object.Id}";
    }

    // If player leaves, if we have input authority then despawn the player object
    // If were the server, send the message to clients that player left
    public void PlayerLeft(PlayerRef player)
    {
        if (Object.HasStateAuthority)
        {
            // Can we get the players Network Object?
            if (Runner.TryGetPlayerObject(player, out NetworkObject playerLeftNetworkObject))
            {
                // If the player who left is OUR PLAYER (if we want to leave)
                if (playerLeftNetworkObject == Object)
                    // Ask the LOCAL player (the host)s to send the RPC for us since we'll
                    // probably not process it in time before our game exits when we leave
                    Local.GetComponent<NetworkInGameMessages>().SendInGameRPCMessage(playerLeftNetworkObject.GetComponent<NetworkPlayer>().nickName.ToString(), "left");
            }
        }

        if (player == Object.InputAuthority)
            Runner.Despawn(Object);
    }

    // IMPORTANT:  Static function cannot access anything that is not static
    // Network Function MUST BE STATIC so this is the one called by the network event  -  [Networked(OnChanged = nameof(OnNickNameChanged))]
    // FLOW:  Network detects nickname change -> which calls:  static void OnNickNameChanged -> which calls:   private void OnNickNameChanged()
    static void OnNickNameChanged(Changed<NetworkPlayer> changed)
    {
        Debug.Log($"{Time.time}  OnNickNameChanged value {changed.Behaviour.nickName}");
        changed.Behaviour.OnNickNameChanged();
    }
    private void OnNickNameChanged()
    {
        Debug.Log($"Nickname changed for player to {nickName} for player {gameObject.name}");
        playerNickNameTM.text = nickName.ToString();
    }


    // REMOTE PROCEDURE CALL -- Must be a Network Behaviour
    // RPC sends a message to the server to do something, Server then sends that information out to other clients
    // RECEIVER RPC
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickName(string nickName, RpcInfo info = default)
    {
        Debug.Log($"{Time.time}:  [RPC] SetNickName:  {nickName}");
        this.nickName = nickName;

        // Send our joined message only once on connect
        // This will be excuted by the server as the target for this function SetNickName is RpcTargets.StateAuthority == SERVER
        if (!isPublicJoinMessageSent)
        {
            networkInGameMessages.SendInGameRPCMessage(nickName, "joined");
            isPublicJoinMessageSent = true;
        }
    }


    void OnDestroy()
    {
        // If we're destroyed (during host migration / reconnection),
        // remove the camera as we will be assigned a new one with the new network player
        if (localCameraHandler != null)
            Destroy(localCameraHandler.gameObject);
    }
}
