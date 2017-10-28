using UnityEngine;
using System.Collections;

public class FireballTargetAnimation : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {
        float maxScale = transform.localScale.x * 2;
        iTween.ScaleTo(gameObject, iTween.Hash("scale", new Vector3(maxScale, maxScale, 1), "looptype", iTween.LoopType.pingPong, "easetype", iTween.EaseType.easeInQuad));
    }


}
