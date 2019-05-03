using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using HttpRequestHeader = BenderProxy.Headers.HttpRequestHeader;

namespace BenderProxy.Utils
{
    /// <summary>
    ///     Provides methods for resolving <see cref="DnsEndPoint" />
    /// </summary>
    public static class DnsUtils
    {
        private static readonly Regex HostAndPortRegex = new Regex(@"(?<host>[\w+\.-]+):(?<port>\d+)");

        /// <summary>
        ///     Resolve destination endpoint using host header or request URI
        /// </summary>
        /// <param name="header">request header to use</param>
        /// <param name="defaultPort">which port to use if none is present in host header</param>
        /// <returns>request destination endpoint</returns>
        public static DnsEndPoint ResolveRequestEndpoint(HttpRequestHeader header, int defaultPort)
        {
            ContractUtils.Requires<ArgumentNullException>(header != null, "header");

            string hostFromHeaders = header.Host;

            return !string.IsNullOrEmpty(hostFromHeaders)
                ? ResolveEndpointFromHostHeader(hostFromHeaders, defaultPort)
                : ResolveEndpointFromURI(header.RequestURI);
        }

        /// <summary>
        ///     Resolve destination endpoint from request URI
        /// </summary>
        /// <param name="uri">request URI</param>
        /// <returns>request destination endpoint</returns>
        /// <exception cref="ArgumentException">thrown if provided string is not URI</exception>
        public static DnsEndPoint ResolveEndpointFromURI(string uri)
        {
            Uri parsedUri;

            if (Uri.TryCreate(uri, UriKind.Absolute, out parsedUri))
            {
                return new DnsEndPoint(parsedUri.Host, parsedUri.Port, AddressFamily.InterNetwork);
            }

            throw new ArgumentException(string.Format("Cannot resolve endpoint from: {0}", uri), "uri");
        }

        /// <summary>
        ///     Resolve destination ednpoint using host header
        /// </summary>
        /// <param name="host">host header value</param>
        /// <param name="defaultPort">port to use if one is absent in host header</param>
        /// <returns>request destination endpoint</returns>
        public static DnsEndPoint ResolveEndpointFromHostHeader(string host, int defaultPort)
        {
            ContractUtils.Requires<ArgumentNullException>(!string.IsNullOrWhiteSpace(host), "host");
            ContractUtils.Requires<ArgumentOutOfRangeException>(
                defaultPort > IPEndPoint.MinPort && defaultPort < IPEndPoint.MaxPort, "defaultPort");

            Match hostAndPortMatch = HostAndPortRegex.Match(host);

            if (hostAndPortMatch.Success)
            {
                return new DnsEndPoint(
                    hostAndPortMatch.Groups["host"].Value,
                    int.Parse(hostAndPortMatch.Groups["port"].Value),
                    AddressFamily.InterNetwork
                    );
            }

            return new DnsEndPoint(host, defaultPort, AddressFamily.InterNetwork);
        }
    }
}