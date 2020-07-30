# Falco

[![NuGet Version](https://img.shields.io/nuget/v/Falco.svg)](https://www.nuget.org/packages/Falco)
[![Build Status](https://travis-ci.org/pimbrouwers/Falco.svg?branch=master)](https://travis-ci.org/pimbrouwers/Falco)

Falco is a toolkit for building functional-first, fast and fault-tolerant web applications using F#. 
- Built upon the high-performance primitives of ASP.NET Core.
- Optimized for building HTTP applications quickly.
- Seamlessly integrates with existing .NET Core middleware and frameworks.

## Key features

- Asynchronous [request handling](#request-handling).
- Simple and powerful [routing](#routing) API.
- Fast, secure and configurable [web server](#host).
- Native F# [view engine](#view-engine).
- Succinct API for [model binding](#model-binding).
- [Authentication](#authentication) and [security](#security) utilities. 
- Built-in support for [large uploads](#handling-large-uploads).

## Design Goals

- Aim to be very small and easily learnable.
- Should be extensible.
- Should provide a toolset to build a working end-to-end web application.

## Quick Start - Hello World 3 ways

Create a new F# web project:
```
dotnet new web -lang F# -o HelloWorldApp
```

Install the nuget package:
```
dotnet add package Falco
```

Remove the `Startup.fs` file and save the following in `Program.fs`:
```f#
module HelloWorld.Program

open Falco
open Falco.Markup

let message = "Hello, world!"

let layout message =
    Elem.html [] [
            Elem.head [] [
                    Elem.title [] [ Text.raw message ]
                ]
            Elem.body [] [
                    Elem.h1 [] [ Text.raw message ]
                ]
        ]

let handleIndex =
    get "/" (Response.ofPlainText message)

let handleJson =
    get "/json" (Response.ofJson {| Message = message |})

let handleHtml =
    get "/html" (Response.ofHtml (layout message))

[<EntryPoint>]
let main args =        
    Host.startWebHostDefault 
        args 
        [
            handleHtml
            handleJson
            handleIndex
        ]
    0
```

Run the application:
```
dotnet run HelloWorldApp
```

There you have it, an industrial-strength "hello world 3 ways" web app, achieved using primarily base ASP.NET Core libraries. Pretty sweet!

## Sample Applications 

Code is always worth a thousand words, so for the most up-to-date usage, the [/samples][6] directory contains a few sample applications.

| Sample | Description |
| ------ | ----------- |
| [HelloWorld][7] | A basic hello world app |
| [Blog][17] | A basic markdown (with YAML frontmatter) blog |

## Request Handling

The `HttpHandler` type is used to represent the processing of a request. It can be thought of as the eventual (i.e. asynchronous) completion of and HTTP request processing, defined in F# as: `HttpContext -> Task`. Handlers will typically involve some combination of: route inspection, form/query binding, business logic and finally response writing.  With access to the `HttpContext` you are able to inspect all components of the request, and manipulate the response in any way you choose. 

Basic request/resposne handling is divided between the aptly named [`Request`][18] and [`Response`][16] modules.

- Plain Text responses 
```f#
let textHandler : HttpHandler =
    Response.ofPlainText "Hello World"
```

- HTML responses
```f#
let htmlHandler : HttpHandler =
    let doc = 
        Elem.html [ Attr.lang "en" ] [
                Elem.head [] [                    
                        Elem.title [] [ Text.raw "Sample App" ]                                                            
                    ]
                Elem.body [] [                     
                        Elem.main [] [
                                Elem.h1 [] [ Text.raw "Sample App" ]
                            ]
                    ]
            ] 

    Response.ofHtml doc
```

- JSON responses

> IMPORTANT: This handler will not work with F# options or unions, since it uses the default `System.Text.Json.JsonSerializer`. See [JSON](#json) section below for further information.

```f#
type Person =
    {
        First : string
        Last  : string
    }

let jsonHandler : HttpHandler =
    { First = "John"; Last = "Doe" }
    |> Response.ofJson
```

- Set the status code of the response
```f#
let notFoundHandler : HttpHandler =
    Response.withStatusCode 404
    >> Response.ofPlainText "Not found"
```

- Redirect (301/302) Response (boolean param to indicate permanency)
```f#
let oldUrlHandler : HttpHandler =
    Response.redirect "/new-url" true
```

- Accessing route parameters.
    - The following function defines an `HttpHandler` which checks for a route value called "name" and uses the built-in `textOut` handler to return plain-text to the client:

```f#
let helloHandler : HttpHandler =
    fun ctx ->        
        let greeting =
            Request.tryGetRouteValue "name" ctx 
            |> Option.defaultValue "someone"
            |> sprintf "hi %s" 

        Response.ofPlainText greeting ctx
```

## Routing

The breakdown of [Endpoint Routing][3] is simple. Associate a a specific [route pattern][5] (and optionally an HTTP verb) to an `HttpHandler` which represents the ongoing processing (and eventual return) of a request. 

Bearing this in mind, routing can practically be represented by a list of these "mappings" known in Falco as an `HttpEndpoint` which bind together: a route, verb and handler.

```f#
let loginHandler : HttpHandler =
  fun ctx -> // ...

let helloHandler : HttpHandler =
  fun ctx -> // ...

let endpoints : HttpEndpoint list = 
  [
    post "/login"              loginHandler        
    get  "/hello/{name:alpha}" helloHandler    
  ]

// OR alternatively
let handleLogin : HttpEndpoint =
    post "/login" (fun ctx -> // ...)

let handleHello : HttpEndpoint =
    get "/hello/{name:alpha}" (fun ctx -> // ...)

let endpoints : HttpEndpoint list = 
    [
        handleLogin
        handleHello
    ]
```

## Host

[Kestrel][1] is the web server at the heart of ASP.NET. It's a performant, secure and maintained by incredibly smart people.  If you're looking to get a host up and running quickly the `Host.startWebHostDefault` function will enable everythigng necessary for Falco to work:

```f#
[<EntryPoint>]
let main args =        
    Host.startWebHostDefault 
        args 
        [
            // Endpoints go here
        ]
    0
```

Should you wish to fully customize your host instance the `Host.startWebHost` exposes the `IWebHostBuilder` which enables full customization. For a full example, see the [Blog sample][8].

```f#
// Logging
let configureLogging 
    (log : ILoggingBuilder) =
    log.SetMinimumLevel(LogLevel.Error)
    |> ignore

// Services
let configureServices 
    (services : IServiceCollection) =
    services.AddRouting()     
            .AddResponseCaching()
            .AddResponseCompression()
    |> ignore

// Middleware
let configure                 
    (endpoints : HttpEndpoint list)
    (app : IApplicationBuilder) = 
            
    app.UseExceptionMiddleware(Host.defaultExceptionHandler)
        .UseResponseCaching()
        .UseResponseCompression()
        .UseStaticFiles()
        .UseRouting()
        .UseHttpEndPoints(endpoints)
        .UseNotFoundHandler(Host.defaultNotFoundHandler)
        |> ignore 

// Web Host
let configureWebHost : ConfigureWebHost =
  fun (endpoints : HttpEndPointList) 
      (host : IWebHostBuilder) ->
      webHost
           .UseKestrel()
           .ConfigureLogging(configureLogging)
           .ConfigureServices(configureServices)
           .Configure(configure endpoints)
           |> ignore

[<EntryPoint>]
let main args =    
    Host.startWebHost 
        args
        configureWebHost
        [
            // Endpoints go here
        ]
    0
```

## View Engine

A core feature of Falco is the functional view engine. Using it means:

- Writing your views in plain F#, directly in your assembly.
- Markup is compiled alongside the rest of your code, leading to improved performance and ultimately simpler deployments.

Most of the standard HTML tags & attributes have been mapped to F# functions, which produce objects to represent the HTML node. Nodes are either:
- `Text` which represents `string` values.
- `SelfClosingNode` which represent self-closing tags (i.e. `<br />`).
- `ParentNode` which represent typical tags with, optionally, other tags within it (i.e. `<div>...</div>`).

```f#
let doc = 
    Elem.html [ Attr.lang "en" ] [
            Elem.head [] [                    
                    Elem.title [] [ Text.raw "Sample App" ]                                                            
                ]
            Elem.body [] [                     
                    Elem.main [] [
                            Elem.h1 [] [ Text.raw "Sample App" ]
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
    Elem.html [ Attr.lang "en" ] [
            Elem.head [] [                    
                    Elem.title [] [ Text.raw "Sample App" ]                                                            
                ]
            Elem.body [] [                     
                    Elem.main [] [
                            Elem.h1 [] [ Text.raw "Sample App" ]
                            Elem.p  [] [ Text.raw (sprintf "%s %s" person.First person.Last)]
                        ]
                ]
        ]
```

Views can also be combined to create more complex views and share output:
```f#
let master (title : string) (content : XmlNode list) =
    Elem.html [ Attr.lang "en" ] [
            Elem.head [] [                    
                    Elem.title [] [ Text.raw "Sample App" ]                                                            
                ]
            Elem.body [] content
        ]

let divider = 
    Elem.hr [ Attr.class' "divider" ]

let homeView =
    [
        Elem.h1 [] [ Text.raw "Homepage" ]
        divider
        Elem.p  [] [ Text.raw "Lorem ipsum dolor sit amet, consectetur adipiscing."]
    ]
    |> master "Homepage" 

let aboutView =
    [
        Elem.h1 [] [ Text.raw "About" ]
        divider
        Elem.p  [] [ Text.raw "Lorem ipsum dolor sit amet, consectetur adipiscing."]
    ]
    |> master "About Us"
```

### Extending the view engine

The view engine is extremely extensible since creating new tags is simple. 

An example to render `<svg>`'s:

```f#
let svg (width : float) (height : float) =
    Elem.tag "svg" [
        Attr.create "version" "1.0"
        Attr.create "xmlns" "http://www.w3.org/2000/svg"
        Attr.create "viewBox" (sprintf "0 0 %f %f" width height)
    ]

let path d = Elem.tag "path" [ Attr.create "d" d ] []

let bars =
    svg 384.0 384.0 [
            path "M368 154.668H16c-8.832 0-16-7.168-16-16s7.168-16 16-16h352c8.832 0 16 7.168 16 16s-7.168 16-16 16zm0 0M368 32H16C7.168 32 0 24.832 0 16S7.168 0 16 0h352c8.832 0 16 7.168 16 16s-7.168 16-16 16zm0 0M368 277.332H16c-8.832 0-16-7.168-16-16s7.168-16 16-16h352c8.832 0 16 7.168 16 16s-7.168 16-16 16zm0 0"
        ]
```

## Model Binding

Binding at IO boundaries is messy, error-prone and often verbose. Reflection-based abstractions tend to work well for simple use cases, but quickly become very complicated as the expected complexity of the input rises. This is especially true for an algebraic type system like F#'s. As such, it is often advisable to take back control of this process from the runtime. An added bonus of doing this is that it all but eliminates the need for `[<CLIMutable>]` attributes.

We can make this simpler by creating a succinct API to obtain typed values from `IFormCollection` and `IQueryCollection`. 

> Methods are available for all primitive types, and perform **case-insenstivie** lookups against the collection.

```f#
/// An example handler, safely obtaining values from IFormCollection
let parseFormHandler : HttpHandler =
    fun ctx -> task {
        let! form = Request.getFormAsync() ctx

        let firstName = form.TryGetString "FirstName" // string -> string option        
        let lastName  = form.TryGet "LastName"        // alias for TryGetString
        let age       = form.TryGetInt "Age"          // string -> int option

        // Rest of handler ...
    }

/// An example handler, safely obtaining values from IQueryCollection
let parseQueryHandler : HttpHandler =
    fun ctx ->
        let query = Request.getQuery ctx

        let firstName = query.TryGetString "FirstName" // string -> string option        
        let lastName  = query.TryGet "LastName"        // alias for TryGetString
        let age       = query.TryGetInt "Age"          // string -> int option

        // Rest of handler ...
```

In this case where you don't care about gracefully handling non-existence. Or, you are certain values will be present, the dynamic operator `?` can be useful:

```f#
let parseQueryHandler : HttpHandler =
    fun ctx ->
        let query = Request.getQuery ctx

        // dynamic operator also case-insensitive
        let firstName = query?FirstName.AsString() // string -> string
        let lastName  = query?LastName.AsString()  // string -> string
        let age       = query?Age.AsInt16()        // string -> int16

        // Rest of handler ...
```

> Use of the `?` dynamic operator also performs **case-insenstive** lookups against the collection.

Further to this, helper functions are available to allow for the typical case of *try-or-fail* applying the provided binding function.

```f#
// An example handler which attempts to bind a form
let exampleTryBindFormHandler : HttpHandler =
    fun ctx ->
        let bindForm form =     
            {
              FirstName = form?FirstName.AsString()
              LastName  = form?LastName.AsString()
              Age       = form?Age.AsInt16()      
            }

        let respondWith =
            match Request.tryBindForm (bindForm >> Result.Ok) ctx with
            | Error error -> Response.ofPlainText error
            | Ok model    -> Response.ofPlainText (sprintf "%A" model)

        respondWith ctx

// An example handler which attempts to bind a query
let exampleTryBindQueryHandler : HttpHandler =
    fun ctx ->
        let bindQuery query =
            {
              FirstName = query?FirstName.AsString()
              LastName  = query?LastName.AsString()
              Age       = query?Age.AsInt16()      
            }

        let respondWith =
            match Request.tryBindQuery (bindQuery >> Result.Ok) ctx with
            | Error error -> Response.ofPlainText error
            | Ok model    -> Response.ofPlainText (sprintf "%A" model)

        respondWith ctx

// An example using a type and static binder, which can make things simpler
type SearchQuery =
    {
        Frag : string
        Page : int option
        Take : int
    }
    static member fromReader (r : StringCollectionReader) =
        {
            Frag = r?frag.AsString()
            Page = r.TryGetInt "page" |> Option.defaultValue 1
            Take = r?take.AsInt()
        }

let searchResultsHandler : HttpHandler =
    fun ctx ->
        let respondWith =
            match Request.tryBindQuery (SearchQuery.fromReader >> Result.Ok) ctx with
            | Error error -> Response.ofPlainText error
            | Ok model    -> Response.ofPlainText (sprintf "%A" model)

        respondWith ctx
```

## Authentication

ASP.NET Core has amazing built-in support for authentication. Review the [docs][13] for specific implementation details. Falco optionally (`open Falco.Auth`) includes some authentication utilites.

> To use the authentication helpers, ensure the service has been registered (`AddAuthentication()`) with the `IServiceCollection` and activated (`UseAuthentication()`) using the `IApplicationBuilder`. 

- Prevent user from accessing secure endpoint:

```f#
open Falco.Security

let secureResourceHandler : HttpHandler =
    fun ctx ->
        let respondWith =
            match Auth.isAuthenticated ctx with
            | false -> Response.redirect "/forbidden" false
            | true  -> Response.ofPlainText "hello authenticated user"

        respondWith ctx
```

- Prevent authenticated user from accessing anonymous-only end-point:

```f#
open Falco.Security
 
let anonResourceOnlyHandler : HttpHandler =
    fun ctx ->
        let respondWith =
            match Auth.isAuthenticated ctx with
            | true  -> Response.redirect "/forbidden" false
            | false -> Response.ofPlainText "hello anonymous"

        respondWith ctx
```

- Allow only user's from a certain group to access endpoint"
```f#
open Falco.Security

let secureResourceHandler : HttpHandler =
    fun ctx ->
        let isAuthenticated = Auth.isAuthenticated ctx
        let isAdmin = Auth.isInRole ["Admin"] ctx
        
        let respondWith =
            match isAuthenticated, isAdmin with
            | true, true -> Response.ofPlainText "hello admin"
            | _          -> Response.redirect "/forbidden" false

        respondWith ctx
```

- End user session (sign out):

```f#
open Falco.Security

let logOut : HttpHandler =         
    fun ctx -> 
        (Auth.signOutAsync Auth.authScheme ctx).Wait()
        Response.redirect Urls.``/login`` false ctx

// OR using task {}
let logOut : HttpHandler =         
    fun ctx -> task {
        do! Auth.signOutAsync Auth.authScheme ctx
        do! Response.redirect Urls.``/login`` false ctx
    }
```

## Security

Cross-site scripting attacks are extremely common, since they are quite simple to carry out. Fortunately, protecting against them is as easy as performing them. 

The [Microsoft.AspNetCore.Antiforgery][14] package provides the required utilities to easily protect yourself against such attacks.

Falco provides a few handlers via `Falco.Security.Xss`:

> To use the Xss helpers, ensure the service has been registered (`AddAntiforgery()`) with the `IServiceCollection` and activated (`UseAntiforgery()`) using the `IApplicationBuilder`. 

```f#
open Falco.Security 

let formView (token : AntiforgeryTokenSet) = 
    html [] [
            body [] [
                    form [ _method "post" ] [
                            // using the CSRF HTML helper
                            Xss.antiforgeryInput token
                            input [ _type "submit"; _value "Submit" ]
                        ]                                
                ]
        ]
    
// A handler that demonstrates obtaining a
// CSRF token and applying it to a view
let csrfViewHandler : HttpHandler = 
    fun ctx ->
        let csrfToken = Xss.getToken ctx        
        Response.ofHtml (formView token) ctx
    
// A handler that demonstrates validating
// the requests CSRF token
let isTokenValidHandler : HttpHander =
    fun ctx -> task {
        let! isTokenValid = Xss.validateToken ctx

        let respondWith =
            match isTokenValid with
            | false -> 
                Response.withStatusCode 400 
                >> Response.ofPlainText "Bad request"

            | true ->
                Response.ofPlainText "Good request"                

        return! respondWith ctx
    }
```

### Crytography

Many sites have the requirement of a secure log in and sign up (i.e. registering and maintaining a user's database). Thus, generating strong hashes and random salts is of critical importance. 

Falco helpers are accessed by importing `Falco.Auth.Crypto`.

```f#
open Falco.Security

// Generating salt,
// using System.Security.Cryptography.RandomNumberGenerator,
// create a random 16 byte salt and base 64 encode
let salt = Crypto.createSalt 16 

// Generate random int for iterations
let iterations = Crypto.randomInt 10000 50000

// Pbkdf2 Key derivation using HMAC algorithm with SHA256 hashing function
let password = "5upe45ecure"
let hashedPassword = password |> Crypto.sha256 iterations 32 salt
``` 

## Handling Large Uploads

Microsoft defines [large uploads][15] as anything **> 64KB**, which well... is most uploads. Anything beyond this size, and they recommend streaming the multipart data to avoid excess memory consumption.

To make this process **a lot** easier Falco exposes an `HttpContext` extension method `TryStreamFormAsync()` that will attempt to stream multipart form data, or return an error message indicating the likely problem.

```f#
let imageUploadHandler : HttpHandler =
    fun ctx -> task {
        let! form = Request.tryStreamFormAsync()
            
        // Rest of code using `FormCollectionReader`
        // ...
    }
```

## JSON

Included in Falco are basic JSON in/out handlers, `Request.bindJsonAsync<'a>` and `Response.ofJson` respectively. Both rely on `System.Text.Json`, thus without support for F#'s algebraic types. This was done purposefully in support of the belief that JSON in F# should be limited to primitive types only in the form of DTO records.

> Looking for a package to work with JSON? Checkout [Jay](https://github.com/pimbrouwers/Jay). 

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
[8]: https://github.com/pimbrouwers/Falco/tree/master/samples/Blog
[9]: https://en.wikipedia.org/wiki/Function_composition "Function composition"
[10]: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1 "ASP.NET Core Middlware"
[11]: https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/options "F# Options"
[12]: https://wiki.haskell.org/Combinator "Combinator"
[13]: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-3.1 "Overview of ASP.NET Core authentication"
[14]: https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-3.1 "Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core"
[15]: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1#upload-large-files-with-streaming "Large file uploads"
[16]: https://github.com/pimbrouwers/Falco/tree/master/src/Response.fs
[17]: https://github.com/pimbrouwers/Falco/tree/master/samples/Blog
[18]: https://github.com/pimbrouwers/Falco/tree/master/src/Request.fs
