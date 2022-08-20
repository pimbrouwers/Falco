# Routing

The breakdown of [Endpoint Routing](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-3.1#configuring-endpoint-metadata) is simple. Associate a specific [route pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-3.1#route-template-reference) (and optionally an HTTP verb) to an `HttpHandler` which represents the ongoing processing (and eventual return) of a request.

Bearing this in mind, routing can practically be represented by a list of these "mappings" known in Falco as an `HttpEndpoint` which bind together: a route, verb and handler.

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder

let helloHandler : HttpHandler =
    let getMessage (route : RouteCollectionReader) =
        route.GetString "name" "World"
        |> sprintf "Hello %s"

    Request.mapRoute getMessage Response.ofPlainText

let loginHandler : HttpHandler = // ...

let loginSubmitHandler : HttpHandler = // ...

webHost [||] {
    endpoints [
        // a basic GET handler
        get "/hello/{name:alpha}" helloHandler

        // multi-method endpoint
        all "/login"
            [ POST, loginSubmitHandler
              GET,  loginHandler ]
    ]
}
```
