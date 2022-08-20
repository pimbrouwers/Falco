# Routing

The breakdown of [Endpoint Routing][3] is simple. Associate a specific [route pattern][5] (and optionally an HTTP verb) to an `HttpHandler` which represents the ongoing processing (and eventual return) of a request.

Bearing this in mind, routing can practically be represented by a list of these "mappings" known in Falco as an `HttpEndpoint` which bind together: a route, verb and handler.

```fsharp
let helloHandler : HttpHandler =
    let getMessage (route : RouteCollectionReader) =
        route.GetString "name" "World"
        |> sprintf "Hello %s"

    Request.mapRoute getMessage Response.ofPlainText

let loginHandler : HttpHandler = // ...

let loginSubmitHandler : HttpHandler = // ...

let endpoints : HttpEndpoint list =
  [
    // a basic GET handler
    get "/hello/{name:alpha}" helloHandler

    // multi-method endpoint
    all "/login"
        [ POST, loginSubmitHandler
          GET,  loginHandler ]
  ]
```
