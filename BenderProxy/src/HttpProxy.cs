using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BenderProxy.Headers;
using BenderProxy.Logging;
using BenderProxy.Readers;
using BenderProxy.Utils;
using BenderProxy.Writers;
using HttpRequestHeader = BenderProxy.Headers.HttpRequestHeader;
using HttpResponseHeader = BenderProxy.Headers.HttpResponseHeader;

namespace BenderProxy
{
    /// <summary>
    ///     Process incoming HTTP request and provides interface for intercepting it at different stages.
    /// </summary>
    public class HttpProxy : IDisposable
    {
        protected const int DefaultHttpPort = 80;

        private static readonly TimeSpan DefaultCommunicationTimeout = TimeSpan.FromSeconds(1);

        private readonly int _defaultPort;

        private readonly Dictionary<DnsEndPoint, Socket> activeSockets = new Dictionary<DnsEndPoint, Socket>();

        private readonly ActionWrapper<ProcessingContext> _onProcessingCompleteWrapper =
            new ActionWrapper<ProcessingContext>();

        private readonly ActionWrapper<ProcessingContext> _onRequestReceivedWrapper =
            new ActionWrapper<ProcessingContext>();

        private readonly ActionWrapper<ProcessingContext> _onResponseReceivedWrapper =
            new ActionWrapper<ProcessingContext>();

        private readonly ActionWrapper<ProcessingContext> _onResponseSentWrapper =
            new ActionWrapper<ProcessingContext>();

        private readonly ActionWrapper<ProcessingContext> _onServerConnectedWrapper =
            new ActionWrapper<ProcessingContext>();

        private readonly ProcessingPipeline _pipeline;

        private bool isDisposed = false;

        /// <summary>
        ///     Creates new instance of <see cref="HttpProxy" /> using default HTTP port (80).
        /// </summary>
        public HttpProxy() : this(DefaultHttpPort)
        {
        }

        /// <summary>
        ///     Creates new instance of <see cref="HttpProxy" /> using provided default port and internal buffer size.
        /// </summary>
        /// <param name="defaultPort">
        ///     Port number on destination server which will be used if not specified in request
        /// </param>
        public HttpProxy(int defaultPort)
        {
            if (defaultPort < IPEndPoint.MinPort || defaultPort > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultPort));
            }

            _defaultPort = defaultPort;

            ClientReadTimeout = DefaultCommunicationTimeout;
            ClientWriteTimeout = DefaultCommunicationTimeout;
            ServerReadTimeout = DefaultCommunicationTimeout;
            ServerWriteTimeout = DefaultCommunicationTimeout;

