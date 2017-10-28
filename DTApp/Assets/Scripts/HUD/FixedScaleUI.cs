using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FixedScaleUI : MonoBehaviour {

    float originalHeight = 480.0f;
    float originalWidth = 800.0f;
    public float empiricalAdjustment = 1.1f;

	// Use this for initialization
	void Start () {
        Rect r = GetComponent<RectTransform>().rect;
        float currentRatio = (float)Screen.width / (float)Screen.height;
        float expectedRatio = 16.0f / 9.0f;
        if (currentRatio < expectedRatio)
        {
            float multiplier = expectedRatio / currentRatio;
            multiplier *= empiricalAdjustment;
            GetComponent<RectTransform>().sizeDelta = new Vector2(r.width * multiplier, r.height * multiplier);
        }
        else
        {
            float widthCoeff = (float)Screen.width / originalWidth;
            float heightCoeff = (float)Screen.height / originalHeight;
            GetComponent<RectTransform>().sizeDelta = new Vector2(r.width * widthCoeff, r.height * heightCoeff);
            if (GetComponent<Text>() != null) GetComponent<Text>().fontSize = Mathf.RoundToInt((float)GetComponent<Text>().fontSize * heightCoeff);
        }
	}


}
