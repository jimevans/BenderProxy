using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;

namespace BenderProxy.Tests.Integration {

    public abstract class IntegrationTestFixture {

        private const String CertificateName = "bender.pfx";

        private const String CertificatePass = "fry";

        static IntegrationTestFixture() {
        }

        protected IWebDriver WebDriver { get; private set; }

        protected HttpProxyServer HttpProxyServer { get; private set; }

        protected HttpProxyServer SslProxyServer { get; private set; }

        [OneTimeSetUp]
        public void SetUpProxy() {
            var socketTimeout = TimeSpan.FromSeconds(5);

            HttpProxyServer = new HttpProxyServer("localhost", new HttpProxy() {
                ClientWriteTimeout = socketTimeout,
                ServerWriteTimeout = socketTimeout,
                ClientReadTimeout = socketTimeout,
                ServerReadTimeout = socketTimeout
            });

            string certFile = Path.Combine(TestContext.CurrentContext.WorkDirectory, CertificateName);
            SslProxyServer = new HttpProxyServer("localhost", new SslProxy(new X509Certificate2(certFile, CertificatePass)) {
                ClientWriteTimeout = socketTimeout,
                ServerWriteTimeout = socketTimeout,
                ClientReadTimeout = socketTimeout,
                ServerReadTimeout = socketTimeout
            });

            WaitHandle.WaitAll(
                new[] {
                    HttpProxyServer.Start(),
                    SslProxyServer.Start()
                });
        }

        protected abstract IWebDriver CreateDriver(Proxy proxy);

        protected static IWebDriver CreateFirefoxDriver(Proxy proxy) {
            FirefoxOptions options = new FirefoxOptions();
            options.Proxy = proxy;
            options.AcceptInsecureCertificates = true;
            options.AddArgument("--headless");
            return new FirefoxDriver(options);
        }

        protected static IWebDriver CreateChromeDriver(Proxy proxy) {
            ChromeOptions options = new ChromeOptions();
            options.Proxy = proxy;
            options.AddArgument("--headless");
            return new ChromeDriver(options);
        }

        [SetUp]
        public void SetUpBrowser() {
            WebDriver = CreateDriver(
                new Proxy {
                    HttpProxy = String.Format("{0}:{1}", HttpProxyServer.ProxyEndPoint.Address, HttpProxyServer.ProxyEndPoint.Port),
                    SslProxy = String.Format("{0}:{1}", SslProxyServer.ProxyEndPoint.Address, SslProxyServer.ProxyEndPoint.Port),
                    Kind = ProxyKind.Manual
                });
        }

        [TearDown]
        public void CloseBrowser() {
            WebDriver.Quit();
        }

        [OneTimeTearDown]
        public void ShutdownBrowserAndProxy() {
            HttpProxyServer.Stop();
            SslProxyServer.Stop();
        }

    }

}