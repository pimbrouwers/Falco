# Falco

[![Build Status](https://travis-ci.org/pimbrouwers/Falco.svg?branch=master)](https://travis-ci.org/pimbrouwers/Falco)

Falco is a micro-library built upon the high-performance components of ASP.NET Core: [Kestrel][1], [Pipelines][2] & [Endpoint Routing][3]. To facilitate building simple, fault-tolerant and blazing fast functional web applications using F#. 

## Why?

> This project was *heavily* inspired by [Giraffe][4]. Those looking for a more mature & comprehensive web framework should definitely go check it out.

Many people often regard of ASP.NET as a big, monolithic framework. Synonymous with ASP.NET MVC. MVC is indeed a large (albeit *very* good) framework. But underneath, is a highly componential suite of tools that you can use in absence of the MVC assemblies.

The goal of this project was to design the thinnest possible API on top of the base ASP.NET components. Aimed at supporting:
- Simple, non-compositional abstraction built upon the new [Endpoint Routing][3] feature in .NET Core, and let developer's program.
- Functional, compositional HTTP handlers that would process incoming requests. 

Following this approach leaves the difficult work of matching & dispatching requests to the core ASP.NET Team and creating the request handling to you. Any performance improvements made to the core libraries are thus passed directly on to your solution. It also means that developers with experience using .NET Core, either C# or F#, will be intimately familiar with the base ASP.NET integration.

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
       .UseHttpEndPoints(routes) // Activating Falco
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

A few sample applications can be found in the [/samples][6] directory.

| Sample | Description |
| ------ | ----------- |
| [HelloWorldApp][7] | A basic hello world app |
| [SampleApp][8] | Demonstrates more complex topics: view engine, authentication and json |

## Routing

The breakdown of [Endpoint Routing][3] is simple. Associate a a specific [route pattern][5] (and optionally an HTTP VERB) to a `RequestDelegate`, a promise to process a request. 

Bearing this in mind, routing can practically be represented by a list of these "mappings" `(verb : string) -> (pattern : string) -> (handler : HttpHandler)`.

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

A `RequestDelegate` can be thought of as the eventual (i.e. async) processing of an HTTP Request. It is the core unit of work in [ASP.NET Core Middleware][10]. Once added to the pipeline, middleware will sequentially processes incoming requests. 

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

To enable glueing this operations together, the `HttpHandler` receives an `HttpFunc` and return another `HttpFunc`. Doing this enables us to use a combinator to combine two `HttpHandler`'s into one using Kleisli composition (i.e. the output of the left function produces monadic input for the right). 

The composition of two `HttpHandler`'s can be accomplished using the "fish" operator: `>=>`.

> `>=>` is really just a function composition, but `>>` wouldn't work here since the return type of the left function isn't the argument of the right, rather it is a monad that needs to be unwrapped. Which is exactly what `>=>` does.

```f#
// An example composing two built-in HttpHandler's
let forbiddenHandler : HttpHandler =
  setStatusCode 403 >=> textOut "Forbidden"
```

## View Engine

Documentation coming soon.

## Authentication

Documentation coming soon.

## Security

Documentation coming soon.

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