BenderProxy
========

BenderProxy is an extensible man in the middle HTTP proxy with SSL support. It is a fork of the otherwise
excellent [FryProxy](https://github.com/eger-geger/FryProxy) project. It was originally written
because the original author need a way to monitor and possibly stub some browser request in
Selenium tests. (It also available as [NuGet Package](https://www.nuget.org/packages/BenderProxy/)

## Examples:

Setup HTTP proxy:

```csharp
  var httpProxyServer = new HttpProxyServer("localhost", new HttpProxy());
  httpProxyServer.Start().WaitOne();
  
  // do stuff
  
  httpProxyServer.stop();
```

Setup SSL proxy:

```csharp
  var certificate = new X509Certificate2("path_to_sertificate", "password");
  var sslProxyServer = new HttpProxyServer("localhost", new SslProxy(certificate));
  sslProxyServer.start();
  
  // do ssl stuff
  
  sslProxyServer.stop();
```

## Extension points
Requests are processed in 5 stages:
- receive request from client
- connect to destination server
- send request to server and receive response
- send response back to client
- complete processing and close connections

It is possible to add additional behavior to any stage with delegates:

```csharp
  var httpProxy = new HttpProxy(){
    OnRequestReceived = context => {},
    OnServerConnected = context => {},
    OnResponseReceived = context => {},
    OnResponseSent = context => {},
    OnProcessingComplete = context => {}
  };
```

Context stores request information during processing single request. What you can possibly do with it ?
- modify request and response headers
- modify request and response body
- respond by yourself on behalf of destination server
- ...or something in between

Take a look at [console app](https://github.com/jimevans/BenderProxy/blob/master/BenderProxy.ConsoleApp/src/Program.cs) and [tests](https://github.com/jimevans/BenderProxy/blob/master/BenderProxy.Tests/src/Integration/InterceptionTests.cs) for usage example.

## Why a Fork?
This fork was created because the original project has dependencies on [Log4Net](https://logging.apache.org/log4net/)
and uses [code contracts](https://docs.microsoft.com/en-us/dotnet/framework/debug-trace-profile/code-contracts).
While there is nothing wrong with these two dependencies, it makes it difficult to use the project in a monorepo,
where all dependencies are committed to the source tree, as all transitive dependencies similarly need to be\
committed. Repeated attempts to contact the author and maintainer of FryProxy to address these shortcomings were
unsuccessful.

## A Note about Pull Requests and Issues
Contributions are always welcome in the form of Pull Requests (PRs). Feel free to submit them as needed, and they will
be reviewed as soon as possible. Regarding issue reports, if the issue is a feature or enhancement request, do submit
it as a new issue report here in the issue tracker for discussion. In the issue report description, please indicate
clearly that it is a request for new functionality, and the issue report will be tagged appropriately.

Bug reports are required to have an accompanying PR with a failing test that demonstrates the bug. Please note that
the PR is not required to fix the bug; it must merely include a failing test that reproduces the issue when using
the current code base at the time of submission. Bug reports without a link to an accompanying PR with a failing
test will have one comment asking for the failing test PR. If the PR is not supplied within one week of the request
for the PR, the bug report will be summarily closed. This policy is not intended to be harsh, but is intended to
prevent the common case of being unable to reproduce issues reported as bugs.
