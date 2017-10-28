using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class ParticleBehavior : MonoBehaviour {

	public string layer = "HUD";
	public int order = 5;

	ParticleRenderer pRenderer;

	// Use this for initialization
	void Start () {
		pRenderer = GetComponent<ParticleRenderer>();
		pRenderer.sortingLayerName = layer;
		pRenderer.sortingOrder = order;
	}

	IEnumerator endEmission () {
		GetComponent<ParticleEmitter>().emit = false;
		yield return new WaitForSeconds(1);
		Destroy(gameObject);
	}

}
