using UnityEngine;
using System.Collections;

public class FireballAnimation : MonoBehaviour {

    public GameObject explosionFX;
    public Transform target;
    float animDuration = 1.2f;
    float rotationDurationCoeff = 0.85f;

    public iTween.EaseType rotateIn = iTween.EaseType.easeInSine;
    public iTween.EaseType rotateOut = iTween.EaseType.easeOutSine;

    Vector3 direction = Vector3.zero;

    void Start()
    {
        EllipsoidParticleEmitter[] particles = GetComponentsInChildren<EllipsoidParticleEmitter>();
        foreach (EllipsoidParticleEmitter ePE in particles)
        {
            ePE.GetComponent<Renderer>().sortingLayerName = "HUD";
        }
    }

    public void fire()
    {
        if (Mathf.Approximately(transform.position.x, target.position.x))
        {
            if (transform.position.y < target.position.y) direction = Vector3.left;
            else direction = Vector3.right;
        }
        else if (Mathf.Approximately(transform.position.y, target.position.y))
        {
            if (transform.position.x < target.position.x) direction = Vector3.forward;
            else direction = Vector3.back;
        }
        iTween.MoveTo(gameObject, iTween.Hash("position", target.position, "time", animDuration, "easetype", iTween.EaseType.easeInOutQuint, "oncomplete", "explode"));
        iTween.RotateAdd(gameObject, iTween.Hash("amount", direction * 50, "time", animDuration * rotationDurationCoeff, "easetype", rotateIn, "oncomplete", "endProjectileCourse"));
    }

    void endProjectileCourse()
    {
        iTween.RotateAdd(gameObject, iTween.Hash("amount", -direction * 50, "time", animDuration * (1 - rotationDurationCoeff), "easetype", rotateOut));
    }

    void explode()
    {
        GameObject explosion = (GameObject)Instantiate(explosionFX, target.position, explosionFX.transform.rotation);
        EllipsoidParticleEmitter[] particles = explosion.GetComponentsInChildren<EllipsoidParticleEmitter>();
        foreach (EllipsoidParticleEmitter ePE in particles)
        {
            ePE.GetComponent<Renderer>().sortingLayerName = "HUD";
        }
    }

    void removeFX()
    {
        EllipsoidParticleEmitter[] particles = GetComponentsInChildren<EllipsoidParticleEmitter>();
        foreach (EllipsoidParticleEmitter ePE in particles)
        {
            ePE.emit = false;
        }
        Invoke("autodestruct", 3);
    }

    void autodestruct()
    {
        Destroy(gameObject);
    }
}
