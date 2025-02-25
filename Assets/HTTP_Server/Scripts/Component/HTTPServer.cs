using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace com.jcs090218.HTTP_Server
{
    /// <summary>
    /// HTTP server component.
    /// </summary>
    public class HTTPServer : MonoBehaviour
    {
        /* Variables */

        private HTTP_Server mServer = null;

        [Header("Socket Settings")]

        [Tooltip("Port used for the server.")]
        public int port = 13579;  // 0 - 65535

        [Tooltip("IO buffer size.")]
        public int bufferSize = 16;

        [Header("Custom Settings")]

        [Tooltip("If enabled, use streaming asset path instead of custom path.")]
        public bool useStreamingAssetsPath = false;

        [Tooltip("The root directory for the http server to host.")]
        [SerializeField]
        private string mPath = "";

        [Tooltip("Controller used to interact with the server.")]
        public MonoBehaviour controller = null;

        [Header("Other Settings")]

        [Tooltip("Start the server in `Awake` time.")]
        [SerializeField]
        private bool mStartOnAwake = false;

        [Tooltip("Open the url after the server has started.")]
        [SerializeField]
        private bool mOpenUrlAfterStarted = false;

        /* Setter & Getter */

        public string path
        {
            get
            {
                if (useStreamingAssetsPath)
                {
                    return Application.streamingAssetsPath;
                }

                return mPath;
            }

            set { mPath = value; }
        }

        /* Functions */

        private void Awake()
        {
            if (mStartOnAwake)
                StartServer();
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            Stop();
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public void StartServer(bool force = false)
        {
            if (mServer != null)
            {
                if (!force)
                {
                    Debug.LogWarning("Server has already started in port: " + port);
                    return;
                }

                Stop();
            }

            mServer = new HTTP_Server(path, port, bufferSize, controller);

            mServer.OnJsonSerialized += (result) =>
            {
#if UseLitJson
                return LitJson.JsonMapper.ToJson(result);
#else
                return JsonUtility.ToJson(result);
#endif
            };

            Debug.Log("Server started in port: " + port);

            if (mOpenUrlAfterStarted)
            {
                Application.OpenURL(GetHTTPUrl());
            }
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop()
        {
            if (mServer == null)
                return;

            mServer.Stop();
        }

        /// <summary>
        /// Get the Host IPv4 adress.
        /// </summary>
        /// <returns> IPv4 address </returns>
        public static string GetLocalIPAddress()
        {
            string host = Dns.GetHostName();

            IPHostEntry entry = Dns.GetHostEntry(host);

            foreach (IPAddress ip in entry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public string GetUrl()
        {
            return GetLocalIPAddress() + ":" + port;
        }

        /// <summary>
        /// Return the host URL.
        /// </summary>
        public string GetHTTPUrl()
        {
            return "http://" + GetUrl() + "/";
        }
    }
}
