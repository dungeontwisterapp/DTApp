using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CombatUI : MonoBehaviour {

    [HideInInspector]
    public Text leftTotal, rightTotal;
    public ChooseCombatCardScreen chooseCombatCardLayout;
    [HideInInspector]
    public CombatBackground combatBackground;

	// Use this for initialization
	void Start () {
        combatBackground = transform.Find("Background").GetComponent<CombatBackground>();
        leftTotal = transform.Find("Background/VS/Left/Image/Text").GetComponent<Text>();
        rightTotal = transform.Find("Background/VS/Right/Image/Text").GetComponent<Text>();

        float currentRatio = (float)Screen.width / (float)Screen.height;
        float expectedRatio = 16.0f / 9.0f;
        if (currentRatio < expectedRatio)
        {
            float multiplier = expectedRatio / currentRatio;
            transform.Find("LeftSide").localScale = new Vector3(2 - multiplier, 2 - multiplier, 1);
            transform.Find("RightSide").localScale = new Vector3(2 - multiplier, 2 - multiplier, 1);
        }

        Invoke("DisableAtStart", 0.5f);
        /*
        if (!AppManager.appManager.standardRatio)
        {
            transform.Find("LeftSide").localScale = new Vector3(0.5f, 0.5f, 1);
            transform.Find("RightSide").localScale = new Vector3(0.5f, 0.5f, 1);
        }
        */
	}

    void DisableAtStart()
    {
        gameObject.SetActive(false);
	}
}
