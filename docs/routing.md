# Routing

The breakdown of [Endpoint Routing](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing#configuring-endpoint-metadata) is simple. Associate a specific [route pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing#route-template-reference) and an HTTP verb to an [`HttpHandler`](request.md) which represents the ongoing processing (and eventual return) of a request.

Bearing this in mind, routing can practically be represented by a list of these "mappings" known in Falco as an `HttpEndpoint` which bind together: a route, verb and handler.

To create a "multi-method" endpoint, the `all` function accepts a list of HTTP Verb and `HttpHandler` pairs.

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder

webHost [||] {
    any "/"      (Response.ofPlainText "/")
    get "/hello" (Response.ofPlainText "/hello")
    all "/form"  [GET, Response.ofPlainText "/form"
                  POST, Requuest.mapJson Response.ofJson]
}
```
