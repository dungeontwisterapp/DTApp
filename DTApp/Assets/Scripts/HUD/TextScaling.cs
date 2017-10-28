using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TextScaling : MonoBehaviour {

    float originalHeight = 480.0f;
    //float originalWidth = 800.0f;

	// Use this for initialization
    void Start()
    {
        //float widthCoeff = (float)Screen.width / originalWidth;
        float heightCoeff = 0.75f * (float)Screen.height / originalHeight;
        if (GetComponent<Text>() != null) GetComponent<Text>().fontSize = Mathf.RoundToInt((float)GetComponent<Text>().fontSize * heightCoeff);
	}

}
