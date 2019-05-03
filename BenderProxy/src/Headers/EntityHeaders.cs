using System;

namespace BenderProxy.Headers {

    public sealed class EntityHeaders {

        public const string AllowHeader = "Allow";

        public const string ExpiresHeader = "Expires";

        public const string LastModifiedHeader = "Last-Modified";

        public const string ContentMD5Header = "Content-MD5";
        public const string ContentTypeHeader = "Content-Type";
        public const string ContentRangeHeader = "Content-Range";
        public const string ContentLengthHeader = "Content-Length";
        public const string ContentLanguageHeader = "Content-Language";
        public const string ContentLocationHeader = "Content-Location";
        public const string ContentEncodingHeader = "Content-Encoding";

        private readonly HttpHeaders _headers;

        public EntityHeaders(HttpHeaders headers) {
            _headers = headers;
        }

        public string Allow {
            get { return _headers[AllowHeader]; }
            set { _headers[AllowHeader] = value; }
        }

        public string Expires {
            get { return _headers[ExpiresHeader]; }
            set { _headers[ExpiresHeader] = value; }
        }

        public string LastModified {
            get { return _headers[LastModifiedHeader]; }
            set { _headers[LastModifiedHeader] = value; }
        }

        public string ContentMD5 {
            get { return _headers[ContentMD5Header]; }
            set { _headers[ContentMD5Header] = value; }
        }

        public string ContentType {
            get { return _headers[ContentTypeHeader]; }
            set { _headers[ContentTypeHeader] = value; }
        }

        public string ContentRange {
            get { return _headers[ContentRangeHeader]; }
            set { _headers[ContentRangeHeader] = value; }
        }

        public long? ContentLength {
            get {
                var contentLength = _headers[ContentLengthHeader];

                if (contentLength != null) {
                    return long.Parse(contentLength);
                }

                return null;
            }
            set { _headers[ContentLengthHeader] = value.HasValue ? value.Value.ToString() : null; }
        }

        public string ContentLanguage {
            get { return _headers[ContentLanguageHeader]; }
            set { _headers[ContentLanguageHeader] = value; }
        }

        public string ContentLocation {
            get { return _headers[ContentLocationHeader]; }
            set { _headers[ContentLocationHeader] = value; }
        }

        public string ContentEncoding {
            get { return _headers[ContentEncodingHeader]; }
            set { _headers[ContentEncodingHeader] = value; }
        }

    }

}