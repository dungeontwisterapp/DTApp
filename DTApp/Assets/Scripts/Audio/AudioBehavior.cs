using UnityEngine;
using System.Collections;

public class AudioBehavior : MonoBehaviour {

	public float waitTimeBeforeDestruct = 5.0f;

	// Use this for initialization
	void Start () {
		StartCoroutine(selfDestruct());
	}

	IEnumerator selfDestruct () {
		yield return new WaitForSeconds(waitTimeBeforeDestruct);
	}

}
