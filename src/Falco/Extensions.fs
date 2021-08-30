[<AutoOpen>]
module Falco.Extensions

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Falco.Routing

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
        let dataSource = FalcoEndpointDatasource(endpoints)
        this.DataSources.Add(dataSource)


/// IApplicationBuilder extensions
type IApplicationBuilder with
    /// Determine if the application is running in development mode
    member this.IsDevelopment () =
        this.ApplicationServices.GetService<IWebHostEnvironment>().IsDevelopment()

    /// Activate Falco integration with IEndpointRouteBuilder
    member this.UseFalco (endpoints : HttpEndpoint list) =
        this.UseRouting()
            .UseEndpoints(fun r -> r.UseFalcoEndpoints(endpoints))
    
    /// Register a Falco HttpHandler as exception handler lambda
    /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
    member this.UseFalcoExceptionHandler (exceptionHandler : HttpHandler) =
        this.UseExceptionHandler (fun (errApp : IApplicationBuilder) -> errApp.Run(HttpHandler.toRequestDelegate exceptionHandler))

    /// Executes function against IApplicationBuidler if the predicate returns true
    member this.UseWhen (predicate : bool, fn : IApplicationBuilder -> IApplicationBuilder) =
        if predicate then fn this
        else this


/// IServiceCollection Extensions
type IServiceCollection with
    /// Adds default Falco services to the ASP.NET Core service container.
    member this.AddFalco () =
        this.AddRouting()

    /// Adds default Falco services to the ASP.NET Core service container.
    member this.AddFalco(routeOptions : RouteOptions -> unit) =
        this.AddRouting(Action<RouteOptions>(routeOptions))

    /// Executes function against IServiceCollection if the predicate returns true
    member this.AddWhen (predicate : bool, fn : IServiceCollection -> IServiceCollection) =
        if predicate then fn this
        else this

type FalcoExtensions = 
    static member IsDevelopment : IApplicationBuilder -> bool =
        fun app -> app.IsDevelopment()

    static member UseFalcoExceptionHandler (exceptionHandler : HttpHandler) (app : IApplicationBuilder) =
        app.UseFalcoExceptionHandler exceptionHandler