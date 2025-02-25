using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.IO;
using System.Text;
using System.Reflection;

namespace com.jcs090218.HTTP_Server
{
    // To make Unity's JsonUtility works.
    [Serializable]
    public class VoidResult
    {
        public string msg;
    }

    /// <summary>
    /// Simple HTTP server.
    /// </summary>
    public class HTTP_Server
    {
        /* Variables */

        private const string DEFAULT_404_PAGE = @"
<head>
<style>*{
    transition: all 0.6s;
}

html {
    height: 100%;
}

body{
    font-family: 'Lato', sans-serif;
    color: #888;
    margin: 0;
}

#main{
    display: table;
    width: 100%;
    height: 100vh;
    text-align: center;
}

.fof{
	  display: table-cell;
	  vertical-align: middle;
}

.fof h1{
	  font-size: 50px;
	  display: inline-block;
	  padding-right: 12px;
	  animation: type .5s alternate infinite;
}

@keyframes type{
	  from{box-shadow: inset -3px 0px 0px #888;}
	  to{box-shadow: inset -3px 0px 0px transparent;}
}</style>
</head>
<body>
    <div id='main'>
    <div class='fof'>
        <h1>Error 404</h1>
    </div>
    </div>
</body>
";

        private static Dictionary<string, string> MIME_TYPE_MAPPINGS = new(StringComparer.InvariantCultureIgnoreCase)
        {
            #region extension to MIME type list
            { ".asf", "video/x-ms-asf" },
            { ".asx", "video/x-ms-asf" },
            { ".avi", "video/x-msvideo" },
            { ".bin", "application/octet-stream" },
            { ".cco", "application/x-cocoa" },
            { ".crt", "application/x-x509-ca-cert" },
            { ".css", "text/css" },
            { ".deb", "application/octet-stream" },
            { ".der", "application/x-x509-ca-cert" },
            { ".dll", "application/octet-stream" },
            { ".dmg", "application/octet-stream" },
            { ".ear", "application/java-archive" },
            { ".eot", "application/octet-stream" },
            { ".exe", "application/octet-stream" },
            { ".flv", "video/x-flv" },
            { ".gif", "image/gif" },
            { ".hqx", "application/mac-binhex40" },
            { ".htc", "text/x-component" },
            { ".htm", "text/html" },
            { ".html", "text/html" },
            { ".ico", "image/x-icon" },
            { ".img", "application/octet-stream" },
            { ".svg", "image/svg+xml" },
            { ".iso", "application/octet-stream" },
            { ".jar", "application/java-archive" },
            { ".jardiff", "application/x-java-archive-diff" },
            { ".jng", "image/x-jng" },
            { ".jnlp", "application/x-java-jnlp-file" },
            { ".jpeg", "image/jpeg" },
            { ".jpg", "image/jpeg" },
            { ".js", "application/x-javascript" },
            { ".mml", "text/mathml" },
            { ".mng", "video/x-mng" },
            { ".mov", "video/quicktime" },
            { ".mp3", "audio/mpeg" },
            { ".mpeg", "video/mpeg" },
            { ".mp4", "video/mp4" },
            { ".mpg", "video/mpeg" },
            { ".msi", "application/octet-stream" },
            { ".msm", "application/octet-stream" },
            { ".msp", "application/octet-stream" },
            { ".pdb", "application/x-pilot" },
            { ".pdf", "application/pdf" },
            { ".pem", "application/x-x509-ca-cert" },
            { ".pl", "application/x-perl" },
            { ".pm", "application/x-perl" },
            { ".png", "image/png" },
            { ".prc", "application/x-pilot" },
            { ".ra", "audio/x-realaudio" },
            { ".rar", "application/x-rar-compressed" },
            { ".rpm", "application/x-redhat-package-manager" },
            { ".rss", "text/xml" },
            { ".run", "application/x-makeself" },
            { ".sea", "application/x-sea" },
            { ".shtml", "text/html" },
            { ".sit", "application/x-stuffit" },
            { ".swf", "application/x-shockwave-flash" },
            { ".tcl", "application/x-tcl" },
            { ".tk", "application/x-tcl" },
            { ".txt", "text/plain" },
            { ".war", "application/java-archive" },
            { ".wbmp", "image/vnd.wap.wbmp" },
            { ".wmv", "video/x-ms-wmv" },
            { ".xml", "text/xml" },
            { ".xpi", "application/x-xpinstall" },
            { ".zip", "application/zip" },
            #endregion
        };

        public Func<object, string> OnJsonSerialized = null;

        private int mBuffSize = 16;  // Default to `16`

        private Object mMethodController = null;

        private readonly string[] INDEX_FILES =
        {
            "index.html",
            "index.htm",
            "default.html",
            "default.htm"
        };

        private Thread mTServer = null;

        private string mRootDir = null;

        private HttpListener mListener = null;

        private int mPort = -1;

        /* Setter & Getter */

        /* Functions */

        public HTTP_Server(string path, int port, int buffer, Object controller)
            : this(path, port, buffer)
        {
            this.mMethodController = controller;
        }

        public HTTP_Server(string path, int port, int buffer)
        {
            mBuffSize = buffer;
            Init(path, port);
        }

        private void Init(string path, int port)
        {
            this.mRootDir = path;
            this.mPort = port;

            // Start listening.
            mTServer = new Thread(this.Listen);
            mTServer.Start();
        }

        private void Listen()
        {
            mListener = new HttpListener();
            mListener.Prefixes.Add("http://*:" + mPort.ToString() + "/");
            mListener.Start();

            while (true)
            {
                try
                {
                    HttpListenerContext context = mListener.GetContext();
                    Process(context);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.Log(ex);
                }
            }
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop()
        {
            if (mTServer != null)
                mTServer.Abort();

            if (mListener != null)
                mListener.Stop();
        }

        /// <summary>
        /// Process the HTTP context.
        /// </summary>
        private void Process(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;
            filename = filename.Substring(1);

            if (string.IsNullOrEmpty(filename))
            {
                foreach (string indexFile in INDEX_FILES)
                {
                    string file = Path.Combine(mRootDir, indexFile);

                    if (File.Exists(file))
                    {
                        filename = indexFile;
                        break;
                    }
                }
            }

            filename = Path.Combine(mRootDir, filename);

            var namedParameters = new Dictionary<string, object>();

            // Handle if file not found.
            if (!string.IsNullOrEmpty(context.Request.Url.Query))
            {
                UnityEngine.Debug.Log(context.Request.Url.Query);

                var query = context.Request.Url.Query.Replace("?", "").Split('&');

                foreach (var item in query)
                {
                    var t = item.Split('=');

                    namedParameters.Add(t[0], t[1]);
                }
            }

            var method = TryParseToController(context.Request.Url);

            if (File.Exists(filename))
            {
                TryServeFile();
            }
            // An ASP.Net MVC like controller route
            else if (method != null)
            {
                context.Response.ContentType = "application/json";

                object result = null;
                try
                {
                    result = method.InvokeWithNamedParameters(mMethodController, namedParameters);
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    UnityEngine.Debug.LogError(ex);

                    context.Response.StatusDescription = ex.Message;
                    goto WebResponse;
                }
                if (result == null)
                {
                    result = new VoidResult { msg = "Success" };
                }

                string json = "";
                if (OnJsonSerialized == null)
                {
                    UnityEngine.Debug.LogError("There is no JsonSerialize delegate regist on SimpleHTTPServer.OnJsonSerialized");
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.StatusDescription = "There is no JsonSerialize delegate regist on SimpleHTTPServer.OnJsonSerialized";
                    goto WebResponse;
                }
                else
                {
                    json = OnJsonSerialized.Invoke(result);
                }

                byte[] jsonByte = Encoding.UTF8.GetBytes(json);

                context.Response.ContentLength64 = jsonByte.Length;

                Stream jsonStream = new MemoryStream(jsonByte);

                byte[] buffer = new byte[1024 * mBuffSize];
                int nbytes = -1;

                while ((nbytes = jsonStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    context.Response.OutputStream.Write(buffer, 0, nbytes);
                }

                jsonStream.Close();
            }
            else
            {
                byte[] resultByte = Encoding.UTF8.GetBytes(DEFAULT_404_PAGE);

                Stream resultStream = new MemoryStream(resultByte);

                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.ContentType = "text/html";
                context.Response.ContentLength64 = resultByte.Length;
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));

                byte[] buffer = new byte[1024 * mBuffSize];
                int nbytes = -1;

                while ((nbytes = resultStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    context.Response.OutputStream.Write(buffer, 0, nbytes);
                }

                resultStream.Close();
            }

        WebResponse:
            context.Response.OutputStream.Flush();
            context.Response.OutputStream.Close();

            void TryServeFile()
            {
                try
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    Stream input = new FileStream(filename, FileMode.Open, FileAccess.Read);

                    // Adding permanent http response headers
                    string mime;
                    context.Response.ContentType = MIME_TYPE_MAPPINGS.TryGetValue(Path.GetExtension(filename), out mime) ? mime : "application/octet-stream";
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));

                    byte[] buffer = new byte[1024 * mBuffSize];
                    int nbytes = -1;

                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        context.Response.OutputStream.Write(buffer, 0, nbytes);
                    }
                    input.Close();

                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    UnityEngine.Debug.LogError(ex);
                    context.Response.StatusDescription = ex.Message;
                }
            }
        }

        private MethodInfo TryParseToController(Uri uri)
        {
            if (uri.Segments.Length <= 1)
            {
                return null;
            }
            string methodName = uri.Segments[1].Replace("/", "");

            MethodInfo method = null;

            try
            {
                method = mMethodController.GetType().GetMethod(methodName);
            }
            catch
            {
                method = null;
            }

            return method;
        }
    }
}
