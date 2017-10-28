using UnityEngine;
using System.Collections;

public class TempMusic : MonoBehaviour {

	public Texture2D icon_on;
	public Texture2D icon_off;
	public bool musicOn = false;

	// Use this for initialization
	void Start () {
		
	}

	void OnMouseDown () {
		if (musicOn) {
			GetComponent<Renderer>().material.mainTexture = icon_off;
			GetComponent<AudioSource>().Stop();
			musicOn = false;
		}
		else {
			GetComponent<Renderer>().material.mainTexture = icon_on;
			GetComponent<AudioSource>().Play();
			musicOn = true;
		}
	}
}
