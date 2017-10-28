using UnityEngine;
using System;
using System.Collections.Generic;

namespace Multi
{
    namespace BGA
    {
        public class NotificationSender
        {
            private int _retryCount;
            private int _retryMax;
            private DateTime _lastChange;

            private Queue<KeyValuePair<string, Dictionary<string, string>>> notificationQueue;
            private KeyValuePair<string, Dictionary<string, string>>? sentNotification;
            private enum NotificationStatus { NONE, BUISY, SENT, SUCCESS, FAILURE }
            private NotificationStatus notificationStatus;

            public string buildingType;
            public Dictionary<string, string> buildingArgs;

            public void Clear()
            {
                notificationQueue = new Queue<KeyValuePair<string, Dictionary<string, string>>>();
                ClearBuildingInfos();
                ClearCurrentNotification();
            }

            public void Start()
            {
                _retryMax = 1;
            }

            public void Update()
            {
                UpdateNotificationQueue();
            }

            public bool isProcessing { get { return notificationStatus == NotificationStatus.SENT || notificationStatus == NotificationStatus.FAILURE; } }

            public void EnqueueNotification()
            {
                notificationQueue.Enqueue(new KeyValuePair<string, Dictionary<string, string>>(buildingType, buildingArgs));
                ClearBuildingInfos();
            }

            public void SendCurrentNotification()
            {
                Debug.Assert(sentNotification.HasValue, "sentNotification not set");
                _lastChange = DateTime.Now;
                if (Http.Instance.SendNotification(sentNotification.Value.Key, sentNotification.Value.Value, NotificationResult))
                    notificationStatus = NotificationStatus.SENT;
                else
                    notificationStatus = NotificationStatus.BUISY;
            }

            public void UpdateNotificationQueue()
            {
                switch (notificationStatus)
                {
                    case NotificationStatus.NONE:
                        Debug.Assert(!sentNotification.HasValue, "sentNotification already set");
                        if (notificationQueue.Count > 0)
                        {
                            sentNotification = notificationQueue.Dequeue();
                            SendCurrentNotification();
                        }
                        break;
                    case NotificationStatus.BUISY:
                        if (IsReadyToResend())
                        {
                            SendCurrentNotification();
                        }
                        break;
                    case NotificationStatus.SENT:
                        Debug.Assert(sentNotification.HasValue, "sentNotification not set");
                        break;
                    case NotificationStatus.SUCCESS:
                        Debug.Assert(sentNotification.HasValue, "sentNotification not set");
                        ClearCurrentNotification();
                        break;
                    case NotificationStatus.FAILURE:
                        Debug.Assert(sentNotification.HasValue, "sentNotification not set");
                        if (IsReadyToResend())
                        {
                            if (_retryCount < _retryMax)
                            {
                                _retryCount++;
                                Logger.Instance.Log("WARNING", "retry Sending Notification");
                                SendCurrentNotification();
                            }
                            else if (_retryCount == _retryMax)
                            {
                                _retryCount++; // hack to log this message only once.
                                Logger.Instance.Log("ERROR", "give up Sending Notification");

                                // TODO: display error to current player and undo action
                            }
                        }
                        break;
                }
            }

            public void NotificationResult(bool success)
            {
                Debug.Assert(notificationStatus == NotificationStatus.SENT, "wrong notification status");
                Debug.Assert(sentNotification.HasValue, "sentNotification not set");
                if (success)
                {
                    Debug.Log("Notification: " + sentNotification.Value.Key + " sent successfully");
                    notificationStatus = NotificationStatus.SUCCESS;
                }
                else
                {
                    Debug.LogWarning("Notification: " + sentNotification.Value.Key + " failed");
                    notificationStatus = NotificationStatus.FAILURE;
                }
                UpdateNotificationQueue();
            }

            // implementation details
            private void ClearBuildingInfos()
            {
                buildingType = "";
                buildingArgs = new Dictionary<string, string>();
            }

            private bool IsReadyToResend()
            {
                TimeSpan resendTimeSpan = new TimeSpan(0, 0, 0, 1);
                return DateTime.Now >= _lastChange + resendTimeSpan;
            }

            private void ClearCurrentNotification()
            {
                sentNotification = null;
                notificationStatus = NotificationStatus.NONE;
                _lastChange = DateTime.Now;
                _retryCount = 0;
            }
        }
    }
}
