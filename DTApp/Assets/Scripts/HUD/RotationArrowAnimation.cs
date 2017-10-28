using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RotationArrowAnimation : MonoBehaviour {

    public GameObject rotationArrowPrefab;
    GameManager gManager;
    TileBehaviorIHM associatedTile = null;
    CaseBehavior associatedCase;
    Transform rotationActionReminder = null;
    float angleArrow = 0;
    bool clockwiseRotation = true;
    Vector3 stdScale;
    Vector3 neutralScale = new Vector3(1, 1, 1);

	// Use this for initialization
	void Start () {
        gManager = GameManager.gManager;
        associatedCase = GetComponent<CaseBehavior>();
        associatedTile = transform.parent.GetComponent<TileBehaviorIHM>();
        clockwiseRotation = transform.parent.GetComponent<TileBehavior>().clockwiseRotation;
	}

    void Update()
    {
        if (characterOnRotationMechanism())
        {
            if (rotationActionReminder == null)
            {
                CharacterBehavior character = associatedCase.getMainCharacter();
                if (character.GetComponent<CharacterBehaviorIHM>().isAtStandardScale())
                {
                    GameObject arrow = (GameObject)Instantiate(rotationArrowPrefab, character.transform.position, rotationArrowPrefab.transform.rotation);
                    if (clockwiseRotation) arrow.GetComponent<SpriteRenderer>().flipX = true;
                    rotationActionReminder = arrow.transform;
                    rotationActionReminder.SetParent(character.transform);
                    stdScale = arrow.transform.localScale;
                    arrow.transform.localScale = neutralScale;
                }
            }
            else
            {
                if (showRotationAnimationFeedback())
                {
                    rotationActionReminder.localScale = stdScale;
                    if (clockwiseRotation) angleArrow -= 100.0f * Time.deltaTime;
                    else angleArrow += 100.0f * Time.deltaTime;
                    angleArrow = angleArrow % 360.0f;
                    if (angleArrow < 0) angleArrow += 360;
                    rotationActionReminder.rotation = Quaternion.Euler(0, 0, angleArrow);
                }
                else rotationActionReminder.localScale = neutralScale;
            }
        }
        else
        {
            if (rotationActionReminder != null) Destroy(rotationActionReminder.gameObject);
        }
    }

    bool characterOnRotationMechanism()
    {
        bool displayRotationArrow = false;
        if (!associatedTile.rotationPhase && !gManager.rotationEnCours)
        {
            displayRotationArrow = (associatedCase.characters.Count > 0);
        }
        return displayRotationArrow;
    }

    bool showRotationAnimationFeedback()
    {
        bool activeRotationFeedback = false;
        if (!gManager.placerTokens)
        {
            CharacterBehavior chara = associatedCase.getMainCharacter();
            Debug.Assert(chara != null);
            activeRotationFeedback = (chara.totalCurrentActionPoints() > 0 && GameManager.gManager.isActivePlayer(chara.affiliationJoueur) && !chara.wounded && !chara.freshlyHealed);
        }
        return activeRotationFeedback;
    }

}
