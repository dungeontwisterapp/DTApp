using UnityEngine;
using System.Collections;

public class ZoomControl : MonoBehaviour {

	public float pas = 0.1f;
	public float speed = 1;
    [HideInInspector]
    public float ZOOM_MIN = 1.6f;
    public float ZOOM_MAX = 6.0f;
    //float ZOOM_INPUT_TRESHOLD = 4.0f;

    Vector3 targetZoom;
    //bool zooming = false;

	Vector3 startPosition;
	GUIText debugDisplay;

    GameManager gManager;
    Camera gameCamera;

    Vector2?[] oldTouchPositions = {
		null,
		null
	};
    Vector2 oldTouchVector;
    float oldTouchDistance;
    float ZOOM_MIN_SIZE_VALUE = 1.6f, ZOOM_MAX_SIZE_VALUE = 6.0f;

    void Start()
    {
        gameCamera = GetComponent<Camera>();
        gManager = GameManager.gManager;
		startPosition = transform.position;

        float currentRatio = (float)Screen.width / (float)Screen.height;
        float expectedRatio = 16.0f / 9.0f;
        if (currentRatio < expectedRatio) {
            float multiplier = expectedRatio / currentRatio;
            ZOOM_MAX_SIZE_VALUE *= multiplier;
        }
        gameCamera.orthographicSize = ZOOM_MAX_SIZE_VALUE;

        //debugDisplay = GameObject.FindGameObjectWithTag("Respawn").guiText;
        //debugDisplay.text = "";
	}
	
	// Update is called once per frame
    void Update()
    {
        //if (Input.GetMouseButtonDown(0)) Debug.Log(Camera.main.ScreenToWorldPoint(Input.mousePosition));

        if (Input.GetKey(KeyCode.KeypadPlus) && gameCamera.orthographicSize > ZOOM_MIN) gameCamera.orthographicSize -= pas;
        if (Input.GetKey(KeyCode.KeypadMinus) && gameCamera.orthographicSize < ZOOM_MAX_SIZE_VALUE) gameCamera.orthographicSize += pas;
		if (Input.GetKey(KeyCode.Space)) {
			transform.position = new Vector3(0, 0, -10);
			gameCamera.orthographicSize = ZOOM_MIN;
		}
		if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0) transform.Translate(new Vector3(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical"),0) * speed * 100 * Time.deltaTime);

        if (!gManager.cameraMovementLocked)
        {
            if (Input.touchCount > 0)
            {
                // Pour déplacer la caméra
                float rightBorder = 1.7f * (ZOOM_MAX_SIZE_VALUE - gameCamera.orthographicSize);
                float leftBorder = -rightBorder;
                float topBorder = 0.7f * (ZOOM_MAX_SIZE_VALUE - gameCamera.orthographicSize);
                float bottomBorder = -topBorder;

                if (Input.touchCount == 1)
                {
                    if (oldTouchPositions[0] == null || oldTouchPositions[1] != null)
                    {
                        oldTouchPositions[0] = Input.GetTouch(0).position;
                        oldTouchPositions[1] = null;
                    }
                    else
                    {
                        Vector2 newTouchPosition = Input.GetTouch(0).position;

                        transform.position += transform.TransformDirection((Vector3)((oldTouchPositions[0] - newTouchPosition) * gameCamera.orthographicSize / gameCamera.pixelHeight * 2f));
                        transform.position = new Vector3(Mathf.Clamp(transform.position.x, leftBorder, rightBorder), Mathf.Clamp(transform.position.y, bottomBorder, topBorder), -10);

                        oldTouchPositions[0] = newTouchPosition;
                    }
                }
                else
                {
                    if (oldTouchPositions[1] == null)
                    {
                        oldTouchPositions[0] = Input.GetTouch(0).position;
                        oldTouchPositions[1] = Input.GetTouch(1).position;
                        oldTouchVector = (Vector2)(oldTouchPositions[0] - oldTouchPositions[1]);
                        oldTouchDistance = oldTouchVector.magnitude;
                    }
                    else
                    {
                        Vector2 screen = new Vector2(gameCamera.pixelWidth, gameCamera.pixelHeight);

                        Vector2[] newTouchPositions = {
					    Input.GetTouch(0).position,
					    Input.GetTouch(1).position
				    };
                        Vector2 newTouchVector = newTouchPositions[0] - newTouchPositions[1];
                        float newTouchDistance = newTouchVector.magnitude;

                        transform.position += transform.TransformDirection((Vector3)((oldTouchPositions[0] + oldTouchPositions[1] - screen) * gameCamera.orthographicSize / screen.y));
                        gameCamera.orthographicSize *= oldTouchDistance / newTouchDistance;
                        gameCamera.orthographicSize = Mathf.Clamp(gameCamera.orthographicSize, ZOOM_MIN_SIZE_VALUE, ZOOM_MAX_SIZE_VALUE);
                        transform.position -= transform.TransformDirection((newTouchPositions[0] + newTouchPositions[1] - screen) * gameCamera.orthographicSize / screen.y);
                        transform.position = new Vector3(Mathf.Clamp(transform.position.x, leftBorder, rightBorder), Mathf.Clamp(transform.position.y, bottomBorder, topBorder), -10);

                        oldTouchPositions[0] = newTouchPositions[0];
                        oldTouchPositions[1] = newTouchPositions[1];
                        oldTouchVector = newTouchVector;
                        oldTouchDistance = newTouchDistance;
                    }
                }
            }
            else
            {
                //zooming = false;
                oldTouchPositions[0] = null;
                oldTouchPositions[1] = null;
            }
        }
	}

	public void dezoom () {
        startSize = gameCamera.orthographicSize;
        beforeMovePosition = transform.position;
        timer = Time.time;
        StartCoroutine(dezoomCoroutine(0.3f));
	}

    float startSize, timer;
    Vector3 beforeMovePosition;

	IEnumerator dezoomCoroutine (float duration) {
        /*
		transform.position = Vector3.Lerp(transform.position, startPosition, 0.1f);
		camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, ZOOM_MAX, 0.1f);
        */
        float valueProgression = (Time.time - timer) / duration;
        transform.position = Vector3.Lerp(beforeMovePosition, startPosition, valueProgression);
        gameCamera.orthographicSize = Mathf.Lerp(startSize, ZOOM_MAX_SIZE_VALUE, valueProgression);

		yield return new WaitForSeconds(0.01f);
        /*
		if (!Mathf.Approximately(camera.orthographicSize, ZOOM_MAX) || Vector3.Distance(transform.position, startPosition) > 1) {
            StartCoroutine(dezoomCoroutine(duration));
		}
        */
        if (Time.time - timer < duration) StartCoroutine(dezoomCoroutine(duration));
		else {
			transform.position = startPosition;
			gameCamera.orthographicSize = ZOOM_MAX_SIZE_VALUE;
		}
	}

}
