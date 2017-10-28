using UnityEngine;
using System.Collections;

public class PlayerVSBehavior : MonoBehaviour {

    // Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update ()
    {
    }

    public void SetActiveIfBoardReady(bool enable)
    {
        AppManager app = GameObject.Find("Application Manager").GetComponent<AppManager>();

        if (!app.onlineGameInterface.blockUserInteractions())
        {
            gameObject.SetActive(enable);
        }
    }
}
