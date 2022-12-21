using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using TMPro;

public class SessionListUIHandler : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public GameObject sessionItemListPrefab;
    public VerticalLayoutGroup verticalLayoutGroup;

    private void Awake()
    {
        ClearList();
    }

    public void ClearList()
    {
        // Delete ALL children of the vertical layout group
        foreach (Transform child in verticalLayoutGroup.transform)
        {
            Destroy(child.gameObject);
        }

        // Hide the status message
        statusText.gameObject.SetActive(false);
    }


    public void AddToList(SessionInfo sessionInfo)
    {
        // Add a NEW entry to the list
        SessionInfoListUIItem addedSessionInfoListUIItem = Instantiate(sessionItemListPrefab, verticalLayoutGroup.transform).GetComponent<SessionInfoListUIItem>();

        // Set info and hook up the event
        addedSessionInfoListUIItem.SetInformation(sessionInfo);
        addedSessionInfoListUIItem.OnJoinSession += AddedSessionInfoListUIItem_OnJoinSession;
    }

    private void AddedSessionInfoListUIItem_OnJoinSession(SessionInfo sessionInfo)
    {
        NetworkRunnerHandler networkRunnerHandler = FindObjectOfType<NetworkRunnerHandler>();
        networkRunnerHandler.JoinGame(sessionInfo);

        MainMenuUIHandler mainMenuUIHandler = FindObjectOfType<MainMenuUIHandler>();
        mainMenuUIHandler.OnJoiningServer();
    }

    public void OnNoSessionsFound()
    {
        ClearList();
        statusText.text = "No game session found";
        statusText.gameObject.SetActive(true);
    }

    public void OnLookingForGameSessions()
    {
        ClearList();
        statusText.text = "Looking for game sessions";
        statusText.gameObject.SetActive(true);
    }
}
