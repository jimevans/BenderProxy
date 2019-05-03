using System;

namespace BenderProxy.Headers {

    public class GeneralHeaders {

        public const string PragmaHeader = "Pragma";

        public const string ConnectionHeader = "Connection";

        public const string ProxyConnectionHeader = "Proxy-Connection";

        public const string CacheControlHeader = "Cache-Control";

        public const string TransferEncodingHeader = "Transfer-Encoding";

        public const string TrailerHeader = "Trailer";

        public readonly HttpHeaders Headers;

        public GeneralHeaders(HttpHeaders headers) {
            Headers = headers;
        }

        /// <summary>
        ///     Cache-Control header value
        /// </summary>
        public string CacheControl {
            get { return Headers[CacheControlHeader]; }
            set { Headers[CacheControlHeader] = value; }
        }

        /// <summary>
        ///     Connection header value
        /// </summary>
        public string Connection {
            get { return Headers[ConnectionHeader]; }
            set { Headers[ConnectionHeader] = value; }
        }

        public string ProxyConnection
        {
            get { return Headers[ProxyConnectionHeader]; }
            set { Headers[ProxyConnectionHeader] = value; }
        }

        /// <summary>
        ///     Pragma header value
        /// </summary>
        public string Pragma {
            get { return Headers[PragmaHeader]; }
            set { Headers[PragmaHeader] = value; }
        }

        public string TransferEncoding {
            get { return Headers[TransferEncodingHeader]; }
            set { Headers[TransferEncodingHeader] = value; }
        }

        public string Trailer {
            get { return Headers[TrailerHeader]; }
            set { Headers[TrailerHeader] = value; }
        }

    }

}