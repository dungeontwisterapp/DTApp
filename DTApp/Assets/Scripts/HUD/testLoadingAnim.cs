using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class testLoadingAnim : MonoBehaviour {

    AppManager app;
    Image image;

    bool rolling = false;
    public float slowness = 0.11f;

	// Use this for initialization
	void Start () {
        app = AppManager.appManager;
        image = GetComponent<Image>();
	}
	
	// Update is called once per frame
	void Update () {
        if (app.onlineWaiting)
        {
            if (!rolling)
            {
                rolling = true;
                image.enabled = true;
                StartCoroutine(rotationAnimation());
            }
        }
        else
        {
            rolling = false;
            image.enabled = false;
        }
	}

    IEnumerator rotationAnimation()
    {
        yield return new WaitForSeconds(slowness);
        transform.Rotate(transform.forward * -45);
        if (rolling) StartCoroutine(rotationAnimation());
    }
}
