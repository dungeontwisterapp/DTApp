using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CombatBonusAnimation : MonoBehaviour {

	public Transform target;
	Vector3 originalPosition;

	// Use this for initialization
	void Start () {
		originalPosition = transform.position;
	}

	public IEnumerator playAnimation () {
		yield return new WaitForSeconds(0.01f);
		if (GetComponent<Image>().color != Color.white) {
			StartCoroutine(playAnimation());
		}
		else {
            //Instantiate(GameObject.FindGameObjectWithTag("Manager").GetComponent<GameManager>().victoryPointsTextPrefab, transform.position)
            /*
			transform.position = Vector3.MoveTowards(transform.position, target.position, 250.0f * Time.deltaTime);
			if (transform.position != target.position) StartCoroutine(playAnimation());
			else {
				//Debug.LogWarning("arrived");
				string prefix = "";
				if (name != "CombatBonus") {
					prefix = name.Split('_')[0] + '_';
					//Debug.Log(prefix);
				}
				Text combatValue = transform.parent.Find(prefix+"CombatValue/Text").GetComponent<Text>();
				int combatBonus = System.Int32.Parse(transform.Find("Text").GetComponent<Text>().text);
				combatValue.text = (System.Int32.Parse(combatValue.text) + combatBonus).ToString();
				Text totalCombatValue = transform.parent.parent.Find("Total Combat Value/Text").GetComponent<Text>();
				totalCombatValue.text = (System.Int32.Parse(totalCombatValue.text) + combatBonus).ToString();
				transform.position = originalPosition;
				gameObject.SetActive(false);
			}
            */
		}
	}

	public void resetPosition () {
		transform.position = originalPosition;
	}
}
