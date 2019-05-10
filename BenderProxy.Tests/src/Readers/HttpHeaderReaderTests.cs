using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BenderProxy.Readers;
using NUnit.Framework;

namespace BenderProxy.Tests.Readers
{
    [TestFixture]
    public class HttpHeaderReaderTests
    {
        [Test]
        public void ShouldReadHttpHeaders()
        {
            var reader = new HttpHeaderReader(new StringReader(
                new StringBuilder(String.Empty)
                    .AppendLine("Cache-Control:private")
                    .AppendLine("Content-Encoding:gzip")
                    .AppendLine("Content-Length:27046")
                    .AppendLine()
                    .ToString()
                ));

            Assert.That(reader.ReadHeaders(), Is.EqualTo(new List<String>
            {
                "Cache-Control:private",
                "Content-Encoding:gzip",
                "Content-Length:27046"
            }));
        }

        [Test]
        public void ShouldReadFirstNotEmptyLine()
        {
            var reader = new HttpHeaderReader(new StringReader(
                new StringBuilder(String.Empty)
                    .AppendLine()
                    .AppendLine()
                    .AppendLine()
                    .AppendLine("HTTP/1.1 200 OK")
                    .ToString()
                ));

            Assert.That(reader.ReadFirstLine(), Is.EqualTo("HTTP/1.1 200 OK"));
        }

        [Test]
        public void ShouldReadHttpMessageHeader()
        {
            var reader = new HttpHeaderReader(new StringReader(
                new StringBuilder("HTTP/1.1 200 OK")
                    .AppendLine()
                    .AppendLine("Cache-Control:private")
                    .AppendLine("Content-Encoding:gzip")
                    .AppendLine("Content-Length:27046")
                    .AppendLine()
                    .ToString()
                ));

            var header = reader.ReadHttpMessageHeader();

            Assert.That(header.StartLine, Is.EqualTo("HTTP/1.1 200 OK"));
            Assert.That(header.GeneralHeaders.CacheControl, Is.EqualTo("private"));
            Assert.That(header.EntityHeaders.ContentEncoding, Is.EqualTo("gzip"));
            Assert.That(header.EntityHeaders.ContentLength, Is.EqualTo(27046));
        }

    }
}