using UnityEngine;
using UnityEngine.UI;

public class continueButtonUserName : MonoBehaviour
{
    [SerializeField] Button continueButton;
    [SerializeField] MainMenuButtons mainMenuScreen;


    private void Awake()
    {
        continueButton.onClick.AddListener(() =>
        {
            mainMenuScreen.createUserNameContinueButton();
        });
    }
}