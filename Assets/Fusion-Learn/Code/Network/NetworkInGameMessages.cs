using UnityEngine;
using Fusion;

public class NetworkInGameMessages : NetworkBehaviour
{
    InGameMessagesUIHandler inGameMessagesUIHandler;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SendInGameRPCMessage (string userNickName, string message)
    {
        RPC_InGameMessage($"<b>{userNickName}</b>:  {message}");
    }

    // REMOTE PROCEDURE CALL -- Must be a Network Behaviour
    // SENDER RPC - Source = SERVER, Targets = ALL
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_InGameMessage(string message, RpcInfo info = default)
    {
        Debug.Log($"{Time.time}:  [RPC] InGameMessage:  {message}");

        // Get OUR local inGameMessagesUIHandler from the network
        if (inGameMessagesUIHandler == null)
            inGameMessagesUIHandler = NetworkPlayer.Local.localCameraHandler.GetComponentInChildren<InGameMessagesUIHandler>();

        if (inGameMessagesUIHandler != null)
            inGameMessagesUIHandler.OnGameMessagesReceived(message);
    }

}
