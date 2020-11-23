[<AutoOpen>]
module Falco.Extensions

open System
open System.IO
open System.Security.Claims
open System.Text
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Antiforgery    
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers
open Falco.StringUtils

type HttpRequest with       
    /// The HttpVerb of the current request
    member this.HttpVerb = 
        match this.Method with 
        | m when strEquals m HttpMethods.Get     -> GET
        | m when strEquals m HttpMethods.Head    -> HEAD
        | m when strEquals m HttpMethods.Post    -> POST
        | m when strEquals m HttpMethods.Put     -> PUT
        | m when strEquals m HttpMethods.Patch   -> PATCH
        | m when strEquals m HttpMethods.Delete  -> DELETE
        | m when strEquals m HttpMethods.Options -> OPTIONS
        | m when strEquals m HttpMethods.Trace   -> TRACE
        | _ -> ANY

    /// Retrieve the HttpRequest body as string
    member this.GetBodyAsync () = task {
        use rd = new StreamReader(this.Body)
        return! rd.ReadToEndAsync()
    }

    /// Retrieve StringCollectionReader for IFormCollection from HttpRequest
    member this.GetFormReaderAsync () = task {
        let! form = this.ReadFormAsync()        
        return FormCollectionReader(form, None)
    }        
    
    /// Obtain HeaderValues for the current request
    member this.GetHeaderReader () : HeaderCollectionReader =
        HeaderCollectionReader(this.Headers)

    /// Retrieve StringCollectionReader for IQueryCollection from HttpRequest
    member this.GetQueryReader () = 
        QueryCollectionReader(this.Query)
                
    /// Obtain RouteValues for the current request
    member this.GetRouteReader () : RouteCollectionReader =
        RouteCollectionReader(this.RouteValues)

type HttpResponse with
    /// Set HttpResponse header
    member this.SetHeader 
        (name : string) 
        (content : string) =            
        if not(this.Headers.ContainsKey(name)) then
            this.Headers.Add(name, StringValues(content))

    /// Set HttpResponse ContentType header
    member this.SetContentType contentType =
        this.SetHeader HeaderNames.ContentType contentType

    member this.SetStatusCode (statusCode : int) =            
        this.StatusCode <- statusCode
            
    /// Write bytes to HttpResponse body
    member this.WriteBytes (bytes : byte[]) =
        let byteLen = bytes.Length
        this.ContentLength <- Nullable<int64>(byteLen |> int64)
        this.Body.WriteAsync(bytes, 0, byteLen)
        
    /// Write UTF8 string to HttpResponse body
    member this.WriteString (encoding : Encoding) (httpBodyStr : string) =
        let httpBodyBytes = encoding.GetBytes httpBodyStr
        this.WriteBytes httpBodyBytes

    /// Append a new cookie with value
    member this.AddCookie (key : string) (value : string) =        
        this.Cookies.Append(key, value)

    /// Append a new cookie with value and options
    member this.AddCookieOptions (key : string) (value : string) (cookieOptions : CookieOptions) =
        this.Cookies.Append(key, value, cookieOptions)

type HttpContext with       
    // ------------
    // IoC & Logging
    // ------------

    /// Attempt to obtain depedency from IServiceCollection
    /// Throws InvalidDependencyException on null
    member this.GetService<'a> () =
        let t = typeof<'a>
        match this.RequestServices.GetService t with
        | null    -> raise (InvalidDependencyException t.Name)
        | service -> service :?> 'a

    /// Obtain a named instance of ILogger
    member this.GetLogger (name : string) =
        let loggerFactory = this.GetService<ILoggerFactory>()
        loggerFactory.CreateLogger name

    // ------------
    // XSS
    // ------------

    /// Returns (and optional creates) csrf tokens for the current session
    member this.GetCsrfToken () =
        let antiFrg = this.GetService<IAntiforgery>()
        antiFrg.GetAndStoreTokens this

    /// Checks the presence and validity of CSRF token 
    member this.ValidateCsrfToken () =
        let antiFrg = this.GetService<IAntiforgery>()        
        antiFrg.IsRequestValidAsync this

    // ------------
    // Auth
    // ------------
    /// Returns the current user (IPrincipal) or None
    member this.GetUser () =
        match this.User with
        | null -> None
        | _    -> Some this.User

    /// Returns authentication status of IPrincipal, false on null
    member this.IsAuthenticated () =
        let isAuthenciated (user : ClaimsPrincipal) = 
            let identity = user.Identity
            match identity with 
            | null -> false
            | _    -> identity.IsAuthenticated

        match this.GetUser () with
        | None      -> false 
        | Some user -> isAuthenciated user

type IApplicationBuilder with
    /// Activate Falco integration with IEndpointRouteBuilder
    member this.UseFalco (endpoints : HttpEndpoint list) =
        this.UseRouting()
            .UseEndpoints(fun r -> 
                for endpoint in endpoints do                           
                    for handler in endpoint.Handlers do                          
                        let requestDelegate = HttpHandler.toRequestDelegate handler.HttpHandler
                    
                        match handler.Verb with
                        | GET     -> r.MapGet(endpoint.Pattern, requestDelegate)
                        | HEAD    -> r.MapMethods(endpoint.Pattern, [ HttpMethods.Head ], requestDelegate)
                        | POST    -> r.MapPost(endpoint.Pattern, requestDelegate)
                        | PUT     -> r.MapPut(endpoint.Pattern, requestDelegate)
                        | PATCH   -> r.MapMethods(endpoint.Pattern, [ HttpMethods.Patch ], requestDelegate)
                        | DELETE  -> r.MapDelete(endpoint.Pattern, requestDelegate)
                        | OPTIONS -> r.MapMethods(endpoint.Pattern, [ HttpMethods.Options ], requestDelegate)
                        | TRACE   -> r.MapMethods(endpoint.Pattern, [ HttpMethods.Trace ], requestDelegate)
                        | ANY     -> r.Map(endpoint.Pattern, requestDelegate)
                        |> ignore)
    
    /// Register a Falco HttpHandler as exception handler lambda
    /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
    member this.UseFalcoExceptionHandler (exceptionHandler : HttpHandler) =
        this.UseExceptionHandler(fun (errApp : IApplicationBuilder) -> errApp.Run(HttpHandler.toRequestDelegate exceptionHandler))

    /// Executes function against IApplicationBuidler if the predicate returns true
    member this.UseWhen (predicate : bool, fn : IApplicationBuilder -> IApplicationBuilder) =
        if predicate then fn this
        else this

type IServiceCollection with        
    /// Adds default Falco services to the ASP.NET Core service container.
    member this.AddFalco() =        
        this.AddRouting()

    /// Adds default Falco services to the ASP.NET Core service container.
    member this.AddFalco(routeOptions : RouteOptions -> unit) =
        this.AddRouting(Action<RouteOptions>(routeOptions))

    /// Executes function against IServiceCollection if the predicate returns true
    member this.AddWhen (predicate : bool, fn : IServiceCollection -> IServiceCollection) =
        if predicate then fn this
        else this