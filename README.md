# Falco

[![Build Status](https://travis-ci.org/pimbrouwers/Falco.svg?branch=master)](https://travis-ci.org/pimbrouwers/Falco)

Falco is a micro-library built upon the high-performance components of ASP.NET Core: [Kestrel][1], [Pipelines][2] & [Endpoint Routing][3]. To facilitate building simple, fault-tolerant and blazing fast functional web applications using F#. 

## Why?

> This project was *heavily* inspired by [Giraffe][4]. Those looking for a more mature & comprehensive web framework should definitely go check it out.

Many people often regard of ASP.NET as a big, monolithic framework. Synonymous with ASP.NET MVC. MVC is indeed a large (albeit *very* good) framework. But underneath, is a highly componential suite of tools that you can use in absence of the MVC assemblies.

The goal of this project was to design the thinnest possible API on top of the base ASP.NET library. Aimed at supporting:
- Non-compositional routing built upon the new [Endpoint Routing][3] feature in .NET Core.
- Compositional request handling. 

Following this approach leaves the difficult work of matching & dispatching requests to the core ASP.NET Team and the request handling to you. Any performance improvements made to the core libraries are thus passed directly on to your solution. And also means that developers with experience using .NET Core, either C# or F#, will be familiar with the base ASP.NET integration.

## Quick Start

Create a new F# web project:
```
dotnet new web -lang F# -o HelloWorldApp
```

Install the nuget package:
```
dotnet add HelloWorldApp package Falco
```

Remove the `Startup.cs` file and save the following in `Program.cs`:
```f#
module HelloWorldApp 

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Falco

// ------------
// Logging
// ------------
let configureLogging (loggerBuilder : ILoggingBuilder) =
    loggerBuilder
        .AddFilter(fun l -> l.Equals LogLevel.Error)
        .AddConsole()
        .AddDebug() |> ignore

// ------------
// Services
// ------------
let configureServices (services : IServiceCollection) =
    services
        .AddResponseCaching()
        .AddResponseCompression()    
        .AddRouting() // Required for Falco
        |> ignore

// ------------
// Web App
// ------------
let helloHandler : HttpHandler =
    textOut "hello world"

let configureApp (app : IApplicationBuilder) =      
    let routes = [        
        get "/" helloHandler
    ]

    app.UseDeveloperExceptionPage()       
       .UseHttpEndPoints(routes) // Activate Falco
       |> ignore

[<EntryPoint>]
let main _ =
    try
        WebHostBuilder()
            .UseKestrel()       
            .ConfigureLogging(configureLogging)
            .ConfigureServices(configureServices)
            .Configure(configureApp)          
            .Build()
            .Run()
        0
    with 
        | _ -> -1
```

Run the application:
```
dotnet run HelloWorldApp
```

## Sample Applications 

Code is always worth a thousand words, so for the most up-to-date usage, the [/samples][6] directory contains a few sample applications.

| Sample | Description |
| ------ | ----------- |
| [HelloWorldApp][7] | A basic hello world app |
| [SampleApp][8] | Demonstrates more complex topics: view engine, authentication and json |

## Routing

The breakdown of [Endpoint Routing][3] is simple. Associate a a specific [route pattern][5] (and optionally an HTTP verb) to a `RequestDelegate`, a promise to process a request. 

Bearing this in mind, routing can practically be represented by a list of these "mappings".

```f#
let routes = 
  [
    route "POST" "/login"              loginHandler        
    route "GET"  "/hello/{name:alpha}" helloHandler    
  ]

// or more simply 
let routes = 
  [
    post "/login"              loginHandler        
    get  "/hello/{name:alpha}" helloHandler    
  ]
```

## Request Handling

A `RequestDelegate` can be thought of as the eventual (i.e. async) processing of an HTTP Request. It is the core unit of work in [ASP.NET Core Middleware][10]. Middleware added to the pipeline can be expected to sequentially processes incoming requests. 

In functional programming, it is VERY common to [compose][9] many functions into larger ones, which process input sequentially and produce output. The beauty of this approach is that it leads to software built of many small, easily tested, functions. 

If we apply this thought pattern to individual HTTP request processing, we can compose our web applications by "glueing" together many little (often) reusable functions.

To support this approrach we need only a few simple types:

```f#
type HttpFuncResult = Task<HttpContext option>
type HttpFunc = HttpContext -> HttpFuncResult
type HttpHandler = HttpFunc -> HttpFunc    
```

At the lowest level is the `HttpFuncResult`, which not unlike a `RequestDelegate`, represents the eventuality of work against the `HttpContext` being performed. In this case, the type [optionally][11] returns the context to enabling short-circuiting future processing.

Performing this work is the `HttpFunc` which upon reception of an `HttpContext` will (eventully) return the optional `HttpContext`.

To enable glueing these operations together, we use a [combinator][12] to combine two `HttpHandler`'s into one using Kleisli composition (i.e. the output of the left function produces monadic input for the right). 

The composition of two `HttpHandler`'s can be accomplished using the `compose` function, or the "fish" operator `>=>`.

> `>=>` is really just a function composition. But `>>` wouldn't work here since the return type of the left function isn't the argument of the right, rather it is a monad that needs to be unwrapped. Which is exactly what `>=>` does.

### Composing two `HttpHandler`'s

```f#
let forbiddenHandler : HttpHandler =
  setStatusCode 403 >=> textOut "Forbidden"
```

### Built-in `HttpHandler`'s

Plain-text
```f#
let textHandler =
    textOut "Hello World"
```

HTML
```f#
let doc = 
    html [] [
            head [] [            
                    title [] [ raw "Sample App" ]                                                    
                ]
            body [] [                     
                    h1 [] [ raw "Sample App" ]
                ]
        ] 

let htmlHandler : HttpHandler =
    htmlOut doc
```

JSON (uses the default `System.Text.Json.JsonSerializer`)
```f#
type Person =
    {
        First : string
        Last  : string
    }

let jsonHandler : HttpHandler =
    { First = "John"; Last = "Doe" }
    |> jsonOut
```

Set Status Code
```f#
let notFoundHandler : HttpHandler =
    setStatusCode 404 >=> textOut "Not Found"
```

HTTP Redirect
```f#
let oldUrlHandler : HttpHandler =
    redirect "/new-url" false
```

### Creating new `HttpHandler`'s

The built-in `HttpHandler`'s will likely only take you so far. Luckily creating new `HttpHandler`'s is very easy.

The following handlers reuse the built-in `textOut` handler:

```f#
let helloHandler : HttpHandler = 
  textOut "hello"

let helloYouHandler (name : string) : HttpHandler = 
  let msg = sprintf "Hello %s" name
  textOut msg
```

The following function defines an `HttpHandler` which checks for a route value called "name" and uses the built-in `textOut` handler to return plain-text to the client. The 

```f#
let helloHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->        
        let name = ctx.RouteValue "name" |> Option.defaultValue "someone"
        let msg = sprintf "hi %s" name 
        textOut msg next ctx
```

## View Engine

A core feature of Falco is the functional view engine. Using it means:

- Writing your views in plain F#, directly in your assembly.
- Markup is compiled along-side the rest of your code. Leading to improved performance and ultimately simpler deployments.

Most of the standard HTML tags & attributes have been mapped to F# functions, which produce objects to represent the HTML node. Node's are either:
- `Text` which represents `string` values.
- `SelfClosingNode` which represent self-closing tags (i.e. `<br />`).
- `ParentNode` which represent typical tags with, optionally, other tags within it (i.e. `<div>...</div>`).

```f#
let doc = html [ _lang "en" ] [
        head [] [
            meta  [ _charset "UTF-8" ]
            meta  [ _httpEquiv "X-UA-Compatible"; _content "IE=edge,chrome=1" ]
            meta  [ _name "viewport"; _content "width=device-width,initial-scale=1" ]
            title [] [ raw "Sample App" ]                                        
            link  [ _href "/style.css"; _rel "stylesheet"]
        ]
        body [] [                     
                main [] [
                        h1 [] [ raw "Sample App" ]
                    ]
            ]
    ] 
```

Since views are plain F# they can easily be made strongly-typed:
```f#
type Person =
    {
        First : string
        Last  : string
    }

let doc (person : Person) = 
    html [ _lang "en" ] [
        head [] [
            meta  [ _charset "UTF-8" ]
            meta  [ _httpEquiv "X-UA-Compatible"; _content "IE=edge,chrome=1" ]
            meta  [ _name "viewport"; _content "width=device-width,initial-scale=1" ]
            title [] [ raw "Sample App" ]                                        
            link  [ _href "/style.css"; _rel "stylesheet"]
        ]
        body [] [                     
                main [] [
                        h1 [] [ raw "Sample App" ]
                        p  [] [ raw (sprintf "%s %s" person.First person.Last)]
                    ]
            ]
    ] 
```

Views can also be combined to create more complex views and share output:
```f#
let master (title : string) (content : XmlNode list) =
    html [ _lang "en" ] [
        head [] [
            meta  [ _charset "UTF-8" ]
            meta  [ _httpEquiv "X-UA-Compatible"; _content "IE=edge,chrome=1" ]
            meta  [ _name "viewport"; _content "width=device-width,initial-scale=1" ]
            title [] [ raw title ]                                        
            link  [ _href "/style.css"; _rel "stylesheet"]
        ]
        body [] content
    ]  

let divider = 
    hr [ _class "divider" ]

let homeView =
    master "Homepage" [
            h1 [] [ raw "Homepage" ]
            divider
            p  [] [ raw "Lorem ipsum dolor sit amet, consectetur adipiscing."]
        ]

let aboutViw =
    master "About Us" [
            h1 [] [ raw "About Us" ]
            divider
            p  [] [ raw "Lorem ipsum dolor sit amet, consectetur adipiscing."]
        ]

```

### Extending the view engine

The view engine is extremely extensible since creating new tag's is simple. 

An example to render `<svg>`'s:

```f#
let svg (width : float) (height : float) =
    tag "svg" [
            attr "version" "1.0"
            attr "xmlns" "http://www.w3.org/2000/svg"
            attr "viewBox" (sprintf "0 0 %f %f" width height)
        ]

let path d = tag "path" [ attr "d" d ] []

let bars =
    svg 384.0 384.0 [
            path "M368 154.668H16c-8.832 0-16-7.168-16-16s7.168-16 16-16h352c8.832 0 16 7.168 16 16s-7.168 16-16 16zm0 0M368 32H16C7.168 32 0 24.832 0 16S7.168 0 16 0h352c8.832 0 16 7.168 16 16s-7.168 16-16 16zm0 0M368 277.332H16c-8.832 0-16-7.168-16-16s7.168-16 16-16h352c8.832 0 16 7.168 16 16s-7.168 16-16 16zm0 0"
        ]
```

## Authentication

ASP.NET Core has amazing built-in support for authentication. Review the [docs][13] for specific implementation details. Falco optionally (`open Falco.Auth`) includes some authentication utilites.

Authentication control flow:

```f#
// prevent user from accessing secure endpoint
let secureResourceHandler : HttpHandler =
    ifAuthenticated (redirect "/forbidden" false) 
    >=> textOut "hello authenticated person"

// prevent authenticated user from accessing anonymous-only end-point
let anonResourceOnlyHandler : HttpHandler =
    ifNotAuthenticated (redirect "/" false) 
    >=> textOut "hello anonymous"
```

Secure views:
```f#
let doc (principal : ClaimsPrincipal option) = 
    let isAuthenticated = 
        match user with 
        | Some u -> u.Identity.IsAuthenticated 
        | None   -> false

    html [ _lang "en" ] [
        head [] [
            meta  [ _charset "UTF-8" ]
            meta  [ _httpEquiv "X-UA-Compatible"; _content "IE=edge,chrome=1" ]
            meta  [ _name "viewport"; _content "width=device-width,initial-scale=1" ]
            title [] [ raw "Sample App" ]                                        
            link  [ _href "/style.css"; _rel "stylesheet"]
        ]
        body [] [                     
                main [] [
                        yield h1 [] [ raw "Sample App" ]
                        if isAuthenticated then yield p  [] [ raw "Hello logged in user" ]
                    ]
            ]
    ]

let secureDocHandler : HttpHandler =
    authHtmlOut doc
```

## Security

Documentation coming soon.

## Why "Falco"?

It's all about [Kestrel][1]. A simply beautiful piece of software that has been a game changer for the .NET web stack. In the animal kingdom, "Kestrel" is a name given to several members of the falcon genus, also known as "Falco".

## Find a bug?

There's an [issue](https://github.com/pimbrouwers/Falco/issues) for that.

## License

Built with ♥ by [Pim Brouwers](https://github.com/pimbrouwers) in Toronto, ON. Licensed under [Apache License 2.0](https://github.com/pimbrouwers/Falco/blob/master/LICENSE).

[1]: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-3.1 "Kestrel web server implementation in ASP.NET Core"
[2]: https://devblogs.microsoft.com/dotnet/system-io-pipelines-high-performance-io-in-net/ "System.IO.Pipelines: High performance IO in .NET"
[3]: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-3.1#configuring-endpoint-metadata "EndpointRouting in ASP.NET Core"
[4]: https://github.com/giraffe-fsharp/Giraffe "A native functional ASP.NET Core web framework for F# developers."
[5]: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-3.1#route-template-reference
[6]: https://github.com/pimbrouwers/Falco/tree/master/samples
[7]: https://github.com/pimbrouwers/Falco/tree/master/samples/HelloWorldApp
[8]: https://github.com/pimbrouwers/Falco/tree/master/samples/SampleApp
[9]: https://en.wikipedia.org/wiki/Function_composition "Function composition"
[10]: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1 "ASP.NET Core Middlware"
[11]: https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/options "F# Options"
[12]: https://wiki.haskell.org/Combinator "Combinator"
[13]: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-3.1 "Overview of ASP.NET Core authentication"