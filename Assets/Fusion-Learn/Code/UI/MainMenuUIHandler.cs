using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;

public class MainMenuUIHandler : MonoBehaviour
{

    public TMP_InputField inputField;

    // Start is called before the first frame update
    void Start()
    {
        // If we already have a nickname in player prefs then just set it
        if (PlayerPrefs.HasKey("PlayerNickname"))
            inputField.text = PlayerPrefs.GetString("PlayerNickname");
    }

    public void OnJoinGameClicked()
    {
        PlayerPrefs.SetString("PlayerNickname", inputField.text);
        PlayerPrefs.Save();

        GameManager.instance.playerNickname = inputField.text;

        SceneManager.LoadScene("Game");
    }
}
