using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AdjustCanvasScaler : MonoBehaviour {

    float height = 480;
    float width = 800;

	// Use this for initialization
    void Start()
    {
        if (Screen.width > width || Screen.height > height)
        {
            float multiplier = Screen.width / width;
            GetComponent<CanvasScaler>().referencePixelsPerUnit *= multiplier;
        }
	}

}
