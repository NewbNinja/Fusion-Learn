using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;

public class MainMenuUIHandler : MonoBehaviour
{
    [Header("Panels")]
    public GameObject playerDetailsPanel;
    public GameObject sessionBrowserPanel;
    public GameObject createSessionPanel;
    public GameObject statusPanel;

    [Header("Player Settings")]
    public TMP_InputField playerNameInputField;

    [Header("Player Settings")]
    public TMP_InputField sessionNameInputField;

    // Start is called before the first frame update
    void Start()
    {
        // If we already have a nickname in player prefs then just set it
        if (PlayerPrefs.HasKey("PlayerNickname"))
            playerNameInputField.text = PlayerPrefs.GetString("PlayerNickname");
    }

    public void OnFindGameClicked()
    {
        PlayerPrefs.SetString("PlayerNickname", playerNameInputField.text);
        PlayerPrefs.Save();
        GameManager.instance.playerNickname = playerNameInputField.text;

        NetworkRunnerHandler networkRunnerHandler = FindObjectOfType<NetworkRunnerHandler>();
        networkRunnerHandler.OnJoinLobby();

        HideAllPanels();
        sessionBrowserPanel.gameObject.SetActive(true);

        // DO THIS AFTER sessionBrowserPanel.gameObject.SetActive(true);  as ClearList() is called upon AWAKE() which will remove the results
        // When we're showing session browser panel, update the information - set (true) to show info as SessionListUIHandler will be inactive and this is required to query inactive objects
        FindObjectOfType<SessionListUIHandler>(true).OnLookingForGameSessions();

    }

    public void OnCreateNewGameClicked()
    {
        HideAllPanels();
        createSessionPanel.gameObject.SetActive(true);
    }

    public void OnStartNewSessionClicked()
    {
        NetworkRunnerHandler networkRunnerHandler = FindObjectOfType<NetworkRunnerHandler>();
        networkRunnerHandler.CreateGame(sessionNameInputField.text, "Game");

        HideAllPanels();
        statusPanel.gameObject.SetActive(true);
    }

    public void OnJoiningServer()
    {
        HideAllPanels();
        statusPanel.gameObject.SetActive(true);
    }
    public void HideAllPanels()
    {
        playerDetailsPanel.SetActive(false);
        sessionBrowserPanel.SetActive(false);
        statusPanel.SetActive(false);
        createSessionPanel.SetActive(false);
    }
}
