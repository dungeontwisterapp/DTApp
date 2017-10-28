using UnityEngine;
using System.Collections;

public class SpritePulseFX : MonoBehaviour {

	// Use this for initialization
	void Start () {
        iTween.FadeTo(gameObject, iTween.Hash("alpha", 0, "time", 1f, "easetype", iTween.EaseType.easeInOutQuad, "looptype", iTween.LoopType.pingPong));
	}

}
