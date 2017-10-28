using UnityEngine;
using System.Collections.Generic;

namespace Multi
{
    namespace BGA
    {

        public class PacketData
        {
            public enum Type { UNKNOWN, HISTORY }
            public enum Status { UNKNOWN, SLEEPING, WAITING, PROCESSING, RESOLVED }

            // accessible data
            public string channel;
            public string tableId;
            public int packetId = 0;
            public string packetTypeStr = "unknown";
            public Type packetType = Type.UNKNOWN;
            public int moveId = 0;
            public string time;

            // other
            private Status _status = Status.UNKNOWN;
            private int _currentNotification = 0;
            private List<NotificationData> notifications;

            public string debugPrintString = "";

            public bool isValid { get { return _status != Status.UNKNOWN; } }
            public Status status { get { return _status; } set { Debug.Assert(isValid); _status = value; } }

            public PacketData(JSONObject json)
            {
                ParseData(json);
                debugPrintString = JSONTools.FormatJsonDisplay( json );
            }

            public void MarkAsWaiting() { Debug.Assert(status == Status.SLEEPING); _status = Status.WAITING; MarkAsChanged(); }
            public void MarkAsChanged() { UpdateStatus(); }
            public NotificationData GetCurrentNotification() { return (_currentNotification < notifications.Count) ? notifications[_currentNotification] : null; }
            public bool isResolved() { return status == Status.RESOLVED; }
            public bool isProcessing() { return status == Status.PROCESSING || (status != Status.RESOLVED && _currentNotification > 0); }

            #region implementation

            private bool ParseData(JSONObject json)
            {
                if (JSONTools.HasFieldOfTypeString(json, "time")) time = json.GetField("time").str;
                if (JSONTools.HasFieldOfTypeString(json, "channel")) channel = json.GetField("channel").str;
                if (JSONTools.HasFieldOfTypeString(json, "table_id")) tableId = json.GetField("table_id").str;
                if (JSONTools.HasFieldOfTypeString(json, "packet_id")) int.TryParse(json.GetField("packet_id").str, out packetId);
                if (JSONTools.HasFieldOfTypeString(json, "packet_type")) { packetTypeStr = json.GetField("packet_type").str; packetType = StringToType(packetTypeStr); }
                if (packetType == Type.HISTORY && JSONTools.HasFieldOfTypeString(json, "move_id")) int.TryParse(json.GetField("move_id").str, out moveId);

                notifications = new List<NotificationData>();
                if (JSONTools.HasFieldOfTypeArray(json, "data"))
                {
                    for (int i=0; i < json.GetField("data").Count; ++i)
                    {
                        notifications.Add(new NotificationData(json.GetField("data")[i]));
                    }
                }

                if ( CheckValidity() )
                {
                    _status = Status.SLEEPING;
                }

                return isValid;
            }

            private void UpdateStatus()
            {
                switch (status)
                {
                    case Status.WAITING:
                    case Status.PROCESSING:
                        if (_currentNotification < notifications.Count)
                        {
                            NotificationData notif = notifications[_currentNotification];
                            switch (notif.status)
                            {
                                case NotificationData.Status.SLEEPING:
                                    notif.MarkAsWaiting();
                                    _status = Status.WAITING;
                                    break;
                                case NotificationData.Status.WAITING:
                                    _status = Status.WAITING;
                                    break;
                                case NotificationData.Status.PROCESSING:
                                    _status = Status.PROCESSING;
                                    break;
                                case NotificationData.Status.RESOLVED:
                                    ++_currentNotification;
                                    UpdateStatus();
                                    break;
                                case NotificationData.Status.UNKNOWN:
                                default:
                                    Logger.Instance.Log("ERROR", "Ill-formated notification skipped");
                                    ++_currentNotification;
                                    UpdateStatus();
                                    break;
                            }
                        }
                        else
                        {
                            _status = Status.RESOLVED;
                        }
                        break;
                    case Status.SLEEPING:
                    case Status.RESOLVED:
                    case Status.UNKNOWN:
                    default:
                        break;
                }
            }

            private bool CheckValidity()
            {
                return time != null
                    && channel != null
                    && tableId != null
                    && packetId > 0
                    && packetType != Type.UNKNOWN
                    && moveId > 0
                    && notifications != null;
            }

            private static string TypeToString(Type type)
            {
                switch (type)
                {
                    case Type.HISTORY:
                        return "history";
                    case Type.UNKNOWN:
                    default:
                        return "unknown";
                }
            }

            private static Type StringToType(string type)
            {
                switch (type)
                {
                    case "history":
                    case "resend":
                        return Type.HISTORY;
                    default:
                        return Type.UNKNOWN;
                }
            }
            #endregion
        }
    }
}
