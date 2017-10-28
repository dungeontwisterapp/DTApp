using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class ActionCards : MonoBehaviour, IPointerDownHandler {

    public AudioClip[] cardAppearSound;
	// Nombre de points d'action donnés par la carte
	public int actionPointsValue = 2;
	bool cardChosen = false;
	// Valeur du cannal alpha du Game Object
	float timer, alpha = 0;
    Vector3 startPosition, finalPosition;

	// Carte accessible ou non en fonction de sa valeur en Points d'Action et de si elle a déjà été utilisée
	bool disponible = false;
	GameManager gManager;

	// Use this for initialization
    void Start()
    {
        gManager = GameManager.gManager;
        finalPosition = transform.position;
        startPosition = new Vector3(transform.position.x, -actionPointsValue * Screen.height / 4.0f, transform.position.z);
        transform.position = startPosition;
	}

    // Lance l'animation de Fade In de la carte
    public void launchCardInAnimation()
    {
        transform.position = startPosition;
        if (!gManager.activePlayer.usedActionCards[actionPointsValue-2])
        {
            gManager.playSound(cardAppearSound[UnityEngine.Random.Range(0, cardAppearSound.Length)]); 
            alpha = 1;
            float gradient = 1;
            if (actionPointsValue > gManager.valeurMaxCarteAction) gradient = 0.5f;

            //renderer.material.color = new Color(gradient, gradient, gradient, alpha);
            GetComponent<Image>().color = new Color(gradient, gradient, gradient, alpha);
            for (int i = 0; i < transform.childCount; i++)
            {
                //transform.GetChild(i).renderer.material.color = new Color(gradient, gradient, gradient, alpha);
                transform.GetChild(i).GetComponent<Image>().color = new Color(gradient, gradient, gradient, alpha);
            }
            timer = Time.time;
            StartCoroutine(cardInAnimation(0.4f));
        }
    }

    IEnumerator cardInAnimation(float animDuration)
    {
        float valueProgression = (Time.time - timer) / animDuration;
        transform.position = Vector3.Lerp(startPosition, finalPosition, valueProgression);
        yield return new WaitForSeconds(0.01f);
        if (Time.time - timer <= animDuration) StartCoroutine(cardInAnimation(animDuration));
        else
        {
            transform.position = finalPosition;
            checkCardAvailability();
        }
    }

    void checkCardAvailability()
    {
        if (gManager.playerInteractionAvailable())
        {
            if (actionPointsValue <= gManager.valeurMaxCarteAction) disponible = true;
        }
        else selectCard();
    }

    float getAppropriateGradient()
    {
        float gradient = 1;
        if (actionPointsValue > gManager.valeurMaxCarteAction) gradient = 0.5f;
        return gradient;
    }

	public void launchFadeIn() {
        if (alpha == 0 && !gManager.activePlayer.usedActionCards[actionPointsValue - 2]) StartCoroutine(fadeIn(0.02f, getAppropriateGradient()));
	}

	// Animation d'apparition de la carte
	IEnumerator fadeIn (float intervals, float gradient) {
		alpha += 0.1f;
        //renderer.material.color = new Color(gradient, gradient, gradient, alpha);
        GetComponent<Image>().color = new Color(gradient, gradient, gradient, alpha);
		for (int i=0 ; i < transform.childCount ; i++) {
            //transform.GetChild(i).renderer.material.color = new Color(gradient, gradient, gradient, alpha);
            transform.GetChild(i).GetComponent<Image>().color = new Color(gradient, gradient, gradient, alpha);
		}
		yield return new WaitForSeconds(intervals);
        if (alpha < 1) StartCoroutine(fadeIn(intervals, gradient));
		else  if (actionPointsValue <= gManager.valeurMaxCarteAction) disponible = true;
	}
	
	// Réaction à un clic / appui sur la carte
    public void OnPointerDown(PointerEventData eventData)
    {
        if (disponible)
        {
            disponible = false;
            selectCard();
		}
	}

    public void selectCard()
    {
        transform.parent.GetComponent<HideActionCards>().deactivateActionCards();
        gManager.actionCardChosen(actionPointsValue);
        cardChosen = true;

        // Mettre un effet visuel sympa de sélection
        transform.parent.BroadcastMessage("launchFadeOut");
        StartCoroutine(cardSelectedAnimation());
    }

	/*
	// Quand on passe la souris au-dessus de la carte, celle-ci change de couleur si elle est disponible
	void OnMouseOver() {
		if (disponible) renderer.material.color = new Color(1f, 0.9f, 0.4f);
	}
	
	// Après avoir passé la souris au-dessus de la carte, celle-ci reprend sa couleur normale
	void OnMouseExit() {
		if (disponible) renderer.material.color = Color.white;
	}
	*/

    IEnumerator cardSelectedAnimation()
    {
        float refScale = transform.localScale.x;
        transform.localScale = new Vector3(refScale * 1.01f, refScale * 1.01f, 1);
        yield return new WaitForSeconds(0.01f);
        if (transform.localScale.x < 1.2f) StartCoroutine(cardSelectedAnimation());
        else StartCoroutine(removeCards(0.6f));
    }

	void launchFadeOut () {
		if (alpha >= 1) StartCoroutine(launchFadeOut(0));
	}

	// Fonction exécutée par toutes les cartes une fois que l'une d'entre elles a été sélectionnée
	IEnumerator launchFadeOut (float wait) {
		// Traitement pour toutes les cartes sauf celle sélectionnée
		if (!cardChosen) {
			disponible = false;
			yield return new WaitForSeconds(wait);
            StartCoroutine(fadeOut(0.02f, getAppropriateGradient()));
		}
	}
	
	// Animation de disparition de la carte
    IEnumerator fadeOut(float intervals, float gradient)
    {
		alpha -= 0.1f;
        //renderer.material.color = new Color(gradient, gradient, gradient, alpha);
        GetComponent<Image>().color = new Color(gradient, gradient, gradient, alpha);
		for (int i=0 ; i < transform.childCount ; i++) {
            //transform.GetChild(i).renderer.material.color = new Color(gradient, gradient, gradient, alpha);
            transform.GetChild(i).GetComponent<Image>().color = new Color(gradient, gradient, gradient, alpha);
		}
		yield return new WaitForSeconds(intervals);
        if (alpha > 0) StartCoroutine(fadeOut(intervals, gradient));
		else alpha = 0;
	}

	// Détruire les Game Object des cartes
	IEnumerator removeCards (float wait) {
		yield return new WaitForSeconds(wait);
        StartCoroutine(fadeOut(0.02f, getAppropriateGradient()));
		yield return new WaitForSeconds(0.3f);
        transform.localScale = new Vector3(1, 1, 1);
		gManager.actionCardsButton.SetActive(false);
        cardChosen = false;
        transform.parent.gameObject.SetActive(false);
		//Destroy(transform.parent.gameObject);
	}

}
