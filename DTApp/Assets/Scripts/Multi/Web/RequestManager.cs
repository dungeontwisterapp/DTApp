using UnityEngine;
using System.Collections;
using BestHTTP;

namespace Multi
{

    namespace Web
    {

        public class RequestManager : MonoBehaviour
        {
            RequestHandle _request = null;

            public void Abort()
            {
                if (OnGoing()) _request.Abort();
            }

            public bool OnGoing()
            {
                return (_request != null) && !_request.Done;
            }

            public RequestHandle ScheduleRequest(HTTPRequest request, string message, RequestHandle.CallBackType callback, bool background = false, bool force = false)
            {
                return ScheduleRequest(new RequestHandle(request, message, callback, background), force);
            }

            public RequestHandle ScheduleRequest(RequestHandle request, bool force = false)
            {
                if (OnGoing())
                {
                    if (force || (_request.Background && !request.Background))
                    {
                        Abort();
                    }
                    else
                    {
                        Logger.Instance.Log("WARNING", "Please wait for current request to terminate");
                        return null;
                    }
                }

                _request = request;
                StartCoroutine(_request.ProcessRequest());
                return request;
            }
        }
    }
}