            _pipeline = new ProcessingPipeline(new Dictionary<ProcessingStage, Action<ProcessingContext>>
            {
                {ProcessingStage.ReceiveRequest, ReceiveRequest + _onRequestReceivedWrapper},
                {ProcessingStage.ConnectToServer, ConnectToServer + _onServerConnectedWrapper},
                {ProcessingStage.ReceiveResponse, ReceiveResponse + _onResponseReceivedWrapper},
                {ProcessingStage.Completed, CompleteProcessing + _onProcessingCompleteWrapper},
                {ProcessingStage.SendResponse, SendResponse + _onResponseSentWrapper}
            });
        }

        public event EventHandler<LogEventArgs> Log;

        /// <summary>
        ///     Called when all other stages of request processing are done.
        ///     All <see cref="ProcessingContext" /> information should be available now.
        /// </summary>
        public Action<ProcessingContext> OnProcessingComplete
        {
            get { return _onProcessingCompleteWrapper.Action; }
            set { _onProcessingCompleteWrapper.Action = value; }
        }

        /// <summary>
        ///     Called when request from client is received by proxy.
        ///     <see cref="ProcessingContext.RequestHeader" /> and <see cref="ProcessingContext.ClientStream" /> are available at
        ///     this stage.
        /// </summary>
        public Action<ProcessingContext> OnRequestReceived
        {
            get { return _onRequestReceivedWrapper.Action; }
            set { _onRequestReceivedWrapper.Action = value; }
        }

        /// <summary>
        ///     Called when response from destination server is received by proxy.
        ///     <see cref="ProcessingContext.ResponseHeader" /> is added at this stage.
        /// </summary>
        public Action<ProcessingContext> OnResponseReceived
        {
            get { return _onResponseReceivedWrapper.Action; }
            set { _onResponseReceivedWrapper.Action = value; }
        }

        /// <summary>
        ///     Called when server response has been relayed to client.
        ///     All <see cref="ProcessingContext" /> information should be available.
        /// </summary>
        public Action<ProcessingContext> OnResponseSent
        {
            get { return _onResponseSentWrapper.Action; }
            set { _onResponseSentWrapper.Action = value; }
        }

        /// <summary>
        ///     Called when proxy has established connection to destination server.
        ///     <see cref="ProcessingContext.ServerEndPoint" /> and <see cref="ProcessingContext.ServerStream" /> are defined at
        ///     this stage.
        /// </summary>
        public Action<ProcessingContext> OnServerConnected
        {
            get { return _onServerConnectedWrapper.Action; }
            set { _onServerConnectedWrapper.Action = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the proxy should attempt
        /// to use a persistent socket connection with the web server.
        /// </summary>
        public bool EnableKeepAlive { get; set; } = true;

        /// <summary>
        ///     Client socket read timeout
        /// </summary>
        public TimeSpan ClientReadTimeout { get; set; }

        /// <summary>
        ///     Client socket write timeout
        /// </summary>
        public TimeSpan ClientWriteTimeout { get; set; }

        /// <summary>
        ///     Server socket read timeout
        /// </summary>
        public TimeSpan ServerReadTimeout { get; set; }

        /// <summary>
        ///     Server socket write timeout
        /// </summary>
        public TimeSpan ServerWriteTimeout { get; set; }

        /// <summary>
        ///     Accept client connection, create <see cref="ProcessingContext" /> and <see cref="ProcessingContext.ClientStream" />
        ///     and start processing request.
        /// </summary>
        /// <param name="clientSocket">Socket opened by the client</param>
        public void HandleClient(Socket clientSocket)
        {
            ContractUtils.Requires<ArgumentNullException>(clientSocket != null, "clientSocket");

            var context = new ProcessingContext
            {
                ClientSocket = clientSocket,
                ClientStream = new NetworkStream(clientSocket, true)
                {
                    ReadTimeout = (int) ClientReadTimeout.TotalMilliseconds,
                    WriteTimeout = (int) ClientWriteTimeout.TotalMilliseconds
                }
            };

            _pipeline.Start(context);
            
            if (context.Exception != null)
            {
                var errorMessage = new StringBuilder("Request processing failed.").AppendLine();

                if (context.RequestHeader != null)
                {
                    errorMessage.AppendLine("Request:");
                    errorMessage.WriteHttpTrace(context.RequestHeader);
                }

                if (context.ResponseHeader != null)
                {
                    errorMessage.AppendLine("Response:");
                    errorMessage.WriteHttpTrace(context.ResponseHeader);
                }

                errorMessage.AppendLine("Exception:");
                errorMessage.AppendLine(context.Exception.ToString());

                OnLog(LogLevel.Error, errorMessage.ToString());
            }
        }

        /// <summary>
        /// Releases resources used by this <see cref="HttpProxy"/> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     Read <see cref="ProcessingContext.RequestHeader" /> from <see cref="ProcessingContext.ClientStream" />.
        ///     <see cref="ProcessingContext.ClientStream" /> should be defined at this point.
        /// </summary>
        /// <param name="context">current request context</param>
        protected virtual void ReceiveRequest(ProcessingContext context)
        {
            ContractUtils.Requires<ArgumentNullException>(context != null, "context");
            ContractUtils.Requires<InvalidContextException>(context.ClientStream != null, "ClientStream");

            var headerReader = new HttpHeaderReader(new PlainStreamReader(context.ClientStream));
            headerReader.Log += this.OnComponentLog;

            try
            {
                context.RequestHeader = new HttpRequestHeader(headerReader.ReadHttpMessageHeader());

                OnLog(LogLevel.Debug, "Request Received. {0}", TraceUtils.GetHttpTrace(context.RequestHeader));

                if (context.RequestHeader.Headers.Contains(GeneralHeaders.ProxyConnectionHeader))
                {
                    context.RequestHeader.Headers.Remove(GeneralHeaders.ProxyConnectionHeader);
                }

                if (!EnableKeepAlive)
                {
                    context.RequestHeader.GeneralHeaders.Connection = "close";
                }
            }
            catch (IOException ex)
            {
                if (ex.IsSocketException(SocketError.OperationAborted, SocketError.ConnectionReset))
                {
                    OnLog(LogLevel.Warn, "Request was terminated by client. {0}", TraceUtils.GetHttpTrace(context.RequestHeader));
                } 
                else if (ex is EndOfStreamException)
                {
                    OnLog(LogLevel.Error, "Failed to read request. {0}", TraceUtils.GetHttpTrace(context.RequestHeader));
                } 
                else if(ex.IsSocketException(SocketError.TimedOut))
                {
                    OnLog(LogLevel.Warn, "Client request time out. {0}", TraceUtils.GetHttpTrace(context.RequestHeader));    
                }
                else
                {
                    throw;
                }

                context.StopProcessing();
            }
        }

        /// <summary>
        ///     Resolve <see cref="ProcessingContext.ServerEndPoint" /> based on <see cref="ProcessingContext.RequestHeader" />,
        ///     establish connection to destination server and open <see cref="ProcessingContext.ServerStream" />.
        ///     <see cref="ProcessingContext.RequestHeader" /> should be defined.
        /// </summary>
        /// <param name="context">current request context</param>
        protected virtual void ConnectToServer(ProcessingContext context)
        {
            ContractUtils.Requires<ArgumentNullException>(context != null, "context");
            ContractUtils.Requires<InvalidContextException>(context.RequestHeader != null, "RequestHeader");

            context.ServerEndPoint = DnsUtils.ResolveRequestEndpoint(context.RequestHeader, _defaultPort);

            bool requiresNewSocket = true;
            if (EnableKeepAlive && activeSockets.ContainsKey(context.ServerEndPoint))
            {
                context.ServerSocket = activeSockets[context.ServerEndPoint];
                if (context.ServerSocket.IsConnected())
                {
                    requiresNewSocket = false;
                }
                else
                {
                    context.ServerSocket.Close();
                    activeSockets.Remove(context.ServerEndPoint);
                    OnLog(LogLevel.Debug, "Server socket appears disconnected. Requiring new socket.");
                }
            }

            if (requiresNewSocket)
            {
                context.ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveTimeout = (int)ServerReadTimeout.TotalMilliseconds,
                    SendTimeout = (int)ServerWriteTimeout.TotalMilliseconds
                };

                context.ServerSocket.Connect(context.ServerEndPoint.Host, context.ServerEndPoint.Port);
                if (EnableKeepAlive)
                {
                    activeSockets[context.ServerEndPoint] = context.ServerSocket;
                }
            }

            context.ServerStream = new NetworkStream(context.ServerSocket, !EnableKeepAlive);

            OnLog(LogLevel.Debug,
                "Connection Established: {0}:{1}",
                context.ServerEndPoint.Host,
                context.ServerEndPoint.Port
            );
        }

        /// <summary>
        ///     Send <see cref="ProcessingContext.RequestHeader" /> to server,
        ///     copy rest of the <see cref="ProcessingContext.ClientStream" /> to <see cref="ProcessingContext.ServerStream" />
        ///     and read <see cref="ProcessingContext.ResponseHeader" /> from <see cref="ProcessingContext.ServerStream" />.
        ///     Expects <see cref="ProcessingContext.ServerStream" />, <see cref="ProcessingContext.RequestHeader" /> and
        ///     <see cref="ProcessingContext.ClientStream" /> to be defined.
        /// </summary>
        /// <param name="context">current request context</param>
        protected virtual void ReceiveResponse(ProcessingContext context)
        {
            ContractUtils.Requires<ArgumentNullException>(context != null, "context");
            ContractUtils.Requires<InvalidContextException>(context.ServerStream != null, "ServerStream");
            ContractUtils.Requires<InvalidContextException>(context.RequestHeader != null, "RequestHeader");
            ContractUtils.Requires<InvalidContextException>(context.ClientStream != null, "ClientStream");
            ContractUtils.Requires<InvalidContextException>(context.ClientSocket != null, "ClientSocket");

            var requestWriter = new HttpMessageWriter(context.ServerStream);
            requestWriter.Log += this.OnComponentLog;

            var responseReader = new HttpHeaderReader(new PlainStreamReader(context.ServerStream));
            responseReader.Log += this.OnComponentLog;

            try
            {
                requestWriter.Write(context.RequestHeader, context.ClientStream, context.ClientSocket.Available);
                context.ResponseHeader = new HttpResponseHeader(responseReader.ReadHttpMessageHeader());

                OnLog(LogLevel.Debug, "Response Received: {0}", TraceUtils.GetHttpTrace(context.ResponseHeader));
            }
            catch (IOException ex)
            {
                var responseWriter = new HttpResponseWriter(context.ClientStream);

                if (ex.IsSocketException(SocketError.TimedOut))
                {
                    OnLog(LogLevel.Warn, "Request to remote server has timed out. {0}", TraceUtils.GetHttpTrace(context.RequestHeader));

                    responseWriter.WriteGatewayTimeout();
                }
                else
                {
                    throw;
                }

                context.StopProcessing();
            }
        }

        /// <summary>
        ///     Send respose to <see cref="ProcessingContext.ClientStream" /> containing
        ///     <see cref="ProcessingContext.ResponseHeader" />
        ///     and rest of<see cref="ProcessingContext.ServerStream" />.
        ///     Expect <see cref="ProcessingContext.ServerStream" />, <see cref="ProcessingContext.ClientStream" /> and
        ///     <see cref="ProcessingContext.ResponseHeader" /> to be defined.
        /// </summary>
        /// <param name="context">current request context</param>
        protected virtual void SendResponse(ProcessingContext context)
        {
            ContractUtils.Requires<ArgumentNullException>(context != null, "context");
            ContractUtils.Requires<InvalidContextException>(context.ServerStream != null, "ServerStream");
            ContractUtils.Requires<InvalidContextException>(context.ResponseHeader != null, "ResponseHeader");
            ContractUtils.Requires<InvalidContextException>(context.ClientStream != null, "ClientStream");
            ContractUtils.Requires<InvalidContextException>(context.ServerSocket != null, "ServerSocket");

            var responseWriter = new HttpResponseWriter(context.ClientStream);

            try
            {
                responseWriter.Write(context.ResponseHeader, context.ServerStream, context.ServerSocket.Available);

                OnLog(LogLevel.Debug, "Response Sent. {0}", TraceUtils.GetHttpTrace(context.ResponseHeader));
            }
            catch (IOException ex)
            {
                if (ex.IsSocketException(SocketError.TimedOut))
                {
                    OnLog(LogLevel.Warn, "Request to remote server has timed out. {0}", TraceUtils.GetHttpTrace(context.RequestHeader));

                    responseWriter.WriteGatewayTimeout();
                }
                else if (ex.IsSocketException(SocketError.ConnectionReset, SocketError.ConnectionAborted))
                {
                    OnLog(LogLevel.Debug, "Request Aborted. {0}", TraceUtils.GetHttpTrace(context.RequestHeader));
                }
                else
                {
                    throw;
                }

                context.StopProcessing();
            }
        }

        /// <summary>
        ///     Close client and server connections.
        ///     Expect <see cref="ProcessingContext.ClientStream" /> and <see cref="ProcessingContext.ServerStream" /> to be
        ///     defined.
        /// </summary>
        /// <param name="context"></param>
        protected virtual void CompleteProcessing(ProcessingContext context)
        {
            ContractUtils.Requires<ArgumentNullException>(context != null, "context");

            if (context.ClientStream != null)
            {
                context.ClientStream.Close();
            }

            if (context.ServerStream != null)
            {
                context.ServerStream.Close();
            }

            OnLog(LogLevel.Debug, "[{0}] processed", context.RequestHeader.StartLine);
        }

        /// <summary>
        /// Releases managed and unmanaged resources used by this <see cref="HttpProxy"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release managed and 
        /// unmanaged resources; <see langword="false"/> to release only unmanaged
        /// resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    foreach(KeyValuePair<DnsEndPoint, Socket> pair in activeSockets)
                    {
                        pair.Value.Dispose();
                    }

                    activeSockets.Clear();
                }

                isDisposed = true;
            }
        }

        /// <summary>
        /// Raises the <see cref="Log"/> event.
        /// </summary>
        /// <param name="level">The <see cref="Logging.LogLevel"/> of the message to log.</param>
        /// <param name="template">The template string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format in the log message.</param>
        protected void OnLog(LogLevel level, string template, params object[] args)
        {
            if (this.Log != null)
            {
                LogEventArgs e = new LogEventArgs(typeof(HttpProxy), level, template, args);
                this.Log(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="Log"/> event with information from components used by this <see cref="HttpProxy"/>.
        /// </summary>
        /// <param name="sender">The object raising the <see cref="Log"/> event.</param>
        /// <param name="e">A <see cref="LogEventArgs"/> that contains the event data.</param>
        protected void OnComponentLog(object sender, LogEventArgs e)
        {
            if (this.Log != null)
            {
                this.Log(sender, e);
            }
        }
    }
}