# Falco

[![Build Status](https://travis-ci.org/pimbrouwers/Falco.svg?branch=master)](https://travis-ci.org/pimbrouwers/Falco)

Falco is a micro-library for building simple, fault-tolerant and blazing fast functional web applications using F#. Built upon the high-performance components of ASP.NET Core: [Kestrel][1], [Pipelines][2] & [Endpoint Routing][3].

Key features:
- Simple and powerful [routing](#routing) API.
- Composable [request handling](#request-handling).
- A native F# [view engine](#view-engine).
- Succinct API for [model binding](#model-binding).
- [Authentication](#authentication) and [security](#security) utilities. 
- Streaming `multipart/form-data` reader for [large uploads](#handling-large-uploads).

## Why?

> This project was inspired by [Giraffe][4]. Those looking for a more mature & comprehensive web framework should definitely go check it out. 

The goal of this project was to design the thinnest possible API on top of the native ASP.NET Core library. Aimed at supporting:
- A low barrier to entry for those new to functional programming.
- Simple integration into the native ASP.NET pipeline.
- High-performance routing.
- Composable request handling. 
- Isomorphic web development.

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
        .AddRouting() // Required for Falco
        |> ignore

// ------------
// Web App
// ------------
let notFoundHandler : HttpHandler =
    setStatusCode 404 >=> textOut "Not found"

let helloHandler : HttpHandler =
    textOut "hello world"

let configureApp (app : IApplicationBuilder) =      
    let routes = [        
        get "/" helloHandler
    ]

    app.UseDeveloperExceptionPage()       
       .UseHttpEndPoints(routes) // Activate Falco
       .UseNotFoundHandler(notFoundHandler) // Activate Falco not found handler
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

So in about 50 lines of code including comments and generous spacing/verticality, you've got an industrial-strength "hello world" web app. Which we achieved using primarily base ASP.NET libraries. Pretty sweet!

## Sample Applications 

Code is always worth a thousand words, so for the most up-to-date usage, the [/samples][6] directory contains a few sample applications.

| Sample | Description |
| ------ | ----------- |
| [HelloWorld][7] | A basic hello world app |
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

If we apply this thought pattern to individual HTTP request processing, we can compose our web applications by "gluing" together many little (often) reusable functions.

To support this approrach we need only a few simple types:

```f#
type HttpFuncResult = Task<HttpContext option>
type HttpFunc = HttpContext -> HttpFuncResult
type HttpHandler = HttpFunc -> HttpFunc    
```

At the lowest level is the `HttpFuncResult`, which not unlike a `RequestDelegate`, represents the eventuality of work against the `HttpContext` being performed. In this case, the type [optionally][11] returns the context to enabling short-circuiting future processing.

Performing this work is the `HttpFunc` which upon reception of an `HttpContext` will (eventully) return the optional `HttpContext`.

To enable gluing these operations together, we use a [combinator][12] to combine two `HttpHandler`'s into one using Kleisli composition (i.e. the output of the left function produces monadic input for the right). 

The composition of two `HttpHandler`'s can be accomplished using the `compose` function, or the "fish" operator `>=>`.

> `>=>` is really just a function composition. But `>>` wouldn't work here since the return type of the left function isn't the argument of the right, rather it is a monad that needs to be unwrapped. Which is exactly what `>=>` does.

### Built-in `HttpHandler`'s

`textOut` - Plain Text responses
```f#
let textHandler =
    textOut "Hello World"
```

`htmlOut` - HTML responses
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

`jsonOut` - JSON responses (uses the default `System.Text.Json.JsonSerializer`)
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

`setStatusCode` - Set the status code of the response
```f#
let notFoundHandler : HttpHandler =
    // here we compose (>=>) two built-in handlers
    setStatusCode 404 >=> textOut "Not Found"
```

`redirect` - 301/302 Redirect Response (boolean param to indicate permanency)
```f#
let oldUrlHandler : HttpHandler =
    redirect "/new-url" true
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
        let name = ctx.TryGetRouteValue "name" |> Option.defaultValue "someone"
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
let doc = 
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

## Model Binding

Binding at IO boundaries is messy, error-prone and often verbose. Reflection-based abstractions tend to work well for simple use cases, but quickly become very complicated as the expected complexity of the input rises. This is especially true for an algebraic type system like F#'s. As such, it is often advisable to take back control of this process from the runtime. 

We can make this simpler by creating a succinct API to obtain typed values from `IFormCollection` and `IQueryCollection`. 

> Methods are available for all primitive types, and perform **case-insenstivie** lookups against the collection.

```f#
/// An example handler, safely obtaining values from IFormCollection
let parseFormHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContexnt) ->
        let form = ctx.GetFormReader() // GetFormReaderAsync() also available

        let firstName = form.TryGetString "FirstName" // string -> string option        
        let lastName  = form.TryGet "LastName"        // alias for TryGetString
        let age       = form.TryGetInt "Age"          // string -> int option

/// An example handler, safely obtaining values from IQueryCollection
let parseQueryHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContexnt) ->
        let form = ctx.GetQueryReader()

        let firstName = form.TryGetString "FirstName" // string -> string option        
        let lastName  = form.TryGet "LastName"        // alias for TryGetString
        let age       = form.TryGetInt "Age"          // string -> int option
```

In this case where you don't care about gracefully handling non-existence. Or, you are certain values will be present, the dynamic operator `?` can be useful:

```f#
let parseQueryHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContexnt) ->
        let form = ctx.GetQueryReader()

        // dynamic operator also case-insensitive
        let firstName = form?FirstName.AsString() // string -> string
        let lastName  = form?LastName.AsString()  // string -> string
        let age       = form?Age.AsInt16()        // string -> int16
```

> Use of the `?` dynamic operator also performs **case-insenstive** lookups against the collection.

Further to this, generic `HttpHandler`'s are available to allow for the typical case of *try-or-fail* that asks for a: binding function and HttpHandler's for error and success cases.

```f#
// An example handler which attempts to bind a form
let exampleTryBindFormHandler : HttpHandler =
    tryBindForm 
        (fun r ->
            Ok {
              FirstName = form?FirstName.AsString()
              LastName  = form?LastName.AsString()
              Age       = form?Age.AsInt16()      
            })
        errorHandler 
        successHandler

// An example handler which attempts to bind a query
let exampleTryBindQueryHandler : HttpHandler =
    tryBindQuery 
        (fun r ->
            Ok {
              FirstName = form?FirstName.AsString()
              LastName  = form?LastName.AsString()
              Age       = form?Age.AsInt16()      
            })
        errorHandler 
        successHandler

// An example using a type and static binder, which can make things simpler
[<CLIMutable>]
type SearchQuery =
    {
        Frag : string
        Page : int option
        Take : int
    }

    static member FromReader (r : StringCollectionReader) =
        Ok {
            Frag = r?frag.AsString()
            Page = r.TryGetInt "page" |> Option.defaultValue 1
            Take = r?take.AsInt()
        }

let searchResultsHandler : HttpHandler =
    tryBindQuery 
        SearchQuery.FromReader 
        errorHandler 
        successHandler
```

## Authentication

ASP.NET Core has amazing built-in support for authentication. Review the [docs][13] for specific implementation details. Falco optionally (`open Falco.Auth`) includes some authentication utilites.

> To use the authentication helpers, ensure the service has been registered (`AddAuthentication()`) with the `IServiceCollection` and activated (`UseAuthentication()`) using the `IApplicationBuilder`. 

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

Cross-site scripting attacks are extremely common, since they are quite simple to carry out. Fortunately, protecting against them is as easy as performing them. 

The [Microsoft.AspNetCore.Antiforgery][14] package provides the required utilities to easily protect yourself against such attacks.

Falco provides a few handlers via `Falco.Security.Xss`:

> To use the Xss helpers, ensure the service has been registered (`AddAntiforgery()`) with the `IServiceCollection` and activated (`UseAntiforgery()`) using the `IApplicationBuilder`. 

```f#
open Falco.Xss 

let formView (token : AntiforgeryTokenSet) = 
    html [] [
            body [] [
                    form [ _method "post" ] [
                            // using the CSRF HTML helper
                            antiforgeryInput token
                            input [ _type "submit"; _value "Submit" ]
                        ]                                
                ]
        ]
    
// a custom handler that requires the CSRF token
let csrfHandler (token : AntiforgeryTokenSet) : HttpHandler = 
    fun (next: HttpFunc) (ctx : HttpContext) ->                                
        htmlView (formView token) next ctx

let routes =
    [
        // using CSRF html handler
        get  "/token" (csrfHtmlOut formView)

        // using token control-flow handler
        post "/token" (ifTokenValid (textOut "intruder!") >=> text "oh hi there ;)")

        // using the tokenizer with a cutom handler
        get  "/manual-token" (csrfTokenizer csrfHandler)
    ]
```

### Crytography

Many sites have the requirement of a secure log in and sign up (i.e. registering and maintaining a user's database). Thus, generating strong hashes and random salts is of critical importance. 

Falco helpers are accessed by importing `Falco.Auth.Crypto`.

```f#
open Falco.Crypto 

// Generating salt,
// using System.Security.Cryptography.RandomNumberGenerator,
// create a random 16 byte salt and base 64 encode
let salt = salt 16 

// Hashing password
// Pbkdf2 Key derivation using HMAC algorithm with SHA256 hashing function
// 25,000 iterations and 32 bytes in length
let password = "5upe45ecure"
let hashedPassword = password |> sha256 25000 32
``` 

## Handling Large Uploads

Microsoft defines [large uploads][15] as anything **> 64KB**, which well... is most uploads. Anything beyond this size, and they recommend streaming the multipart data to avoid excess memory consumption.

To make this process **a lot** easier Falco exposes an `HttpContext` extension method `TryStreamFormAsync()` that will attempt to stream multipart form data, or return an error message indicating the likely problem.

```f#
open Falco.Multipart 

let imageUploadHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! form = ctx.TryStreamFormAsync() // Returns a standard IFormCollection
            
            return!
                (match form with 
                | Error msg -> setStatusCode 400 >=> textOut msg
                | Ok form   -> ... ) next ctx
        }
```

## Why "Falco"?

It's all about [Kestrel][1], a simply beautiful piece of software that has been a game changer for the .NET web stack. In the animal kingdom, "Kestrel" is a name given to several members of the falcon genus, also known as "Falco".

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
[7]: https://github.com/pimbrouwers/Falco/tree/master/samples/HelloWorld
[8]: https://github.com/pimbrouwers/Falco/tree/master/samples/SampleApp
[9]: https://en.wikipedia.org/wiki/Function_composition "Function composition"
[10]: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1 "ASP.NET Core Middlware"
[11]: https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/options "F# Options"
[12]: https://wiki.haskell.org/Combinator "Combinator"
[13]: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-3.1 "Overview of ASP.NET Core authentication"
[14]: https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-3.1 "Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core"
[15]: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1#upload-large-files-with-streaming "Large file uploads"
[16]: https://github.com/mgravell/fast-member/
