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
        [SerializeField]
        private int mPort = 13579;  // 0 - 65535

        [Tooltip("IO buffer size.")]
        [SerializeField]
        private int mBufferSize = 16;

        [Header("Custom Settings")]

        [Tooltip("If enabled, use streaming asset path instead of custom path.")]
        [SerializeField]
        private bool mUseStreamingAssetsPath = false;

        [Tooltip("The root directory for the http server to host.")]
        [SerializeField]
        private string mPath;

        [Tooltip("Controller used to interact with the server.")]
        [SerializeField]
        private MonoBehaviour mController = null;

        [Header("Other Settings")]

        [Tooltip("Start the server in `Awake` time.")]
        [SerializeField]
        private bool mStartOnAwake = false;

        [Tooltip("Open the url after the server has started.")]
        [SerializeField]
        private bool mOpenUrlAfterStarted = false;

        /* Setter & Getter */

        public int port { get { return mPort; } }

        public string path
        {
            get
            {
                if (mUseStreamingAssetsPath)
                {
                    return Application.streamingAssetsPath;
                }

                return mPath;
            }
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
                    Debug.LogWarning("Server has already started in port: " + mPort);
                    return;
                }

                Stop();
            }

            mServer = new HTTP_Server(path, port, mBufferSize, mController);

            mServer.OnJsonSerialized += (result) =>
            {
#if UseLitJson
                return LitJson.JsonMapper.ToJson(result);
#else
                return JsonUtility.ToJson(result);
#endif
            };

            Debug.Log("Server started in port: " + mPort);

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
            return GetLocalIPAddress() + ":" + mPort;
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
