using System;
using System.Text.RegularExpressions;

namespace BenderProxy.Headers {

    public class HttpResponseHeader : HttpMessageHeader {

        public const string AgeHeader = "Age";

        public const string EtagHeader = "Etag";

        public const string VaryHeader = "Vary";

        public const string ServerHeader = "Server";

        public const string LocationHeader = "Location";

        public const string RetryAfterHeader = "Retry-After";

        public const string AcceptRangesHeader = "Accept-Ranges";

        public const string WWWAuthenticateHeader = "WWW-Authenticate";
        public const string ProxyAuthenticateHeader = "Proxy-Authenticate";

        private static readonly Regex ResponseLineRegex = new Regex(
            @"HTTP/(?<version>\d\.\d)\s(?<status>\d{3})\s(?<reason>.*)", RegexOptions.Compiled
            );

        public HttpResponseHeader(HttpMessageHeader header) : base(header.StartLine, header.Headers)
        {
            StartLine = base.StartLine;
        }

        public HttpResponseHeader(int statusCode, string statusMessage, string version)
        {
            StatusCode = statusCode;
            Reason = statusMessage;
            Version = version;
        }

        public HttpResponseHeader(string startLine) : base(startLine) {
            StartLine = base.StartLine;
        }

        /// <summary>
        ///     HTTP response status code
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        ///     HTTP protocol version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///     HTTP respnse status message
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        ///     First line of HTTP response message
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     If Status-Line is invalid
        /// </exception>
        public override sealed string StartLine {
            get { return string.Format("HTTP/{0} {1} {2}", Version, StatusCode, Reason); }

            set {
                var match = ResponseLineRegex.Match(value);

                if (!match.Success) {
                    throw new ArgumentException("Ivalid Response-Line", "value");
                }

                Reason = match.Groups["reason"].Value;
                Version = match.Groups["version"].Value;
                StatusCode = int.Parse(match.Groups["status"].Value);

                base.StartLine = value;
            }
        }

        public string Age {
            get { return Headers[AgeHeader]; }
            set { Headers[AgeHeader] = value; }
        }

        public string Etag {
            get { return Headers[EtagHeader]; }
            set { Headers[EtagHeader] = value; }
        }

        public string Vary {
            get { return Headers[VaryHeader]; }
            set { Headers[VaryHeader] = value; }
        }

        public string Server {
            get { return Headers[ServerHeader]; }
            set { Headers[ServerHeader] = value; }
        }

        public string Location {
            get { return Headers[LocationHeader]; }
            set { Headers[LocationHeader] = value; }
        }

        public string RetryAfter {
            get { return Headers[RetryAfterHeader]; }
            set { Headers[RetryAfterHeader] = value; }
        }

        public string AcceptRanges {
            get { return Headers[AcceptRangesHeader]; }
            set { Headers[AcceptRangesHeader] = value; }
        }

        public string WWWAuthenticate {
            get { return Headers[WWWAuthenticateHeader]; }
            set { Headers[WWWAuthenticateHeader] = value; }
        }

        public string ProxyAuthenticate {
            get { return Headers[ProxyAuthenticateHeader]; }
            set { Headers[ProxyAuthenticateHeader] = value; }
        }
    }

}