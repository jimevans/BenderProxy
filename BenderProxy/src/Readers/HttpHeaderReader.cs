using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BenderProxy.Headers;
using BenderProxy.Logging;

namespace BenderProxy.Readers
{
    /// <summary>
    ///     Read HTTP message entities from underlying reader
    /// </summary>
    public class HttpHeaderReader
    {
        private readonly TextReader _reader;

        /// <summary>
        ///     Create new instance of <see cref="HttpHeaderReader"/> 
        /// </summary>
        /// <param name="reader">used for actual reading</param>
        public HttpHeaderReader(TextReader reader)
        {
            _reader = reader;
        }

        public event EventHandler<LogEventArgs> Log;

        /// <summary>
        ///     Read next not empty line.
        ///     Can be used for reading request line and status line from HTTP messages.
        ///     Also can be used for reading chunk length of chunked message body.
        /// </summary>
        /// <returns>firts not empty line</returns>
        public string ReadFirstLine()
        {
            var firstLine = string.Empty;

            while (string.IsNullOrWhiteSpace(firstLine))
            {
                firstLine = _reader.ReadLine();
            }

            return firstLine;
        }

        /// <summary>
        ///     Read size of next chunked HTTP message part
        /// </summary>
        /// <returns>next chunk size</returns>
        public int ReadNextChunkSize()
        {
            int nextCharValue = _reader.Peek();
            if (nextCharValue < 0)
            {
                // At end of stream.
                return 0;
            }

            var firstLine = ReadFirstLine();

            try
            {
                return int.Parse(firstLine, NumberStyles.HexNumber);
            }
            catch
            {
                this.OnLog(LogLevel.Error, "Wrong chunk size: {0}", firstLine);
                
                throw;
            }
        }

        /// <summary>
        ///     Read HTTP headers from underlying <see cref="TextReader"/>
        /// </summary>
        /// <returns>raw header lines</returns>
        public IList<string> ReadHeaders()
        {
            var headers = new List<string>();

            for (var nextLine = _reader.ReadLine();
                !string.IsNullOrEmpty(nextLine);
                nextLine = _reader.ReadLine())
            {
                headers.Add(nextLine);
            }

            return headers;
        }

        /// <summary>
        ///     Read <see cref="HttpMessageHeader"/> from underlying reader.
        /// </summary>
        /// <returns>HTTP message header</returns>
        public HttpMessageHeader ReadHttpMessageHeader()
        {
            return new HttpMessageHeader(ReadFirstLine())
            {
                Headers = new HttpHeaders(ReadHeaders())
            };
        }

        protected void OnLog(LogLevel level, string template, params object[] args)
        {
            if (this.Log != null)
            {
                LogEventArgs e = new LogEventArgs(typeof(HttpProxy), level, template, args);
                this.Log(this, e);
            }
        }
    }
}
