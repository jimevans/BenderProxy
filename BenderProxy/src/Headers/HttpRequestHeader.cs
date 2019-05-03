using System;
using System.Text;
using System.Text.RegularExpressions;

namespace BenderProxy.Headers {

    public class HttpRequestHeader : HttpMessageHeader {

        public const string TEHeader = "TE";

        public const string RangeHeader = "Range";

        public const string FromHeader = "From";
        public const string HostHeader = "Host";
        public const string RefererHeader = "Referer";
        public const string ExpectHeader = "Expect";

        public const string UserAgentHeader = "User-Agent";

        public const string MaxForwardsHeader = "Max-Forwards";

        public const string AuthorizationHeader = "Authorization";
        public const string ProxyAuthorizationHeader = "Proxy-Authorization";

        public const string AcceptHeader = "Accept";
        public const string AcceptCharsetHeader = "Accept-Charset";
        public const string AcceptEncodingHeader = "Accept-Encoding";
        public const string AcceptLanguageHeader = "Accept-Language";

        public const string IfMatchHeader = "If-Match";
        public const string IfRangeHeader = "If-Range";
        public const string IfNoneMatchHeader = "If-None-Match";
        public const string IfModifiedSinceHeader = "If-Modified-Since";
        public const string IfUnmodifiedSinceHeader = "If-Unmodified-Since";

        private static readonly Regex RequestLineRegex = new Regex(
            @"(?<method>\w+)\s(?<uri>.+)\sHTTP/(?<version>\d\.\d)", RegexOptions.Compiled
            );

        /// <summary>
        ///     Convert generic <see cref="HttpMessageHeader"/> to <see cref="HttpResponseHeader"/>
        /// </summary>
        /// <param name="header">generic HTTP header</param>
        /// <returns>HTTP request message header</returns>
        public HttpRequestHeader(HttpMessageHeader header) : base(header.StartLine, header.Headers)
        {
            StartLine = header.StartLine;
        }

        public HttpRequestHeader(string startLine = null) : base(startLine) {
            StartLine = base.StartLine;
        }

        public RequestMethodTypes MethodType {
            get {
                RequestMethodTypes methodType;

                var rawHttpMethod = Method;

                if (RequestMethodTypes.TryParse(Method, false, out methodType)) {
                    return methodType;
                }

                throw new InvalidOperationException(string.Format("Unknown method type: [{0}]", rawHttpMethod));
            }

            set {
                Method = Enum.GetName(typeof(RequestMethodTypes), value);
            }
        }

        /// <summary>
        ///     Request method
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        ///     Request path
        /// </summary>
        public string RequestURI { get; set; }

        /// <summary>
        ///     HTTP protocol version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///     First line of HTTP response message
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     If Request-Line is invalid
        /// </exception>
        public override sealed string StartLine {
            get { return string.Format("{0} {1} HTTP/{2}", Method, RequestURI, Version); }

            set {
                var match = RequestLineRegex.Match(value);

                if (!match.Success) {
                    throw new ArgumentException("Ivalid Response-Line", "value");
                }

                RequestURI = match.Groups["uri"].Value;
                Method = match.Groups["method"].Value;
                Version = match.Groups["version"].Value;

                base.StartLine = value;
            }

        }

        public override string ToString() {
            return new StringBuilder()
                .AppendLine(StartLine)
                .AppendLine(Headers.ToString())
                .ToString();
        }

        /// <summary>
        ///     Host header value
        /// </summary>
        public string Host {
            get { return Headers[HostHeader]; }
            set { Headers[HostHeader] = value; }
        }

        /// <summary>
        ///     Referer header value
        /// </summary>
        public string Referer {
            get { return Headers[RefererHeader]; }
            set { Headers[RefererHeader] = value; }
        }

        public string TE {
            get { return Headers[TEHeader]; }
            set { Headers[TEHeader] = value; }
        }

        public string Range {
            get { return Headers[RangeHeader]; }
            set { Headers[RangeHeader] = value; }
        }

        public string From {
            get { return Headers[FromHeader]; }
            set { Headers[FromHeader] = value; }
        }

        public string Expect {
            get { return Headers[ExpectHeader]; }
            set { Headers[ExpectHeader] = value; }
        }

        public string UserAgent {
            get { return Headers[UserAgentHeader]; }
            set { Headers[UserAgentHeader] = value; }
        }

        public string MaxForwards {
            get { return Headers[MaxForwardsHeader]; }
            set { Headers[MaxForwardsHeader] = value; }
        }

        public string Authorization {
            get { return Headers[AuthorizationHeader]; }
            set { Headers[AuthorizationHeader] = value; }
        }

        public string ProxyAuthorization {
            get { return Headers[ProxyAuthorizationHeader]; }
            set { Headers[ProxyAuthorizationHeader] = value; }
        }

        public string Accept {
            get { return Headers[AcceptHeader]; }
            set { Headers[AcceptHeader] = value; }
        }

        public string AcceptCharset {
            get { return Headers[AcceptCharsetHeader]; }
            set { Headers[AcceptCharsetHeader] = value; }
        }

        public string AcceptEncoding {
            get { return Headers[AcceptEncodingHeader]; }
            set { Headers[AcceptEncodingHeader] = value; }
        }

        public string AcceptLanguage {
            get { return Headers[AcceptLanguageHeader]; }
            set { Headers[AcceptLanguageHeader] = value; }
        }

        public string IfMatch {
            get { return Headers[IfMatchHeader]; }
            set { Headers[IfMatchHeader] = value; }
        }

        public string IfRange {
            get { return Headers[IfRangeHeader]; }
            set { Headers[IfRangeHeader] = value; }
        }

        public string IfNoneMatch {
            get { return Headers[IfNoneMatchHeader]; }
            set { Headers[IfNoneMatchHeader] = value; }
        }

        public string IfModifiedSince {
            get { return Headers[IfModifiedSinceHeader]; }
            set { Headers[IfModifiedSinceHeader] = value; }
        }

        public string IfUnmodifiedSince {
            get { return Headers[IfUnmodifiedSinceHeader]; }
            set { Headers[IfUnmodifiedSinceHeader] = value; }
        }

    }

}