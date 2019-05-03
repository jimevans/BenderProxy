using OpenQA.Selenium;

namespace BenderProxy.Tests.Integration {

    public class ChromeIntegratonTests : AbstractIntegrationTests {

        protected override IWebDriver CreateDriver(Proxy proxy) {
            return CreateChromeDriver(proxy);
        }

    }

}