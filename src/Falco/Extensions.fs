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
    member x.GetService<'a> () =
        let t = typeof<'a>
        match x.RequestServices.GetService t with
        | null    -> raise (InvalidDependencyException t.Name)
        | service -> service :?> 'a

    /// Obtain a named instance of ILogger
    member x.GetLogger (name : string) =
        let loggerFactory = x.GetService<ILoggerFactory>()
        loggerFactory.CreateLogger name


/// IEndpointRouteBuilder extensions
type IEndpointRouteBuilder with
    member x.UseFalcoEndpoints (endpoints : HttpEndpoint list) =
        let dataSource = FalcoEndpointDatasource(endpoints)
        x.DataSources.Add(dataSource)


/// IApplicationBuilder extensions
type IApplicationBuilder with
    /// Determine if the application is running in development mode
    member x.IsDevelopment () =
        x.ApplicationServices.GetService<IWebHostEnvironment>().IsDevelopment()

    /// Activate Falco integration with IEndpointRouteBuilder
    member x.UseFalco (endpoints : HttpEndpoint list) =
        x.UseRouting()
            .UseEndpoints(fun r -> r.UseFalcoEndpoints(endpoints))

    /// Register a Falco HttpHandler as exception handler lambda
    /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
    member x.UseFalcoExceptionHandler (exceptionHandler : HttpHandler) =
        let configure (appBuilder : IApplicationBuilder) =
            appBuilder.Run(HttpHandler.toRequestDelegate exceptionHandler)

        x.UseExceptionHandler(configure)

    /// Executes function against IApplicationBuidler if the predicate returns true
    member x.UseWhen (predicate : bool, fn : IApplicationBuilder -> IApplicationBuilder) =
        if predicate then fn x
        else x


/// IServiceCollection Extensions
type IServiceCollection with
    /// Adds default Falco services to the ASP.NET Core service container.
    member x.AddFalco () =
        x.AddRouting()

    /// Adds default Falco services to the ASP.NET Core service container.
    member x.AddFalco(routeOptions : RouteOptions -> unit) =
        x.AddRouting(Action<RouteOptions>(routeOptions))

    /// Executes function against IServiceCollection if the predicate returns true
    member x.AddWhen (predicate : bool, fn : IServiceCollection -> IServiceCollection) =
        if predicate then fn x
        else x

type FalcoExtensions =
    static member IsDevelopment : IApplicationBuilder -> bool =
        fun app -> app.IsDevelopment()

    static member UseFalcoExceptionHandler (exceptionHandler : HttpHandler) (app : IApplicationBuilder) =
        app.UseFalcoExceptionHandler exceptionHandler