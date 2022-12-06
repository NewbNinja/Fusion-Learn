using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // SINGLETON:   Static instance so other scripts can access it  (GameManager.instance)
    public static GameManager instance = null;

    byte[] connectionToken;     // The unique connection token for Network Players (used for host migration / when a player rejoins)

    public Vector2 cameraViewRotation = Vector2.zero;   // TODO:  Make getters/setters for this and change to private
    public string playerNickname = "";


    private void Awake()
    {
        // SINGLETON SETUP:  If doesn't exist, create it, else return reference to the current instance
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Check if token is valid, if not, get a new one
        if (connectionToken == null)
        {
            connectionToken = ConnectionTokenUtils.NewToken();
            Debug.Log($"{Time.time}:  Player Connection Token:  {ConnectionTokenUtils.HashToken(connectionToken)}");
        }
    }

    public void SetConnectionToken(byte[] connectionToken)
    {
        this.connectionToken = connectionToken;
    }

    public byte[] GetConnectionToken()
    {
        return connectionToken;
    }


}
