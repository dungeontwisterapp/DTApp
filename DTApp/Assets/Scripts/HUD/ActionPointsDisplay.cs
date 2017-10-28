using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ActionPointsDisplay : MonoBehaviour {

	public int playerIndex;
	public Sprite emptyState;
	public Sprite availableState;
	public Sprite costFeedbackState;

	GameManager gManager;
	List<Image> childrenSprites = new List<Image>();

	// Use this for initialization
    void Start()
    {
        gManager = GameManager.gManager;
		for (int i=0 ; i < transform.childCount ; i++) {
			childrenSprites.Add(transform.GetChild(i).GetComponent<Image>());
		}
		childrenSprites.Reverse();
	}
	
	// Update is called once per frame
	void Update () {
        if (!gManager.freezeDisplay)
        {
            int i = 0;
            int actionPoints = gManager.getPlayerActionPoints(playerIndex);
            foreach (Image actionPoint in childrenSprites)
            {
                if (i >= actionPoints) actionPoint.sprite = emptyState;
                else if (i >= actionPoints - gManager.actionPointCost) actionPoint.sprite = costFeedbackState;
                else actionPoint.sprite = availableState;
                i++;
            }
        }
	}
}
