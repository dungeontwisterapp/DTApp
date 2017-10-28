using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InfoCardPanel : MonoBehaviour {
	
	public Sprite standardState;
	public Sprite usedState;

	// Use this for initialization
	void Start () {
		standardState = GetComponent<Image>().sprite;
	}

}
