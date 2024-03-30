# Example - Hello World MVC

Let's take our basic [Hello World](example-hello-world.md) to the next level. This means we're going to dial up the complexity a little bit. But we'll do this using the well recognized MVC pattern. We'll contain the app to a single file to make "landscaping" the pattern more straight-forward.

The code for this example can be found [here](https://github.com/pimbrouwers/Falco/tree/master/examples/HelloWorldMvc).

## Model

Since this app has no persistence, the model is somewhat boring. But included here to demonstrate the concept.

We define two simple record types. One to contain the patron name, the other to contain a `string` message.

```fsharp
module Model =
    type NameGreeting =
        { Name : string }

    type Greeting =
        { Message : string }
```

## Routing

As the project scales, it is generally helpful to have static references to your URLs and/or URL generating functions for dynamic resources.

[Routing](routing.md) begins with a route template, so it's only natural to define those first.

```fsharp
module Route =
    let index = "/"
    let greetPlainText = "/greet/text/{name}"
    let greetJson = "/greet/json/{name}"
    let greetHtml = "/greet/html/{name}"
```

Here you can see we define one static route, and 3 dynamic route templates. We can provide URL generation from these dynamic route templates quite easily with some simple functions.

```fsharp
module Url =
    let greetPlainText name = Route.greetPlainText.Replace("{name}", name)
    let greetJson name = Route.greetJson.Replace("{name}", name)
    let greetHtml name = Route.greetHtml.Replace("{name}", name)
```

These 3 functions take a string input called `name` and plug it into the `{name}` placeholder in the route template. This gives us a nice little typed API for creating our application URLs.

## View

Falco comes packaged with a [lovely little HTML DSL](https://github.com/pimbrouwers/Falco.Markup/). It can produce any form of angle-markup, and does so very [efficiently](https://github.com/pimbrouwers/Falco.Markup/?tab=readme-ov-file#performance). The main benefit is that our views are _pure_ F#, compile-time checked and live alongside the rest of our code.

First we define a shared HTML5 `layout` function, that references our project `style.css`. Next, we define a module to contain the views for our greetings.

> You'll notice the `style.css` file resides in a folder called `wwwroot`. This is an [ASP.NET convention](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files) which we'll enable later when we [build the web server](#web-server).

```fsharp
module View =
    open Model

    let layout content =
        Templates.html5 "en"
            [ Elem.link [ Attr.href "/style.css"; Attr.rel "stylesheet" ] ]
            content

    module GreetingView =
        /// HTML view for /greet/html
        let detail greeting =
            layout [
                Text.h1 $"Hello {greeting.Name} from /html"
                Elem.hr []
                Text.p "Greet other ways:"
                Elem.nav [] [
                    Elem.a  [ Attr.href (Url.greetPlainText greeting.Name) ] [ Text.raw "Greet in text"]
                    Text.raw " | "
                    Elem.a [ Attr.href (Url.greetJson greeting.Name) ] [ Text.raw "Greet in JSON " ]
                ]
            ]
```

The markup code is fairly self-explanatory. But essentially:

- `Elem` produces HTML elements.
- `Attr` produces HTML element attributes.
- `Text` produces HTML text nodes.

Each of these modules matches (or tries to) the full HTML spec. You'll also notice two of our URL generators at work.

## Errors

We'll define a couple static error pages to help prettify our error output.

```fsharp
module ErrorPage =
    let notFound : HttpHandler =
        Response.withStatusCode 404 >>
        Response.ofHtml (View.layout [ Text.h1 "Not Found" ])

    let serverException : HttpHandler =
        Response.withStatusCode 500 >>
        Response.ofHtml (View.layout [ Text.h1 "Server Error" ])
```

Here we see the [`HttpResponseModifier`](repsonse.md#response-modifiers) at play, which set the status code before buffering out the HTML response. We'll reference these pages later when be [build the web server](#web-server).

## Controller

Our controller will be responsible for four actions, as defined in our [route](#routing) module.

We take advantage of the `Request.mapRoute` continuation to create a little helper function called `mapRouteToNameGreeting` to obtain our `NameGreeting` from the route.

Next, we define three handlers to consume the name in three different ways: plain text, JSON and HTML.

```fsharp
module Controller =
    open Model
    open View

    module GreetingController =
        /// GET /
        let index : HttpHandler =
            Response.ofPlainText "Hello world" // <-- we've seen this before!

        /// A helper to project the name into an HttpHandler
        let private mapRouteToNameGreeting next =
            Request.mapRoute 
                (fun route -> { Name = route.GetString "name" }) // <-- almost feels like a dynamic 
                next

        /// GET /greet/{name}
        let plainTextDetail : HttpHandler =
            mapRouteToNameGreeting (fun greeting ->
                let message = $"Hello {greeting.Name}"
                Response.ofPlainText message)

        /// GET /greet/json
        let jsonDetail : HttpHandler = // <-- Continuation-style HttpHandler
            mapRouteToNameGreeting (fun greeting ->
                let message = { Message = $"Hello {greeting.Name} from /json" }
                Response.ofJson message) 

        /// GET /greet/html
        let htmlDetail : HttpHandler =
            mapRouteToNameGreeting (fun greeting ->
                greeting
                |> GreetingView.detail
                |> Response.ofHtml)
```

If you aren't a fan of using continuation-passing style, you can just as easily get the route using the `HttpContext` explicitly. For example:

```fsharp
let private getNameGreeting (ctx : HttpContext) =
    let route = Request.getRoute ctx
    { Name = route.GetString "name" }

/// GET /greet/{name}
let plainTextDetail : HttpHandler = fun ctx ->
    let greeting = getNameGreeting ctx
    let message = $"Hello {greeting.Name}"
    Response.ofPlainText message ctx
```

## Web Server

This is a great opportunity to demonstrate further how to configure a more complex web server than we saw in the basic hello world example.

To do that, we'll define an explicit entry point function which gives us access to the command line argument. By then forwarding these into the web application, we gain further [configurability](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration#command-line). You'll notice the application contains a file called `appsettings.json`, this is another [ASP.NET convention](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration#default-application-configuration-sources) that provides fully-featured and extensible configuration functionality.

In this example, we examine the environment name to create an "is development" toggle. We use this to determine the extensiveness of our error output. You'll notice we use our exception page from above when an exception occurs when not in development mode. Otherwise, we show a developer-friendly error page.

Next we define an explicit collection of endpoints, which gets passed into the `.UseFalco(endpoints)` extension method. You could just as easily invoke `.FalcoGet()` four times, but the collection approach generally scales better. The choice is yours.

We end off by registering a terminal handler, which functions as our "not found" response.

## Wrapping Up

This example was a leap ahead from our basic hello world. But having followed this, you know understand 85% of the pattern you'll need to know to build end-to-end server applications with Falco. Unsurprisingly, the entire program fits inside 118 LOC. One of the magnificent benefits of writing code in F#.

[Next: Example - Dependency Injection](sample-hello-world-mvc.md)