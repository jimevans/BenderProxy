using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BenderProxy.Logging;
using BenderProxy.Utils;

namespace BenderProxy {

    /// <summary>
    ///     Server which listens to incoming connections and delegates handling to provided <see cref="HttpProxy" />
    /// </summary>
    public class HttpProxyServer {

        private readonly HttpProxyWorker _worker;

        /// <summary>
        ///     Create server bound to given hostname and random port
        /// </summary>
        /// <param name="hostname">hostname to bind</param>
        /// <param name="httpProxy">proxy which will handle incoming requests</param>
        public HttpProxyServer(string hostname, HttpProxy httpProxy) : this(hostname, 0, httpProxy) {}

        /// <summary>
        ///     Create server bound to given hostname and port
        /// </summary>
        /// <param name="hostname">hostname to bind</param>
        /// <param name="port">port to bind</param>
        /// <param name="httpProxy">proxy which will handle incoming requests</param>
        public HttpProxyServer(string hostname, int port, HttpProxy httpProxy)
            : this(new DnsEndPoint(hostname, port, AddressFamily.InterNetwork), httpProxy) {}

        /// <summary>
        ///     Create server bound to given local endpoint
        /// </summary>
        /// <param name="proxyEndPoint">local endpoint to bind</param>
        /// <param name="httpProxy">proxy which will handle incoming requests</param>
        public HttpProxyServer(DnsEndPoint proxyEndPoint, HttpProxy httpProxy) : this(ToIPEndPoint(proxyEndPoint), httpProxy) {}

        /// <summary>
        ///     Create server bound to given local endpoint
        /// </summary>
        /// <param name="proxyEndPoint">local endpoint to bind</param>
        /// <param name="httpProxy">proxy which will handle incoming requests</param>
        public HttpProxyServer(IPEndPoint proxyEndPoint, HttpProxy httpProxy) {
            ContractUtils.Requires<ArgumentNullException>(proxyEndPoint != null, "proxyEndPoint");
            ContractUtils.Requires<ArgumentNullException>(httpProxy != null, "httpProxy");

            _worker = new HttpProxyWorker(proxyEndPoint, httpProxy);
            _worker.Log += this.OnComponentLog;
        }

        /// <summary>
        ///     Local endpoint server is bound to
        /// </summary>
        public IPEndPoint ProxyEndPoint {
            get { return _worker.LocalEndPoint; }
        }

        /// <summary>
        ///     Proxy which handles incoming request
        /// </summary>
        public HttpProxy Proxy {
            get { return _worker.Proxy; }
        }

        /// <summary>
        ///     Indicates if server is running and expecting requests
        /// </summary>
        public bool IsListening {
            get { return _worker.Active; }
        }

        public event EventHandler<LogEventArgs> Log;

        private static IPEndPoint ToIPEndPoint(DnsEndPoint proxyEndPoint) {
            ContractUtils.Requires<ArgumentNullException>(proxyEndPoint != null, "proxyEndPoint");

            var ipAddress = Dns.GetHostAddresses(proxyEndPoint.Host)
                .First(address => address.AddressFamily == AddressFamily.InterNetwork);

            return new IPEndPoint(ipAddress, proxyEndPoint.Port);
        }

        /// <summary>
        ///     Initialize server and bind it to local endpoint
        /// </summary>
        /// <returns>handle triggered once server is started</returns>
        public WaitHandle Start() {
            var startUpEvent = new ManualResetEvent(false);
            
            _worker.Start(startUpEvent);
            
            return startUpEvent;
        }

        /// <summary>
        ///     Stop listening and unbind from local enpoint
        /// </summary>
        public void Stop() {
            _worker.Stop();
        }

        protected void OnLog(LogLevel level, string template, params object[] args)
        {
            if (this.Log != null)
            {
                LogEventArgs e = new LogEventArgs(typeof(HttpProxy), level, template, args);
                this.Log(this, e);
            }
        }

        protected void OnComponentLog(object sender, LogEventArgs e)
        {
            if (this.Log != null)
            {
                this.Log(sender, e);
            }
        }
    }

}