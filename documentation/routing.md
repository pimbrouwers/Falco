# Routing

Routing is responsible for matching incoming HTTP requests and dispatching those requests to the app's `HttpHandler`s. The breakdown of [Endpoint Routing](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing#configuring-endpoint-metadata) is simple. Associate a specific [route pattern](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing#route-template-reference) and an HTTP verb to an [`HttpHandler`](request.md) which represents the ongoing processing (and eventual return) of a request.

Bearing this in mind, routing can practically be represented by a list of these "mappings" known in Falco as an `HttpEndpoint` which bind together: a route, verb and handler.

> Note: All of the following examples are _fully functioning_ web apps.

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder

webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello World!")
    ]
}
```

The preceding example includes a single `HttpEndpoint`:
- When an HTTP `GET` request is sent to the root URL `/`:
    - The `HttpHandler` shown executes.
    - `Hello World!` is written to the HTTP response using the [Response](response.md) module.
- If the request method is not `GET` or the URL is not `/`, no route matches and an HTTP 404 is returned.

The following example shows a more sophisticated `HttpEndpoint`:

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder

webHost [||] {
    endpoints [
        get "/hello/{name:alpha}" (fun ctx ->
            let route = Request.getRoute ctx
            let name = route.GetString "name" ""
            let message = sprintf "Hello %s" name
            Response.ofPlainText message ctx)
    ]
}
```

The string `/hello/{name:alpha}` is a **route template**. It is used to configure how the endpoint is matched. In this case, the template matches:

- A URL like `/hello/Ryan`
- Any URL path that begins with `/hello/` followed by a sequence of alphabetic characters. `:alpha` applies a route constraint that matches only alphabetic characters.
  - Full route constraint reference: [https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing#route-constraint-reference](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing#route-constraint-reference).

The second segment of the URL path, `{name:alpha}`:

- Is bound to the `name` parameter.
- Is captured and stored in `HttpRequest.RouteValues`, which Falco exposes through a [uniform API](request.md) to obtain primitive typed values.

An alternative way to express the `HttEndpoint` above is seen below. Note the omission of the `ctx` parameter, made possible by the [Request](request.md) module:

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder

webHost [||] {
    endpoints [
        get "/hello/{name:alpha}"
            (Request.mapRoute
                (fun route -> route.GetString "name" "John Doe")
                Response.ofPlainText)
    ]
}
```

## Multi-method Endpoints

There are scenarios where you may want to accept multiple HTTP verbs to single a URL. For example, a `GET`/`POST` form submission.

To create a "multi-method" endpoint, the `all` function accepts a list of HTTP Verb and HttpHandler pairs.

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder

let form =
    Templates.html5 "en" [] [
        [ Elem.form [ Attr.method "post" ] [
            Elem.input [ Attr.name "name" ]
            Elem.input [ Attr.type' "submit" ] ] ]

webHost [||] {
    endpoints [
        get "/hello" (Response.ofPlainText "/hello")
        all "/form"  [GET, Response.ofHtml form
                      POST, Request.debug] // useful development tool
    ]
}
