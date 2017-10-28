using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class test_collision : MonoBehaviour {
	
	void OnMouseDown () {
		if (EventSystem.current.IsPointerOverGameObject()) Debug.Log("ok");
		else Debug.LogWarning("HideActionCards, OnMouseDown: L'input a été détecté sur une partie du HUD");
	}

}
