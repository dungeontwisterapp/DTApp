using UnityEngine;
using System.Collections;

public class ScreenShake : MonoBehaviour {

    Vector3 originalScreenPosition;
    float shakeAmount = 2.0f;
    float screenSize;

	// Use this for initialization
    void Start()
    {
        originalScreenPosition = transform.position;
        screenSize = Screen.height / 600.0f;
	}

    public void launchShake(float tempshakeAmount)
    {
        shakeAmount = tempshakeAmount;
        launchShake();
    }

    public void launchShake()
    {
        InvokeRepeating("shakeScreen", 0, .01f);
        Invoke("stopShaking", 0.3f);
    }

    void shakeScreen()
    {
        if (shakeAmount > 0)
        {
            float quakeAmtX = (UnityEngine.Random.value * shakeAmount * 2 - shakeAmount) * screenSize;
            float quakeAmtY = (UnityEngine.Random.value * shakeAmount * 2 - shakeAmount) * screenSize;
            Vector3 pp = transform.position;
            pp.x += quakeAmtX;
            pp.y += quakeAmtY;
            transform.position = pp;
        }
    }

    void stopShaking()
    {
        CancelInvoke("shakeScreen");
        transform.position = originalScreenPosition;
        shakeAmount = 2.0f;
    }
}
