using UnityEngine;
using System.Collections;
using BestHTTP;
//using BestHTTP.SocketIO;
//using BestHTTP.WebSocket;

namespace Multi
{

    namespace Web
    {

        public class WebSocket : MonoBehaviour
        {

            // socket informations
            //private SocketManager _manager;
            //private WebSocket _socket;

            // Use this for initialization
            void Start()
            {

            }

            // Update is called once per frame
            void Update()
            {

            }

            /*public void CreateSocket()
            {*/
                /*_manager = new SocketManager(new Uri(_memSocketURL));
                Socket root = _manager.Socket; //manager.GetSocket("/");

                //_manager.Transport = BestHTTP.SocketIO.Transports.WebSocketTransport;
                //root.AutoDecodePayload = false;
                // =  Transports.ITransport

                root.On("open", ReceiveEvent);
                root.On("boop", ReceiveEvent);
                root.On("error", ReceiveEvent);
                root.On("close", ReceiveEvent);
                root.On("bgamsg", ReceiveEvent);
                root.On("tablemanagerInfosChanged", ReceiveEvent);*/
                /*
                _socket = new WebSocket(new Uri(_memSocketURL));

                _socket.OnOpen += OnWebSocketOpen;
                _socket.OnMessage += OnMessageReceived;
                _socket.OnBinary += OnBinaryMessageReceived;
                _socket.OnClosed += OnWebSocketClosed;
                _socket.OnError += OnError;
                _socket.OnErrorDesc += OnErrorDesc;

                _socket.Open();
                SendEvent("hello");
            }

            private void OnWebSocketOpen(WebSocket webSocket)
            {
                Debug.Log("WebSocket Open!");
            }
            private void OnMessageReceived(WebSocket webSocket, string message)
            {
                Debug.Log("Text Message received from server: " + message);
            }
            private void OnBinaryMessageReceived(WebSocket webSocket, byte[] message)
            {
                Debug.Log("Binary Message received from server. Length: " + message.Length);
            }
            private void OnWebSocketClosed(WebSocket webSocket, UInt16 code, string message)
            {
                Debug.Log("WebSocket Closed!");
            }
            private void OnError(WebSocket ws, Exception ex)
            {
                string errorMsg = string.Empty;
                if (ws.InternalRequest.Response != null)
                    errorMsg = string.Format("Status Code from Server: {0} and Message: {1}",
                    ws.InternalRequest.Response.StatusCode,
                    ws.InternalRequest.Response.Message);
                Debug.Log("An error occured: " + (ex != null ? ex.Message : "Unknown: " + errorMsg));
            }
            void OnErrorDesc(WebSocket ws, string error)
            {
                Debug.Log("Error: " + error);
            }

            void ReceiveEvent(Socket socket, Packet packet, params object[] args)
            {
                for (int i=0; i < args.Length; ++i)
                {
                    Debug.Log("[SocketIO] Event arg(" + i + ") : " + args[i]);
                }
            }

            void SendEvent(string msg) //, params object[] args)
            {
                _socket.Send(msg);
                //_manager.Socket.Emit(msg);
                Debug.Log("[SocketIO] Emit Event " + msg);
            }*/
        }
    }
}
