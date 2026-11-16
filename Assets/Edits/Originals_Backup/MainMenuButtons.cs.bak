using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuButtons : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] GameObject JoinLobbyScreen;
    [SerializeField] GameObject CreateLobbyScreen;
    [SerializeField] GameObject chooseUserNameScreen;

    [Header("Buttons")]
    [SerializeField] Button[] menuButtons; //  setting buttons as an array just to it is easier to set them all active and inactive when specific screens are open
                                           //  to prevent the player from pressing the buttons when other screens are open

    private void Awake()
    {
        JoinLobbyScreen.SetActive(false);
        CreateLobbyScreen.SetActive(false);
        chooseUserNameScreen.SetActive(false);
    }

    public void hostLobbyButton()
    {
        if (JoinLobbyScreen.activeInHierarchy) JoinLobbyScreen.SetActive(false); // guard statement in case the join lobby screen is active 
        chooseUserNameScreen.SetActive(true);
        foreach (var button in menuButtons)
        {
            button.gameObject.SetActive(false);
        }
    }

    public void joinLobbyButton()
    {
        if (CreateLobbyScreen.activeInHierarchy) CreateLobbyScreen.SetActive(false);
        JoinLobbyScreen.SetActive(true);
        foreach (var button in menuButtons)
        {
            button.gameObject.SetActive(false);
        }
    }

    public void joinLobbybackButton()
    {
        JoinLobbyScreen.SetActive(false);   
        foreach (var button in menuButtons)
        {
            button.gameObject.SetActive(true);
        }
    }
    public void joinLobbyCreateLobbyBackButton()
    {
        CreateLobbyScreen.SetActive(false);
        foreach (var button in menuButtons)
        {
            button.gameObject.SetActive(true);
        }
    }
    public void createUserNameContinueButton()
    {
        chooseUserNameScreen.SetActive(false);
        CreateLobbyScreen.SetActive(true);
    }
    public void OnApplicationQuit()
    {
        Application.Quit();
    }
}
