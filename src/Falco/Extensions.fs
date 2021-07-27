[<AutoOpen>]
module Falco.Extensions

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

/// HttpContext extension methods
type HttpContext with
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


/// IEndpointRouteBuilder extensions
type IEndpointRouteBuilder with
    member this.UseFalcoEndpoints (endpoints : HttpEndpoint list) =
        for endpoint in endpoints do                           
            for (verb, handler) in endpoint.Handlers do                          
                let requestDelegate = HttpHandler.toRequestDelegate handler
            
                match verb with
                | GET     -> this.MapGet(endpoint.Pattern, requestDelegate)
                | HEAD    -> this.MapMethods(endpoint.Pattern, [ HttpMethods.Head ], requestDelegate)
                | POST    -> this.MapPost(endpoint.Pattern, requestDelegate)
                | PUT     -> this.MapPut(endpoint.Pattern, requestDelegate)
                | PATCH   -> this.MapMethods(endpoint.Pattern, [ HttpMethods.Patch ], requestDelegate)
                | DELETE  -> this.MapDelete(endpoint.Pattern, requestDelegate)
                | OPTIONS -> this.MapMethods(endpoint.Pattern, [ HttpMethods.Options ], requestDelegate)
                | TRACE   -> this.MapMethods(endpoint.Pattern, [ HttpMethods.Trace ], requestDelegate)
                | ANY     -> this.Map(endpoint.Pattern, requestDelegate)
                |> ignore


/// IApplicationBuilder extensions
type IApplicationBuilder with
    /// Activate Falco integration with IEndpointRouteBuilder
    member this.UseFalco (endpoints : HttpEndpoint list) =
        this.UseRouting()
            .UseEndpoints(fun r -> r.UseFalcoEndpoints(endpoints))
    
    /// Register a Falco HttpHandler as exception handler lambda
    /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
    member this.UseFalcoExceptionHandler (exceptionHandler : HttpHandler) =
        this.UseExceptionHandler(fun (errApp : IApplicationBuilder) -> errApp.Run(HttpHandler.toRequestDelegate exceptionHandler))

    /// Executes function against IApplicationBuidler if the predicate returns true
    member this.UseWhen (predicate : bool, fn : IApplicationBuilder -> IApplicationBuilder) =
        if predicate then fn this
        else this


/// IServiceCollection Extensions
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