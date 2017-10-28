using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

using IA;

public class TileBehaviorIHM : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
	
	public TileBehavior associatedTile;
	GameManager gManager;
	public GameObject tileHighlight;
    GameObject highlight;
    Transform rotationArrow;
    public AudioClip engagingRotation;
    public AudioClip disengagingRotation;
    public AudioClip rotationGoingLeft;
    public AudioClip rotationGoingRight;
	
	Quaternion originalDirection, temporaryChoosedDirection;
    bool interacting = false;
    [HideInInspector]
    public bool rotationPhase = false;

	float stdScaleValue;
	float coeffRotatingScaleValue = 1.2f;
	float newScaleValue;

	float selectionTimer;
	float SIMPLE_CLICK_DURATION = 0.1f;
	Vector3 lastPosition;

    [HideInInspector]
    public bool scalingDown = false;
	float scaleChangeStartTime;
	float SCALE_UP_DURATION = 1.0f;
    float SCALE_DOWN_DURATION = 0.6f;

    float angleArrow = 0;
    [HideInInspector]
    public bool inverseRotationDirection = false;

	void Awake () {
		gManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameManager>();
		associatedTile = GetComponent<TileBehavior>();
		if (associatedTile == null) {
			Debug.LogError("TileBehaviorIHM, Awake: Le script TileBehavior n'a pas été trouvé sur le même Game Object");
			this.enabled = false;
		}
		stdScaleValue = transform.localScale.x;
		newScaleValue = transform.localScale.x;
        rotationArrow = transform.Find("Rotation Arrow");
	}

    Vector3 GetTouchPosition()
    {
        Vector3 position;
        if (Input.touchCount > 0) position = Input.GetTouch(0).position;
        else position = Input.mousePosition;
        return Camera.main.ScreenToWorldPoint(position);
    }
	
	void Update () {
        if (rotationPhase)
        {
            if (associatedTile.clockwiseRotation) angleArrow -= 100.0f * Time.deltaTime;
            else angleArrow += 100.0f * Time.deltaTime;
            angleArrow = angleArrow % 360.0f;
            if (angleArrow < 0) angleArrow += 360;
            rotationArrow.rotation = Quaternion.Euler(0, 0, angleArrow);

            if (interacting && Time.time - selectionTimer > SIMPLE_CLICK_DURATION)
            {
                var pos = GetTouchPosition();

                float angle = NormalizeAngle(-AngleSigned(lastPosition - transform.position, pos - transform.position));
                //Debug.Log(angle);

                //Debug.Log(Vector3.forward);
                try
                {
                    transform.rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * angle + temporaryChoosedDirection.eulerAngles.z, Vector3.forward);
                }
                catch (UnityException e)
                {
                    Debug.LogError("TileBehaviorIHM, OnMouseDrag: Unity exception caught +" + e.Data);
                }
            }
        }
	}

	public void chooseTileToRotate () {
		if (highlight == null) {
			Vector3 highlightPosition = new Vector3(transform.position.x, transform.position.y, tileHighlight.transform.position.z);
			highlight = (GameObject) Instantiate(tileHighlight, highlightPosition, tileHighlight.transform.rotation);
            highlight.GetComponent<ChooseRotationTile>().tile = this;
            displayArrow();
            Debug.Assert(associatedTile.canRotateSisterTile());
            associatedTile.getSisterTile().GetComponent<TileBehaviorIHM>().chooseTileToRotate();
		}
	}

	public void disableHighlight () {
		if (highlight != null) Destroy(highlight);
	}
	
	// Faire tourner la salle d'un cran dans son sens prédéfini
	public void rotateTile () {
		rotateTile(false);
	}
	
	// Faire tourner la salle d'un cran dans un sens donné
	public void rotateTile (bool sensInverse) {
		int direction;
		// Si on souhaite faire tourner la salle dans son sens inverse, on inverse momentanément son sens
		if (!sensInverse) {
			if (associatedTile.clockwiseRotation) direction = 1;
			else direction = -1;
		}
		else {
			if (associatedTile.clockwiseRotation) direction = -1;
			else direction = 1;
		}
		// On fait tourner le Sprite de 90° dans le sens approprié
		rotateTileMesh (direction*90);
        associatedTile.rotateTile(sensInverse, true);
	}
	
	// Tourner le Sprite de la salle selon un angle donné
	public void rotateTileMesh (int angle) {
		if (angle != 0) transform.RotateAround(transform.position, Vector3.forward, -angle);
	}

    public void goToDesiredOrientation(int orientation)
    {
        if (orientation >= 0 && orientation < 4)
        {
            while (orientation != associatedTile.tileRotation)
            {
                rotateTile(false);
            }
        }
    }
	
	public void enableTileRotation ()
    {
        gManager.actionWheel.resetButtonsActions();
        if (highlight != null)
        {
            disableHighlight();
            hideArrow();
            Debug.Assert(associatedTile.canRotateSisterTile());
            TileBehaviorIHM sisterTile = associatedTile.getSisterTile().GetComponent<TileBehaviorIHM>();
            sisterTile.disableHighlight();
            sisterTile.hideArrow();
        }
		gManager.tilePourRotation = gameObject;
		originalDirection = transform.rotation;
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sR in spriteRenderers)
        {
            sR.sortingLayerName = "HUD";
        }
        //GetComponent<SpriteRenderer>().sortingLayerName = "Selection";
		for (int i=0 ; i < transform.childCount ; i++) {
            if (transform.GetChild(i).GetComponent<CaseBehavior>() != null)
            {
				foreach (Token t in transform.GetChild(i).GetComponent<CaseBehavior>().tokens_) {
					t.GetComponent<TokenIHM>().forceEndScaleValue();
					t.transform.parent = transform.GetChild(i);
					t.GetComponent<TokenIHM>().changeSortingLayer("Selection");
				}
			}
		}
        scaleChangeStartTime = Time.time;
        gManager.playSound(engagingRotation);
        newScaleValue = stdScaleValue * coeffRotatingScaleValue;
        Vector3 newScale = new Vector3(newScaleValue, newScaleValue, 1);
        iTween.ScaleTo(gameObject, iTween.Hash("name", "scaleTileUp", "scale", newScale, "time", SCALE_UP_DURATION, "easetype", iTween.EaseType.linear, "oncomplete", "enableTileInteraction"));
		//StartCoroutine(scaleTileUp());
	}

    void disableTileRotation()
    {
        gManager.displayCancelButton = false;
        GetComponent<Collider>().enabled = false;
        hideArrow();
        scalingDown = true;
        scaleChangeStartTime = Time.time;
        gManager.playSound(disengagingRotation);
        newScaleValue = stdScaleValue;
        Vector3 newScale = new Vector3(newScaleValue, newScaleValue, 1);
        iTween.ScaleTo(gameObject, iTween.Hash("name", "scaleTileDown", "scale", newScale, "time", SCALE_DOWN_DURATION, "easetype", iTween.EaseType.linear, "oncomplete", "disableTileInteraction"));
		//StartCoroutine(scaleTileDown());
	}

    float positiveAngle(float angle)
    {
        while (angle < 0) angle += 360.0f;
        return angle;
    }

	void updateTileAfterRotation ()
    {
        gManager.rotationEnCours = false;
        inverseRotationDirection = false;

        if (gManager.actionPointCost == 0) // no rotation
        {
            gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().characterSelection();
        }
        else
        {
            Quaternion finalDirection = getClosestOrthogonalDirection();

            Debug.Assert(gManager.actionPointCost == directionActionPointCost(finalDirection));

            bool isDirectionInversed = false;
            int numberOfRotations = countNumberOfRotations(finalDirection);
            // If the character is a Mekanork, check if rotation is inversed
            if (gManager.actionCharacter.GetComponent<CB_Mechanork>() != null && numberOfRotations == 3)
            {
                Debug.Assert(gManager.actionPointCost == 1);
                isDirectionInversed = true;
            }
            else Debug.Assert(gManager.actionPointCost == numberOfRotations);

            doRotateTile(gManager.actionPointCost, isDirectionInversed); // do the rotation
        }
    }

    void doRotateTile(int numberOfRotation, bool isDirectionInversed = false)
    {
        gManager.onlineGameInterface.RecordRotation(associatedTile, numberOfRotation, isDirectionInversed);

        Debug.Assert(numberOfRotation > 0 && numberOfRotation <= 3); // there must be between 1 and 3 rotations
        Debug.Assert(numberOfRotation == 1 || !isDirectionInversed); // there is only one case of inverse rotation: mechanork one turn inversed.
        for (int i = 0; i < numberOfRotation; i++)
            if (gManager.actionCharacter.GetComponent<CB_Mechanork>() != null) gameObject.GetComponent<TileBehavior>().rotateTile(isDirectionInversed, false);
            else gameObject.GetComponent<TileBehavior>().rotateTile(false);
        if (gManager.actionPointCost != numberOfRotation)
        {
            Debug.LogWarning("ActionPointCost is not up to date for rotation. Change from " + gManager.actionPointCost + " to " + numberOfRotation);
            gManager.actionPointCost = numberOfRotation;
        }
        gManager.actionCharacter.GetComponent<CharacterBehaviorIHM>().endDeplacementIHM();

        gManager.onlineGameInterface.EndReplayAction();
    }

    IEnumerator scaleTileUp () {
		if (scalingDown) {
			newScaleValue = stdScaleValue;
			Vector3 newScale = new Vector3(newScaleValue, newScaleValue, 1);
			transform.localScale = newScale;
			yield return new WaitForSeconds(0.1f);
			StartCoroutine(scaleTileUp());
		}
		else {
			yield return new WaitForSeconds(0.01f);
            if (Time.time - scaleChangeStartTime < SCALE_UP_DURATION)
            {
                newScaleValue = stdScaleValue + stdScaleValue * (coeffRotatingScaleValue - 1) * ((Time.time - scaleChangeStartTime) / SCALE_UP_DURATION);

				Vector3 newScale = new Vector3(newScaleValue, newScaleValue, 1);
				transform.localScale = newScale;
				StartCoroutine(scaleTileUp());
			}
			else {
				newScaleValue = stdScaleValue * coeffRotatingScaleValue;
				Vector3 newScale = new Vector3(newScaleValue, newScaleValue, 1);
				transform.localScale = newScale;
                enableTileInteraction();
			}
		}
	}

    void enableTileInteraction()
    {
        if (gManager.playerInteractionAvailable())
        {
            GetComponent<Collider>().enabled = true;
            displayArrow();
            temporaryChoosedDirection = originalDirection;
        }
        else
        {
            gManager.actionPointCost = 1;
            Vector3 rotationDirection = transform.forward * 90;
            if (inverseRotationDirection)
            {
                if (!associatedTile.clockwiseRotation) rotationDirection *= -1;
            }
            else if (associatedTile.clockwiseRotation) rotationDirection *= -1;
            iTween.RotateAdd(gameObject, iTween.Hash("amount", rotationDirection, "time", SCALE_UP_DURATION, "easetype", iTween.EaseType.linear, "oncomplete", "disableTileRotation"));
        }
    }

    void displayArrow()
    {
        rotationPhase = true;
        SpriteRenderer arrow = rotationArrow.GetComponent<SpriteRenderer>();
        arrow.sortingLayerName = "HUDSelection";
        arrow.enabled = true;
    }

    void hideArrow()
    {
        rotationPhase = false;
        SpriteRenderer arrow = rotationArrow.GetComponent<SpriteRenderer>();
        arrow.sortingLayerName = "Board";
        arrow.enabled = false;
    }
	
	IEnumerator scaleTileDown () {
		yield return new WaitForSeconds(0.01f);
        if (Time.time - scaleChangeStartTime < SCALE_DOWN_DURATION)
        {
            newScaleValue = stdScaleValue * coeffRotatingScaleValue - stdScaleValue * (coeffRotatingScaleValue - 1) * ((Time.time - scaleChangeStartTime) / SCALE_DOWN_DURATION);
			
			Vector3 newScale = new Vector3(newScaleValue, newScaleValue, 1);
			transform.localScale = newScale;
			StartCoroutine(scaleTileDown());
		}
		else {
			newScaleValue = stdScaleValue;
			Vector3 newScale = new Vector3(newScaleValue, newScaleValue, 1);
			transform.localScale = newScale;
            disableTileInteraction();
		}
	}

    void disableTileInteraction()
    {
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sR in spriteRenderers)
        {
            if (sR.GetComponent<CibleDeplacement>() == null) sR.sortingLayerName = "Board";
        }
        //GetComponent<SpriteRenderer>().sortingLayerName = "Board";
        Transform personnages = GameObject.Find("Personnages").transform;
        Transform items = GameObject.Find("Items").transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name;
            if (childName != "Herse" && childName != "Rotation Arrow")
            {
                foreach (Token t in transform.GetChild(i).GetComponent<CaseBehavior>().tokens_)
                {
                    if (t.tokenHolder == null)
                    {
                        if (t.GetComponent<CharacterBehavior>() != null) t.transform.parent = personnages;
                        else if (t.GetComponent<Item>() != null) t.transform.parent = items;
                        else
                        {
                            t.transform.parent = null;
                            Debug.LogError("TileBehaviorIHM, disableTileInteraction: le token à remettre à sa place dans la hiérarchie n'est pas un personnage ni un objet");
                        }
                    }
                    else
                    {
                        t.transform.parent = t.tokenHolder.transform;
                        Vector3 newPosition = t.tokenHolder.transform.position;
                        t.transform.position = new Vector3(newPosition.x + 0.5f, newPosition.y + 0.5f, 0);
                    }
                    t.GetComponent<TokenIHM>().changeSortingLayer("TokensOnBoard");
                }
            }
        }
        scalingDown = false;
        updateTileAfterRotation();
        gManager.straightenTokens();
    }
	
	// Rotation
	
    public void OnPointerDown(PointerEventData e)
	{
		gManager.cameraMovementLocked = true;
		interacting = true;
        lastPosition = GetTouchPosition();
		selectionTimer = Time.time;
	}

    Quaternion getClosestOrthogonalDirection()
    {
        float direction = transform.rotation.eulerAngles.z;
        Quaternion finalDirection = Quaternion.identity;
        if (direction < 45 || direction > 315) finalDirection = Quaternion.Euler(0, 0, 0);
        else if (direction > 45 && direction < 135) finalDirection = Quaternion.Euler(0, 0, 90);
        else if (direction > 135 && direction < 225) finalDirection = Quaternion.Euler(0, 0, 180);
        else if (direction > 225 && direction < 315) finalDirection = Quaternion.Euler(0, 0, 270);
        return finalDirection;
    }
	
    public void OnPointerUp(PointerEventData e)
    {
		interacting = false;
		int currentActionsPoints = gManager.actionCharacter.GetComponent<CharacterBehavior>().totalCurrentActionPoints();
        Quaternion finalDirection = getClosestOrthogonalDirection();
		
		int currentDirectionActionPointCost = directionActionPointCost(finalDirection);
        if (gManager.actionCharacter.GetComponent<CB_Mechanork>() != null) Debug.Assert(currentDirectionActionPointCost <= 2);
        if (currentActionsPoints < currentDirectionActionPointCost)
        {
            //iTween.RotateTo(gameObject, iTween.Hash("name", "realignTile", "rotation", originalDirection.eulerAngles, "speed", 100, "easetype", iTween.EaseType.linear, "onupdate", "checkForPlayerInteraction"));
            StartCoroutine(rotateToGivenDirection(originalDirection));
        }
        else
        {
            gManager.actionPointCost = currentDirectionActionPointCost;
            //iTween.RotateTo(gameObject, iTween.Hash("name", "realignTile", "rotation", finalDirection.eulerAngles, "speed", 100, "easetype", iTween.EaseType.linear, "onupdate", "checkForPlayerInteraction"));
            StartCoroutine(rotateToGivenDirection(finalDirection));
        }
		gManager.cameraMovementLocked = false;
		// Si on valide la position du
		if (Time.time - selectionTimer < SIMPLE_CLICK_DURATION) disableTileRotation();
	}

    public bool selectedForRotation()
    {
        return (transform.localScale.x != stdScaleValue);
    }

    public void cancelRotation()
    {
        StartCoroutine(rotateToGivenDirection(originalDirection));
        disableTileRotation();
    }

    void checkForPlayerInteraction()
    {
        if (interacting) iTween.StopByName("realignTile");
    }

    static int modulo(int value, int mod) { return (value + mod) % mod; }
    static float getPositiveAngle(float angle) { float increment = 360.0f; while (angle < 0.0f) { angle += increment; increment *= 2.0f; } return angle; }
    static int convertAngleToQuarter(float angle) { return modulo(Mathf.RoundToInt(getPositiveAngle(angle) / 90.0f), 4); }
    static int countClockwizeRotations(float originalAngle, float newAngle) { return modulo(convertAngleToQuarter(originalAngle) - convertAngleToQuarter(newAngle), 4); }

    int countNumberOfRotations(Quaternion direction)
    {
        int nClockwizeRotation = countClockwizeRotations(originalDirection.eulerAngles.z, direction.eulerAngles.z);
        if (associatedTile.clockwiseRotation) return nClockwizeRotation;
        else return modulo(-nClockwizeRotation, 4);
    }

	int directionActionPointCost (Quaternion direction) {
        int cost = countNumberOfRotations(direction);
        if (gManager.actionCharacter.GetComponent<CB_Mechanork>() != null && cost == 3)
            cost = 1;
        return cost;
	}

    IEnumerator rotateToGivenDirection(float startTime, float duration, Quaternion playerReleasePosition, Quaternion endDirection)
    {
        yield return new WaitForSeconds(0.001f);
        temporaryChoosedDirection = endDirection;
        float valueProgression = (Time.time - startTime) / duration;
        if (!interacting && Time.time - startTime < duration)
        {
            transform.rotation = Quaternion.Lerp(playerReleasePosition, endDirection, valueProgression);
            StartCoroutine(rotateToGivenDirection(startTime, duration, playerReleasePosition, endDirection));
        }
    }
	
	IEnumerator rotateToGivenDirection (Quaternion endDirection) {
        temporaryChoosedDirection = endDirection;
		yield return new WaitForSeconds(0.001f);
		if (transform.rotation != endDirection && !interacting) {
			transform.rotation = Quaternion.Lerp(transform.rotation, endDirection, 0.2f);
			StartCoroutine(rotateToGivenDirection(endDirection));
		}
		//else gManager.straightenTokens();
	}

	// Angles

	public static float AngleSigned(Vector2 v1, Vector2 v2) {
		var left = v2;
		var up = new Vector2(-v2.y, v2.x);
		
		var angle = Angle(v1, left);
		if (Vector2.Dot(v1, up) < 0)
			angle *= -1;
		
		return angle;
	}

	public static float Angle(Vector2 v1, Vector2 v2) {
		float dot = Vector2.Dot(v1, v2) / v2.magnitude / v1.magnitude;
		return (float)Mathf.Acos(dot);
	}
	
	public static float AngleDistanceSigned(float angle1, float angle2) {
		return NormalizeAngle(angle2 - angle1);
	}        
	
	/// <summary>
	/// Return an angle between -PI and +PI
	/// </summary>
	/// <param name="angle"></param>
	/// <returns></returns>
	public static float NormalizeAngle(float angle) {
		angle = angle % (2 * (float)Mathf.PI);
		if (angle < -Mathf.PI)
			return angle + 2 * (float)Mathf.PI;
		if (angle > Mathf.PI)
			return angle - 2 * (float)Mathf.PI;
		return angle;
	}
}
