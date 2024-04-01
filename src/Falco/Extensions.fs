namespace Falco

[<AutoOpen>]
module Extensions =
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.Configuration
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.Logging

    type HttpContext with
        /// Attempts to obtain dependency from IServiceCollection
        /// Throws InvalidDependencyException on missing.
        member x.Plug<'T>() =
            x.RequestServices.GetRequiredService<'T>()

    type WebApplicationBuilder with
        member x.AddConfiguration(fn : IConfigurationBuilder -> IConfigurationBuilder) : WebApplicationBuilder =
            fn x.Configuration |> ignore
            x

        member x.AddLogging(fn : ILoggingBuilder -> ILoggingBuilder) : WebApplicationBuilder =
            fn x.Logging |> ignore
            x

        /// Apply `fn` to `WebApplicationBuilder.Services :> IServiceCollection`  if `predicate` is true.
        member x.AddServicesIf(predicate : bool, fn : IConfiguration -> IServiceCollection -> IServiceCollection) : WebApplicationBuilder =
            if predicate then fn x.Configuration x.Services |> ignore
            x

        member x.AddServices(fn : IConfiguration -> IServiceCollection -> IServiceCollection) : WebApplicationBuilder =
            x.AddServicesIf(true, fn)

    type IApplicationBuilder with
        /// Registers a `Falco.HttpHandler` as exception handler lambda.
        /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
        member x.UseFalcoExceptionHandler(exceptionHandler : HttpHandler) : IApplicationBuilder =
            let configure (appBuilder : IApplicationBuilder) =
                appBuilder.Run(HttpHandler.toRequestDelegate exceptionHandler)

            x.UseExceptionHandler(configure) |> ignore
            x

    type WebApplication with
        /// Apply `fn` to `WebApplication :> IApplicationBuilder` if `predicate` is true.
        member x.UseIf(predicate : bool, fn : IApplicationBuilder -> IApplicationBuilder) : WebApplication =
            if predicate then fn x |> ignore
            x

        /// Analagous to `IApplicationBuilder.Use` but returns `WebApplication`.
        member x.Use(fn : IApplicationBuilder -> IApplicationBuilder) : WebApplication =
            x.UseIf(true, fn)

        /// Activates Falco integration with IEndpointRouteBuilder.
        member x.UseFalco(?endpoints : HttpEndpoint seq) : WebApplication =
            x.UseRouting()
             .UseEndpoints(fun endpointBuilder ->
                let dataSource = FalcoEndpointDatasource(defaultArg endpoints [])
                endpointBuilder.DataSources.Add(dataSource)) |> ignore
            x

        /// Registers a `Falco.HttpHandler` as exception handler lambda.
        /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
        member x.UseFalcoExceptionHandler(exceptionHandler : HttpHandler) : WebApplication =
            (x :> IApplicationBuilder).UseFalcoExceptionHandler(exceptionHandler) |> ignore
            x

        /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder`.
        member x.MapFalco(endpoint : HttpEndpoint) : WebApplication =
            for (verb, handler) in endpoint.Handlers do
                match verb with
                | GET -> x.MapGet(endpoint.Pattern, handler)
                | HEAD -> x.MapMethods(endpoint.Pattern, [HttpMethods.Head], handler)
                | POST -> x.MapPost(endpoint.Pattern, handler)
                | PUT -> x.MapPut(endpoint.Pattern, handler)
                | PATCH -> x.MapPatch(endpoint.Pattern, handler)
                | DELETE -> x.MapDelete(endpoint.Pattern, handler)
                | OPTIONS -> x.MapMethods(endpoint.Pattern, [HttpMethods.Options], handler)
                | TRACE -> x.MapMethods(endpoint.Pattern, [HttpMethods.Trace], handler)
                | ANY -> x.Map(endpoint.Pattern, handler)
                |> ignore
            x

        /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches HTTP GET requests for the specified pattern.
        member x.FalcoGet(pattern : string, handler : HttpHandler) : WebApplication =
            x.MapFalco({
                Pattern = pattern
                Handlers = [ GET, handler ] })

        /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches HTTP POST requests for the specified pattern.
        member x.FalcoPost(pattern : string, handler : HttpHandler) : WebApplication =
            x.MapFalco({
                Pattern = pattern
                Handlers = [ POST, handler ] })

        /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches HTTP PUT requests for the specified pattern.
        member x.FalcoPut(pattern : string, handler : HttpHandler) : WebApplication =
            x.MapFalco({
                Pattern = pattern
                Handlers = [ PUT, handler ] })

        /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches HTTP DELETE requests for the specified pattern.
        member x.FalcoDelete(pattern : string, handler : HttpHandler) : WebApplication =
            x.MapFalco({
                Pattern = pattern
                Handlers = [ DELETE, handler ] })

        /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches HTTP PATCH requests for the specified pattern.
        member x.FalcoPatch(pattern : string, handler : HttpHandler) : WebApplication =
            x.MapFalco({
                Pattern = pattern
                Handlers = [ PATCH, handler ] })

        /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches all HTTP requests for the specified pattern.
        member x.FalcoAny(pattern : string, handler : HttpHandler) : WebApplication =
            x.MapFalco({
                Pattern = pattern
                Handlers = [ ANY, handler ] })

        /// Adds a `Falco.HttpEndpoint` to the `Microsoft.AspNetCore.Routing.IEndpointRouteBuilder` that matches the provided HTTP requests for the specified pattern.
        member x.FalcoAll(pattern : string, handlers : (HttpVerb * HttpHandler) seq) : WebApplication =
            x.MapFalco({
                Pattern = pattern
                Handlers = handlers })

        /// Registers a `Falco.HttpHandler` as terminal middleware (i.e., not found).
        member x.FalcoNotFound(handler : HttpHandler) : WebApplication =
            x.Run(handler = HttpHandler.toRequestDelegate handler) |> ignore
            x

    type FalcoExtensions =
        /// Registers a `Falco.HttpHandler` as global exception handler.
        static member UseFalcoExceptionHandler
            (exceptionHandler : HttpHandler)
            (app : IApplicationBuilder) =
            app.UseFalcoExceptionHandler exceptionHandler
