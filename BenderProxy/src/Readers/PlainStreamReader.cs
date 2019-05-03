using System;
using System.IO;

namespace BenderProxy.Utils {

    internal class PlainStreamReader : TextReader {

        private const int EmptyBuffer = int.MinValue;
        private int _lastPeek = EmptyBuffer;
        private int _lastRead = EmptyBuffer;
        private readonly Stream _stream;

        public PlainStreamReader(Stream stream) {
            _stream = stream;
        }

        public bool EndOfStream {
            get { return _lastRead == -1; }
        }

        public override int Read() {
            if (EndOfStream) {
                throw new EndOfStreamException();
            }

            if (_lastPeek == EmptyBuffer) {
                return _lastRead = _stream.ReadByte();
            }

            _lastPeek = EmptyBuffer;

            return _lastRead;
        }

        public override int Peek() {
            return _lastPeek == EmptyBuffer ? _lastPeek = Read() : _lastPeek;
        }

    }

}