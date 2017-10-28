using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Multi
{

    public class Logger
    {

        static private Logger _instance = null;

        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Logger();
                }
                return _instance;
            }
        }

        public enum DebugLevel { NONE, INFO, WARNING, ERROR }

        public class Message
        {
            public string type;
            public string content;
            public DateTime date;
            public int id;
        }

        // ASSERTION
        // _messages[i].id == (_nextId - _messages.Count + i)

        private int _maxsize;
        private List<Message> _messages;
        private Dictionary<string, DebugLevel> _registeredTypes;
        private List<string> _stackingTypes;
        private string _dateFormat;
        private int _nextId;

        private Logger()
        {
            Clear();
            DefaultRegister();
        }

        public void Clear()
        {
            _maxsize = 10; // keep 10 last messages in queue
            _messages = new List<Message>();
            _registeredTypes = new Dictionary<string, DebugLevel>();
            _stackingTypes = new List<string>();
            _dateFormat = "[HH:mm:ss:fff] "; // "yyyyMMddHHmmss";
            _nextId = 0;
        }

        public void DefaultRegister()
        {
#if DEBUG_DETAIL
            Register("DETAIL", DebugLevel.INFO);
            Register("LOG", DebugLevel.INFO);
#elif DEBUG_LOG
            Register("DETAIL", DebugLevel.NONE);
            Register("LOG", DebugLevel.INFO);
#else
            Register("DETAIL", DebugLevel.NONE);
            Register("LOG", DebugLevel.NONE);
#endif
            Register("INFO", DebugLevel.INFO); // Code Info
            Register("WARNING", DebugLevel.WARNING); // Code Warning
            Register("ERROR", DebugLevel.ERROR); // Code Error
            Register("NW_INFO", DebugLevel.INFO, true); // display to user
            Register("NW_WARNING", DebugLevel.WARNING, true); // Warning + display to user
        }

        public string Print(Message message, bool displayDate = false, bool displayType = false, bool displayId = false)
        {
            string result = "";
            if (displayDate)
            {
                result += message.date.ToString(_dateFormat);
            }
            if (displayType)
            {
                result += message.type + ": ";
            }
            if (displayId)
            {
                result += "(" + message.id.ToString() + ") ";
            }
            result += message.content;
            return result;
        }

        /// Register a Log type
        ///  string type: name of the log type
        ///  DebugLevel: defining unity debug log type
        ///  stack: Keep in stack memory (for later display)
        public void Register(string type, DebugLevel debugLevel, bool stack = false)
        {
            if (_registeredTypes.ContainsKey(type))
            {
                Debug.LogWarning("Logger Warning: type " + type + " is already registered with debug level " + _registeredTypes[type] + (_stackingTypes.Contains(type) ? " stacking" : ""));
                Debug.LogWarning("Logger Warning: type " + type + " register overwritten with debug level " + debugLevel + (stack ? " stacking" : ""));
            }
            else
            {
#if DEBUG_LOGGER
                Debug.Log("Logger Info: Register type " + type + " with debug level " + debugLevel.ToString() + (stack ? " stacking" : ""));
#endif
            }
            _registeredTypes[type] = debugLevel;
            if (stack && !_stackingTypes.Contains(type))
                _stackingTypes.Add(type);
            if (!stack && _stackingTypes.Contains(type))
                _stackingTypes.Remove(type);
        }

        public void Log(string type, string content)
        {
            if (!_registeredTypes.ContainsKey(type))
            {
#if DEBUG_LOGGER
                Debug.LogWarning("Logger Warning: type " + type + " is not registered");
                Register(type, DebugLevel.INFO);
#else
                return;
#endif
            }

            Message message = new Message();
            message.type = type;
            message.content = content;
            message.date = DateTime.Now;
            message.id = _nextId++;

            switch (_registeredTypes[type])
            {
                case DebugLevel.ERROR:
                    Debug.LogError(Print(message, true, true, true));
                    break;
                case DebugLevel.WARNING:
                    Debug.LogWarning(Print(message, true, true, true));
                    break;
                case DebugLevel.INFO:
                    Debug.Log(Print(message, true, true, true));
                    break;
                case DebugLevel.NONE:
                    break;
            }

            if (_stackingTypes.Contains(type))
            {
                if (_maxsize > 0 && _messages.Count == _maxsize)
                {
                    _messages.RemoveAt(0);
                }
                _messages.Add(message);
            }
        }

        public Message GetLastMessage(List<string> filter = null)
        {
            for (int i = _messages.Count - 1; i >= 0; --i)
            {
                Message message = _messages[i];
                if (filter == null || filter.Contains(message.type))
                {
                    return message;
                }
            }

            return null;
        }

        public Message GetNextMessage(int previousId, List<string> filter = null)
        {
            int start = previousId + _messages.Count - _nextId;
            if (start < 0) start = 0;
            for (int i = start; i < _messages.Count; ++i)
            {
                Message message = _messages[i];
                if (filter == null || filter.Contains(message.type))
                {
                    return message;
                }
            }

            return null;
        }
    }
}
