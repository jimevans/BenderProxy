# BenderProxy
A man-in-the-middle web proxy written in C#
BenderProxy
========

Extensible man in the middle HTTP proxy with SSL support. BenderProxy is a fork of the otherwise
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
