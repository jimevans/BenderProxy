using System;
using System.IO;
using System.Text;
using BenderProxy.Headers;
using BenderProxy.Logging;
using BenderProxy.Readers;
using BenderProxy.Utils;

namespace BenderProxy.Writers
{
    /// <summary>
    ///     Writes HTTP message to underlying stream
    /// </summary>
    public class HttpMessageWriter
    {
        protected const int BufferSize = 8192;

        protected readonly Stream OutputStream;

        /// <summary>
        ///     Creates new writer instance, writing to provided stream
        /// </summary>
        /// <param name="outputStream">stream which will be written to</param>
        public HttpMessageWriter(Stream outputStream)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            OutputStream = outputStream;
        }

        public event EventHandler<LogEventArgs> Log;

        /// <summary>
        ///     Writes HTTP message to wrapped stream
        /// </summary>
        /// <param name="header">HTTP message header</param>
        /// <param name="body">HTTP message body</param>
        /// <param name="bodyLength">expected length of HTTP message body</param>
        public void Write(HttpMessageHeader header, Stream body = null, Nullable<long> bodyLength = null)
        {
            ContractUtils.Requires<ArgumentNullException>(header != null, "header");

            var writer = new StreamWriter(OutputStream, Encoding.ASCII, BufferSize, true);

            writer.WriteLine(header.StartLine);

            foreach (string headerLine in header.Headers.Lines)
            {
                writer.WriteLine(headerLine);
            }

            writer.WriteLine();
            writer.Flush();

            if (body == null)
            {
                return;
            }

            if (WriteBody(header, body, bodyLength.GetValueOrDefault(0)))
            {
                writer.WriteLine();
                writer.Flush();
            }

            writer.Dispose();
        }

        /// <summary>
        ///     Writes messag body to <seealso cref="OutputStream"/>
        /// </summary>
        /// <param name="header">HTTP message header</param>
        /// <param name="body">HTTP message body</param>
        /// <param name="bodyLength">expected length of HTTP message body</param>
        /// <returns>True when body content was written to output stream and false otherwise</returns>
        protected virtual bool WriteBody(HttpMessageHeader header, Stream body, long bodyLength)
        {
            if (header.Chunked)
            {
                CopyChunkedMessageBody(body);
            }
            else if (header.EntityHeaders.ContentLength.HasValue && header.EntityHeaders.ContentLength.Value > 0)
            {
                CopyPlainMessageBody(body, header.EntityHeaders.ContentLength.Value);
            }
            else if (bodyLength > 0)
            {
                body.CopyTo(OutputStream);
            }
            else
            {
                OnLog(LogLevel.Debug, "Message body is empty");
                return false;
            }
            return true;
        }

        private void CopyPlainMessageBody(Stream body, long contentLength)
        {
            var buffer = new byte[BufferSize];

            long totalBytesRead = 0;

            while (totalBytesRead < contentLength)
            {
                var bytesCopied = body.Read(buffer, 0, (int) Math.Min(buffer.Length, contentLength - totalBytesRead));

                OutputStream.Write(buffer, 0, bytesCopied);

                totalBytesRead += bytesCopied;
            }
        }

        private void CopyChunk(Stream body, long chunkLength)
        {
            CopyPlainMessageBody(body, chunkLength);

            // Advance the body stream position beyond the CR-LF pair that
            // defines the end of a chunk.
            var buffer = new byte[2];
            body.Read(buffer, 0, buffer.Length);
        }

        /// <summary>
        ///     Copy chunked message body to <see cref="OutputStream" /> from given stream
        /// </summary>
        /// <param name="body">chunked HTTP message body</param>
        protected virtual void CopyChunkedMessageBody(Stream body)
        {
            var reader = new HttpHeaderReader(new PlainStreamReader(body));

            var writer = new StreamWriter(OutputStream, Encoding.ASCII, BufferSize, true);

            for (var size = reader.ReadNextChunkSize(); size != 0; size = reader.ReadNextChunkSize())
            {
                writer.WriteLine(size.ToString("X"));
                writer.Flush();

                CopyChunk(body, size);

                writer.WriteLine();
                writer.Flush();
            }

            writer.WriteLine("0");
            writer.Flush();
            writer.Dispose();
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