using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Multi
{
    public class Status : MonoBehaviour
    {
        private TimeSpan _msgDuration;

        public GameObject logPanel;
        public GameObject statusPanel;

        // Use this for initialization
        void Start()
        {
            _msgDuration = new TimeSpan(0, 0, 0, 5); // 5 seconds
        }

        void OnGUI()
        {
            if (statusPanel)
            {
                Text statusText = statusPanel.transform.FindChild("Text").gameObject.GetComponent<Text>() as Text;
                statusText.text = Http.Instance.Status;
            }


            if (logPanel)
            {
                List<string> filter = new List<string> { "NW_INFO", "NW_WARNING" };
                Logger.Message message = Logger.Instance.GetLastMessage(filter);
                if (message != null && (DateTime.Now - message.date) < _msgDuration)
                {
                    Text logText = logPanel.transform.FindChild("Text").gameObject.GetComponent<Text>() as Text;
                    if (message.type == "NW_WARNING")
                    {
                        logText.color = Color.red;
                    }
                    else if (message.type == "NW_INFO")
                    {
                        logText.color = Color.green;
                    }
                    logText.text = Logger.Instance.Print(message);
                    logPanel.SetActive(true);
                }
                else
                {
                    logPanel.SetActive(false);
                }
            }
        }

        public void Close()
        {
        }

        public void Open()
        {
        }

 /*       public void Awake()
        {
            Debug.Log("Status: Awake");
        }

        public void OnEnable()
        {
            Debug.Log("Status: OnEnable");
        }

        public void OnDisable()
        {
            Debug.Log("Status: OnDisable");
        }

        public void OnDestroy()
        {
            Debug.Log("Status: OnDestroy");
        }*/
    }
}
