using System;
using System.Text;
using BenderProxy.Utils;

namespace BenderProxy.Headers {

    public class HttpMessageHeader {

        private const string ChunkedTransferEncoding = "chunked";

        private HttpHeaders _httpHeaders;

        private string _startLine;

        public HttpMessageHeader(string startLine, HttpHeaders headers)
        {
            ContractUtils.Requires<ArgumentNullException>(!string.IsNullOrEmpty(startLine), "startLine");
            ContractUtils.Requires<ArgumentNullException>(headers != null, "headers");

            _startLine = startLine;
            _httpHeaders = headers;
        }

        public HttpMessageHeader(string startLine = null) {
            _startLine = startLine ?? string.Empty;
            _httpHeaders = new HttpHeaders();
        }

        public bool Chunked {
            get { return (GeneralHeaders.TransferEncoding ?? string.Empty).Contains(ChunkedTransferEncoding); }
        }

        public virtual string StartLine {
            get { return _startLine; }
            set { _startLine = value; }
        }

        public HttpHeaders Headers {
            get { return _httpHeaders; }
            set { _httpHeaders = value ?? new HttpHeaders(); }
        }

        public GeneralHeaders GeneralHeaders {
            get { return new GeneralHeaders(Headers); }
        }

        public EntityHeaders EntityHeaders {
            get { return new EntityHeaders(Headers); }
        }
        
        public override string ToString()
        {
            var sb = new StringBuilder(_startLine).AppendLine();

            foreach (var headerLine in Headers.Lines)
            {
                sb.AppendLine(headerLine);
            }

            return sb.ToString();
        }

    }

}