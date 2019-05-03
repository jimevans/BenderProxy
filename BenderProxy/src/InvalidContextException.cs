using System;

namespace BenderProxy {

    public class InvalidContextException : InvalidOperationException {

        public InvalidContextException(string message) : base(string.Format("{0} should not be null", message)) {}

    }

}