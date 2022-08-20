# Routing

The breakdown of [Endpoint Routing](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing#configuring-endpoint-metadata) is simple. Associate a specific [route pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing#route-template-reference) and an HTTP verb) to an [`HttpHandler`](request.md) which represents the ongoing processing (and eventual return) of a request.

Bearing this in mind, routing can practically be represented by a list of these "mappings" known in Falco as an `HttpEndpoint` which bind together: a route, verb and handler. To create a "multi-mehtod" endpoint, the `all` function accepts a list of HTTP Verb and `HttpHandler` pairs.

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder

/// GET /Hello/{Name:alpha}
let helloHandler : HttpHandler =
    let getMessage (route : RouteCollectionReader) =
        let name = route.GetString "Name" "World"
        sprintf "Hello %s" name

    Request.mapRoute getMessage Response.ofPlainText

/// GET /Login
let loginHandler : HttpHandler = // ...

/// POST /Login
let loginSubmitHandler : HttpHandler = // ...

module Router =
    let endpoints =
        [
            // a basic GET handler, with an alphanumerically constrained route parameter
            get "/Hello/{Name:alpha}" helloHandler

            // multi-method endpoint
            all "/Login" [
                POST, loginSubmitHandler
                GET,  loginHandler ]
        ]

webHost [||] {
    endpoints Router.endpoints
}
```
