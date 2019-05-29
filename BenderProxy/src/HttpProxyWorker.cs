using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BenderProxy.Logging;
using BenderProxy.Utils;

namespace BenderProxy
{
    internal class HttpProxyWorker
    {
        private readonly HttpProxy _httpProxy;
        private readonly TcpListener _listener;

        private readonly ISet<Socket> _openSockets;
        private Thread _acceptSocketThread;
        private bool _shuttingDown;

        public HttpProxyWorker(IPEndPoint proxyEndPoint, HttpProxy httpProxy)
            : this(new TcpListener(proxyEndPoint), httpProxy)
        {
        }

        private HttpProxyWorker(TcpListener listener, HttpProxy httpProxy)
        {
            _openSockets = new HashSet<Socket>();
            _httpProxy = httpProxy;
            _listener = listener;
            _httpProxy.Log += OnHttpProxyLog;
        }

        public IPEndPoint LocalEndPoint
        {
            get { return _listener.LocalEndpoint as IPEndPoint; }
        }

        public HttpProxy Proxy
        {
            get { return _httpProxy; }
        }

        public bool Active
        {
            get { return _listener.Server.IsBound; }
        }

        public event EventHandler<LogEventArgs> Log;

        public void Start(EventWaitHandle startEventHandle)
        {
            ContractUtils.Requires<ArgumentNullException>(startEventHandle != null, "startEventHandle");

            if (!Active)
            {
                var waitHandle = new ManualResetEventSlim(false);

                lock (_listener)
                {
                    _shuttingDown = false;
                    
                    _listener.Start();

                    _acceptSocketThread = new Thread(AcceptSocketLoop);
                    
                    _acceptSocketThread.Start(waitHandle);
                }

                waitHandle.Wait();

                OnLog(LogLevel.Debug, "started on {0}", LocalEndPoint);
            }

            startEventHandle.Set();
        }

        private void AcceptSocketLoop(Object startEvent)
        {
            var resetHandle = new AutoResetEvent(false);

            if (startEvent is ManualResetEventSlim)
            {
                (startEvent as ManualResetEventSlim).Set();
            }

            do
            {
                OnLog(LogLevel.Debug, "Begin Accept Socket");

                try
                {
                    _listener.BeginAcceptSocket(AcceptClientSocket, resetHandle);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    if (_shuttingDown)
                    {
                        break;
                    }
                }
            } while (!_shuttingDown && resetHandle.WaitOne());

            OnLog(LogLevel.Debug, "Socket Accept loop finished");
        }

        private void AcceptClientSocket(IAsyncResult ar)
        {
            Socket socket = null;

            try
            {
                socket = _listener.EndAcceptSocket(ar);
            }
            catch
            {
                if (socket != null)
                {
                    socket.Close();
                }

                return;
            }
            finally
            {
                var resetEvent = ar.AsyncState as AutoResetEvent;

                if (resetEvent != null)
                {
                    resetEvent.Set();
                }   
            }

            ThreadPool.QueueUserWorkItem(ignore => HandleSocket(socket));

            OnLog(LogLevel.Debug, "End Accept Socket");
        }

        private void HandleSocket(Socket socket)
        {
            lock (_openSockets)
            {
                _openSockets.Add(socket);
            }

            try
            {
                _httpProxy.HandleClient(socket);
            }
            catch (Exception ex)
            {
                OnLog(LogLevel.Debug, "Failed to handle client request", ex);
            }

            socket.Close();

            lock (_openSockets)
            {
                _openSockets.Remove(socket);
            }
        }

        public void Stop()
        {
            lock (_listener)
            {
                if (!Active)
                {
                    return;
                }

                _shuttingDown = true;

                try
                {
                    if (!_acceptSocketThread.Join(TimeSpan.FromSeconds(5)))
                    {
                        _acceptSocketThread.Abort();
                    }
                }
                catch (Exception ex)
                {
                    OnLog(LogLevel.Debug, "Error occured while stopping", ex);
                }

                try
                {
                    _listener.Stop();
                }
                catch (Exception ex)
                {
                    OnLog(LogLevel.Debug, "Error while stopping", ex);
                }
            }

            lock (_openSockets)
            {
                foreach (Socket socket in _openSockets)
                {
                    socket.Close();
                }

                _openSockets.Clear();
            }

            _httpProxy.Dispose();

            OnLog(LogLevel.Debug, "stopped on {0}", LocalEndPoint.Address);
        }

        protected void OnLog(LogLevel level, string template, params object[] args)
        {
            if (this.Log != null)
            {
                LogEventArgs e = new LogEventArgs(typeof(HttpProxy), level, template, args);
                this.Log(this, e);
            }
        }

        private void OnHttpProxyLog(object sender, LogEventArgs e)
        {
            // Bubble up the log event from components.
            if (this.Log != null)
            {
                this.Log(sender, e);
            }
        }
    }
}