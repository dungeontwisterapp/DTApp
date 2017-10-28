using UnityEngine;
using System;
using System.Collections;
using BestHTTP;

namespace Multi
{

    namespace Web
    {

        public class RequestHandle
        {
            public delegate void CallBackType(RequestHandle request);
            public delegate void SuccessCallBackType(bool success);
            public delegate void DataCallBackType(JSONObject data);

            HTTPRequest _request;
            private string _status;
            private CallBackType _callback;
            private JSONObject _result;
            private bool _background;
            private bool _success;
            private bool _done;

            // you can set those callbacks for use in the regular callback function
            public SuccessCallBackType successCallback;
            public DataCallBackType dataCallback;

            public bool Success { get { return _success; } }

            public JSONObject ResultData { get { return (Success && _result.HasField("data")) ? _result.GetField("data") : _result; } }

            public bool Done { get { return _done; } }
            public bool Background { get { return _background; } }

            public RequestHandle(HTTPRequest request, string message, CallBackType callback, bool background = false)
            {
                _request = request;
                _status = message;
                _callback = callback;
                _done = false;
                _background = background;
                successCallback = null;
                dataCallback = null;
            }

            public void Abort()
            {
                if (!_done)
                {
                    if (!_background) Logger.Instance.Log("NW_WARNING", "Request Aborted");
                    _request.Abort();
                    _done = true;
                }
            }

            public IEnumerator ProcessRequest()
            {
                Logger.Instance.Log("DETAIL", "Process Request: " + _status);

                /*if (_request.Uri.ToString().Contains("characterChoice") || _request.Uri.ToString().Contains("placeTokenOnTile"))
                {
                    Logger.Instance.Log("LOG", "URI: " + _request.Uri.ToString());
                    OnHeaderEnumerationDelegate callback = (header, values) => {
                        string log = header + ":\n";
                        foreach (string value in values)
                            log += "* " + value + "\n";
                        Logger.Instance.Log("LOG", log);
                    };
                    _request.EnumerateHeaders(callback);
                }*/

                _request = _request.Send();

                while (_request.State < HTTPRequestStates.Finished)
                {
                    yield return new WaitForSeconds(0.1f);

                    _status += ".";
                }

                bool success = CheckRequestStatus();

                if (success)
                {
                    _result = new JSONObject(_request.Response.DataAsText);
                    if (JSONTools.HasFieldOfTypeNumber(_result, "status") && _result.GetField("status").n == 0)
                    {
                        string encoded = _result.GetField("error").str;
                        string decoded = AsciiDecoder.Decode(encoded);

                        Logger.Instance.Log("NW_WARNING", decoded);
                        _success = false;
                    }
                    else
                    {
                        Logger.Instance.Log("NW_INFO", "Success");
                        _success = true;
                    }
                }
                else
                {
                    Logger.Instance.Log("NW_WARNING", _status);
                    _success = false;
                }

                _done = true;

                _callback(this);
            }

            private bool CheckRequestStatus()
            {
                bool success = false;

                // Check the outcome of our request.
                switch (_request.State)
                {
                    // The request finished without any problem.
                    case HTTPRequestStates.Finished:

                        if (_request.Response.IsSuccess)
                        {
                            _status = string.Format("Request finished Successfully: {0}", _request.Response.DataAsText);

                            Logger.Instance.Log("DETAIL", _status);

                            success = true;
                        }
                        else
                        {
                            _status = string.Format("Server Error : Status Code: {0}-{1} Message: {2}",
                                                            _request.Response.StatusCode,
                                                            _request.Response.Message,
                                                            _request.Response.DataAsText);
                        }

                        break;

                    // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
                    case HTTPRequestStates.Error:
                        _status = "Request Finished with Error! " + (_request.Exception != null ? (_request.Exception.Message + "\n" + _request.Exception.StackTrace) : "No Exception");
                        break;

                    // The request aborted, initiated by the user.
                    case HTTPRequestStates.Aborted:
                        _status = "Request Aborted!";
                        break;

                    // Ceonnecting to the server is timed out.
                    case HTTPRequestStates.ConnectionTimedOut:
                        _status = "Connection Timed Out!";
                        break;

                    // The request didn't finished in the given time.
                    case HTTPRequestStates.TimedOut:
                        _status = "Processing the request Timed Out!";
                        break;
                }

                return success;
            }
        }
    }
}

