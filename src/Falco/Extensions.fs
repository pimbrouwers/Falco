namespace Falco

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers
open Falco.Multipart

/// Represents a missing dependency, thrown on request.
exception InvalidDependencyException of string

[<AutoOpen>]
module Extensions =
    type HttpContext with
        /// Attempts to obtain dependency from IServiceCollection
        /// Throws InvalidDependencyException on null.
        member x.GetService<'T>() =
            x.RequestServices.GetRequiredService<'T>()

        /// Obtains a named instance of ILogger.
        member x.GetLogger (name : string) =
            let loggerFactory = x.GetService<ILoggerFactory>()
            loggerFactory.CreateLogger name

    type IEndpointRouteBuilder with
        /// Activates Falco Endpoint integration.
        member x.UseFalcoEndpoints (endpoints : HttpEndpoint list) =
            let dataSource = FalcoEndpointDatasource(endpoints)
            x.DataSources.Add(dataSource)

    type IApplicationBuilder with
        /// Determines if the application is running in development mode.
        member x.IsDevelopment() =
            x.ApplicationServices.GetService<IWebHostEnvironment>().IsDevelopment()

        /// Activates Falco integration with IEndpointRouteBuilder.
        member x.UseFalco (endpoints : HttpEndpoint list) =
            x.UseRouting()
             .UseEndpoints(fun r -> r.UseFalcoEndpoints(endpoints))

        /// Registers a Falco HttpHandler as exception handler lambda.
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

    type HttpRequest with
        /// Determines if the content type contains multipart.
        member internal x.IsMultipart () : bool =
            x.ContentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0

        member private x.GetBoundary() =
            // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
            // The spec at https://tools.ietf.org/html/rfc2046#section-5.1 states that 70 characters is a reasonable limit.
            let lengthLimit = 70
            let contentType = MediaTypeHeaderValue.Parse(StringSegment(x.ContentType))
            let boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
            match boundary with
            | b when isNull b               -> None
            | b when b.Length > lengthLimit -> None
            | b                             -> Some b

        /// Attempts to stream the HttpRequest body into IFormCollection.
        member x.StreamFormAsync () : Task<IFormCollection> =
            task {
                match x.IsMultipart(), x.GetBoundary() with
                | true, Some boundary ->
                    let multipartReader = new MultipartReader(boundary, x.Body)
                    let! formCollection = multipartReader.StreamSectionsAsync()
                    return formCollection

                | _, None
                | false, _ -> return FormCollection.Empty
            }


type FalcoExtensions =
    static member IsDevelopment : IApplicationBuilder -> bool =
        fun app -> app.IsDevelopment()

    static member UseFalcoExceptionHandler
        (exceptionHandler : HttpHandler)
        (app : IApplicationBuilder) =
        app.UseFalcoExceptionHandler exceptionHandler

type Services =
    static member withLogger name next : HttpHandler = fun ctx ->
        next (ctx.GetLogger name) ctx

    static member inject<'T1> next : HttpHandler = fun ctx ->
        next
            (ctx.GetService<'T1>())
            ctx

    static member inject<'T1, 'T2> next : HttpHandler = fun ctx ->
        next
            (ctx.GetService<'T1>())
            (ctx.GetService<'T2>())
            ctx

    static member inject<'T1, 'T2, 'T3> next : HttpHandler = fun ctx ->
        next
            (ctx.GetService<'T1>())
            (ctx.GetService<'T2>())
            (ctx.GetService<'T3>())
            ctx

    static member inject<'T1, 'T2, 'T3, 'T4> next : HttpHandler = fun ctx ->
        next
            (ctx.GetService<'T1>())
            (ctx.GetService<'T2>())
            (ctx.GetService<'T3>())
            (ctx.GetService<'T4>())
            ctx

    static member inject<'T1, 'T2, 'T3, 'T4, 'T5> next : HttpHandler = fun ctx ->
        next
            (ctx.GetService<'T1>())
            (ctx.GetService<'T2>())
            (ctx.GetService<'T3>())
            (ctx.GetService<'T4>())
            (ctx.GetService<'T5>())
            ctx
