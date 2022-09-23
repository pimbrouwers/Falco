namespace Falco

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.FileProviders
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Falco.StringUtils

/// Represents a missing dependency, thrown on request.
exception InvalidDependencyException of string

[<Sealed>]
type internal FalcoEndpointDatasource(httpEndpoints : HttpEndpoint list) =
    inherit EndpointDataSource()

    [<Literal>]
    let DefaultOrder = 0

    let endpoints =
        [| for endpoint in httpEndpoints do
            let routePattern = Patterns.RoutePatternFactory.Parse endpoint.Pattern

            for (verb, handler) in endpoint.Handlers do
                let routeNameMetadata = RouteNameMetadata(endpoint.Pattern)

                let verbStr = verb.ToString()
                let displayName = if strEmpty verbStr then endpoint.Pattern else strConcat [|verbStr; " "; endpoint.Pattern|]
                let httpMethodMetadata =
                    match verb with
                    | ANY -> HttpMethodMetadata [||]
                    | _   -> HttpMethodMetadata [|verbStr|]

                let metadata = EndpointMetadataCollection(routeNameMetadata, httpMethodMetadata)

                let requestDelegate = HttpHandler.toRequestDelegate handler

                RouteEndpoint(requestDelegate, routePattern, DefaultOrder, metadata, displayName) :> Endpoint |]

    override _.Endpoints = endpoints :> _

    override _.GetChangeToken() = NullChangeToken.Singleton :> _

[<AutoOpen>]
module Extensions =
    type HttpContext with
        /// Attempt to obtain depedency from IServiceCollection
        /// Throws InvalidDependencyException on null.
        member x.GetService<'a>() =
            let t = typeof<'a>
            match x.RequestServices.GetService t with
            | null    -> raise (InvalidDependencyException t.Name)
            | service -> service :?> 'a

        /// Obtain a named instance of ILogger.
        member x.GetLogger (name : string) =
            let loggerFactory = x.GetService<ILoggerFactory>()
            loggerFactory.CreateLogger name

    type IEndpointRouteBuilder with
        /// Activate Falco Endpoint integration
        member x.UseFalcoEndpoints (endpoints : HttpEndpoint list) =
            let dataSource = FalcoEndpointDatasource(endpoints)
            x.DataSources.Add(dataSource)

    type IApplicationBuilder with
        /// Determine if the application is running in development mode
        member x.IsDevelopment() =
            x.ApplicationServices.GetService<IWebHostEnvironment>().IsDevelopment()

        /// Activate Falco integration with IEndpointRouteBuilder
        member x.UseFalco (endpoints : HttpEndpoint list) =
            x.UseRouting()
             .UseEndpoints(fun r -> r.UseFalcoEndpoints(endpoints))

        /// Register a Falco HttpHandler as exception handler lambda.
        /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
        member x.UseFalcoExceptionHandler (exceptionHandler : HttpHandler) =
            let configure (appBuilder : IApplicationBuilder) =
                appBuilder.Run(HttpHandler.toRequestDelegate exceptionHandler)

            x.UseExceptionHandler(configure)

        /// Executes function against IApplicationBuidler if the predicate returns
        /// true.
        member x.UseWhen
            (predicate : bool, fn : IApplicationBuilder -> IApplicationBuilder) =
            if predicate then fn x
            else x

    type IServiceCollection with
        /// Adds default Falco services to the ASP.NET Core service container.
        member x.AddFalco() =
            x.AddRouting()

        /// Adds default Falco services to the ASP.NET Core service container.
        member x.AddFalco (routeOptions : RouteOptions -> unit) =
            x.AddRouting(Action<RouteOptions>(routeOptions))

        /// Executes function against IServiceCollection if the predicate returns
        /// true.
        member x.AddWhen
            (predicate : bool, fn : IServiceCollection -> IServiceCollection) =
            if predicate then fn x
            else x

    let getService<'a> (ctx : HttpContext) =
        ctx.GetService<'a> ()

    let getLogger (name : string) (ctx : HttpContext) =
        ctx.GetLogger name

type FalcoExtensions =
    static member IsDevelopment : IApplicationBuilder -> bool =
        fun app -> app.IsDevelopment()

    static member UseFalcoExceptionHandler
        (exceptionHandler : HttpHandler)
        (app : IApplicationBuilder) =
        app.UseFalcoExceptionHandler exceptionHandler