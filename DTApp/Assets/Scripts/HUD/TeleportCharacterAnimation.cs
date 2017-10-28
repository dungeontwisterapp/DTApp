using UnityEngine;
using System.Collections;

public class TeleportCharacterAnimation : MonoBehaviour {

    EllipsoidParticleEmitter ePE;
    public float animIntroDuration = 0.4f;
    public float waitDuringFull = 1;
    float stdFXSize;

	// Use this for initialization
    void Start()
    {
        ePE = GetComponent<EllipsoidParticleEmitter>();
        ePE.GetComponent<Renderer>().sortingLayerName = "TokensOnBoard";
        stdFXSize = ePE.minSize;
        activateTeleportAnimation();
	}

    void activateTeleportAnimation()
    {
        GetComponent<ParticleRenderer>().material.SetColor("_TintColor", GameManager.gManager.activePlayer.playerColor);
        ePE.emit = true;
        StartCoroutine(teleportAnimation(Time.time, animIntroDuration, stdFXSize / 10.0f, stdFXSize));
        Invoke("endTeleportAnimation", animIntroDuration + waitDuringFull);
	}

    IEnumerator teleportAnimation(float startTime, float duration, float fromSize, float toSize)
    {
        float valueProgression = (Time.time - startTime) / duration;
        ePE.minSize = Mathf.Lerp(fromSize, toSize, valueProgression);
        ePE.maxSize = Mathf.Lerp(fromSize, toSize, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - startTime < duration) StartCoroutine(teleportAnimation(startTime, duration, fromSize, toSize));
        else
        {
            ePE.minSize = toSize;
            ePE.maxSize = toSize;
        }
    }

    void endTeleportAnimation()
    {
        StartCoroutine(teleportAnimation(Time.time, animIntroDuration * 2, stdFXSize, stdFXSize / 10.0f));
        Invoke("hideFX", animIntroDuration * 2f);
    }

    void hideFX()
    {
        ePE.emit = false;
        Invoke("destroyFX", 1);
    }

    void destroyFX()
    {
        Destroy(gameObject);
    }
}
