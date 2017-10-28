using UnityEngine;
using Multi;

// Controller Network related Menu Calls
public class NetworkManager : MonoBehaviour
{
    public GameObject buttonAction1 = null;
    public GameObject buttonAction2 = null;

    public void Awake()
    {
        Debug.Log("NetworkManager: Awake");
        Http.Instance.Lobby.RegisterActionButtons(buttonAction1, buttonAction2);
    }

    public void OnEnable()
    {
        Debug.Log("NetworkManager: OnEnable");
    }

    public void OnDisable()
    {
        Debug.Log("NetworkManager: OnDisable");
    }

    public void OnDestroy()
    {
        Debug.Log("NetworkManager: OnDestroy");
        Http.Instance.Lobby.UnRegisterActionButtons();
    }

    public void OpenMenu(string name)
    {
        Debug.Log("NetworkManager: Open " + name);
        switch (name)
        {
            case "Online":
                Http.Instance.ChangeMode(Http.Mode.ONLINE);
                break;
            case "Lobby":
                Http.Instance.ChangeMode(Http.Mode.LOBBY);
                break;
            case "Log In":
                Http.Instance.ChangeMode(Http.Mode.LOGIN);
                break;
            case "Sign In":
                Http.Instance.ChangeMode(Http.Mode.SIGNIN);
                break;
            default:
                Debug.LogError("Unknown online menu :" + name);
                break;
        }
    }

    public void CloseMenu(string name)
    {
        Debug.Log("NetworkManager: Close " + name);
        switch (name)
        {
            case "Online":
                Http.Instance.ChangeMode(Http.Mode.OFFLINE);
                break;
            case "Lobby":
                break;
            case "LogIn":
                break;
            case "SignIn":
                break;
            default:
                Debug.LogError("Unknown online menu :" + name);
                break;
        }
    }

    public void Refresh() { Http.Instance.LoadTables(); }
    public void SignIn() { Http.Instance.SignIn(); }
    public void LogIn() { Http.Instance.LogIn(); }
    public void LogOut() { Http.Instance.LogOut(); }
    public void Action1() { Http.Instance.Lobby.Action(0); }
    public void Action2() { Http.Instance.Lobby.Action(1); }

    public void SetUsername(string value) { Http.Instance.SetUsername(value); }
    public void SetPassword(string value) { Http.Instance.SetPassword(value); }
    public void SetEmail(string value) { Http.Instance.SetEmail(value); }
    public void SetRememberMe(bool value) { Http.Instance.SetRememberMe(value); }
}
