using UnityEngine;
using System.Collections;

public class DisplayFX : MonoBehaviour {

	// Use this for initialization
    void Start()
    {
        if (GetComponent<EllipsoidParticleEmitter>() != null) GetComponent<EllipsoidParticleEmitter>().GetComponent<Renderer>().sortingLayerName = "TokensOnBoard";
        if (GetComponent<ParticleSystem>() != null) GetComponent<ParticleSystem>().GetComponent<Renderer>().sortingLayerName = "TokensOnBoard";
	}

}
