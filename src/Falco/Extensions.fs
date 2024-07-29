namespace Falco

[<AutoOpen>]
module Extensions =
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Http
    open Microsoft.AspNetCore.Routing
    open Microsoft.Extensions.Configuration
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.Logging

    type HttpContext with
        /// Attempts to obtain dependency from IServiceCollection
        /// Throws InvalidDependencyException on missing.
        member this.Plug<'T>() =
            this.RequestServices.GetRequiredService<'T>()

    type WebApplicationBuilder with
        member this.AddConfiguration(fn : IConfigurationBuilder -> IConfigurationBuilder) : WebApplicationBuilder =
            fn this.Configuration |> ignore
            this

        member this.AddLogging(fn : ILoggingBuilder -> ILoggingBuilder) : WebApplicationBuilder =
            fn this.Logging |> ignore
            this

        /// Apply `fn` to `WebApplicationBuilder.Services :> IServiceCollection`  if `predicate` is true.
        member this.AddServicesIf(predicate : bool, fn : IConfiguration -> IServiceCollection -> IServiceCollection) : WebApplicationBuilder =
            if predicate then fn this.Configuration this.Services |> ignore
            this

        member this.AddServices(fn : IConfiguration -> IServiceCollection -> IServiceCollection) : WebApplicationBuilder =
            this.AddServicesIf(true, fn)

    type IEndpointRouteBuilder with
        member this.MapFalco(configure : FalcoEndpointBuilder -> unit) : IEndpointConventionBuilder =
            let dataSource = FalcoEndpointDatasource([])
            let falcoEndpointBuilder = FalcoEndpointBuilder(dataSource)
            configure falcoEndpointBuilder
            this.DataSources.Add(dataSource)
            dataSource

        member this.MapFalco(endpoints : HttpEndpoint seq) : IEndpointConventionBuilder =
            this.MapFalco(fun endpointBuilder ->
                for endpoint in endpoints do
                    for (verb, handler) in endpoint.Handlers do
                        match verb with
                        | GET -> endpointBuilder.FalcoGet(endpoint.Pattern, handler)
                        | HEAD -> endpointBuilder.FalcoHead(endpoint.Pattern, handler)
                        | POST -> endpointBuilder.FalcoPost(endpoint.Pattern, handler)
                        | PUT -> endpointBuilder.FalcoPut(endpoint.Pattern, handler)
                        | PATCH -> endpointBuilder.FalcoPatch(endpoint.Pattern, handler)
                        | DELETE -> endpointBuilder.FalcoDelete(endpoint.Pattern, handler)
                        | OPTIONS -> endpointBuilder.FalcoOptions(endpoint.Pattern, handler)
                        | TRACE -> endpointBuilder.FalcoTrace(endpoint.Pattern, handler)
                        | ANY -> endpointBuilder.FalcoAny(endpoint.Pattern, handler)
                        |> ignore)

        member this.MapFalcoGet(pattern, handler) : IEndpointConventionBuilder =
            this.MapFalco(fun endpointBuilder -> 
                endpointBuilder.FalcoGet(pattern, handler)
                |> ignore)

    type IApplicationBuilder with
        /// Registers a `Falco.HttpHandler` as exception handler lambda.
        /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
        member this.UseFalcoExceptionHandler(exceptionHandler : HttpHandler) : IApplicationBuilder =
            let configure (appBuilder : IApplicationBuilder) =
                appBuilder.Run(HttpHandler.toRequestDelegate exceptionHandler)

            this.UseExceptionHandler(configure) |> ignore
            this

    type WebApplication with
        /// Apply `fn` to `WebApplication :> IApplicationBuilder` if `predicate` is true.
        member this.UseIf(predicate : bool, fn : IApplicationBuilder -> IApplicationBuilder) : WebApplication =
            if predicate then fn this |> ignore
            this

        /// Analagous to `IApplicationBuilder.Use` but returns `WebApplication`.
        member this.Use(fn : IApplicationBuilder -> IApplicationBuilder) : WebApplication =
            this.UseIf(true, fn)

        /// Activates Falco integration with IEndpointRouteBuilder.
        member this.UseFalco(configure : FalcoEndpointBuilder -> unit) : WebApplication =
            this.UseRouting()
                .UseEndpoints(fun endpointBuilder ->
                    endpointBuilder.MapFalco(configure)
                    |> ignore)
                |> ignore
            this

        /// Activates Falco integration with IEndpointRouteBuilder.
        member this.UseFalco(endpoints : HttpEndpoint seq) : WebApplication =
            this.UseEndpoints(fun endpointBuilder ->
                endpointBuilder.MapFalco(endpoints)
                |> ignore)
                |> ignore
            this

        /// Registers a `Falco.HttpHandler` as exception handler lambda.
        /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
        member this.UseFalcoExceptionHandler(exceptionHandler : HttpHandler) : WebApplication =
            (this :> IApplicationBuilder).UseFalcoExceptionHandler(exceptionHandler) |> ignore
            this

        /// Registers a `Falco.HttpHandler` as terminal middleware (i.e., not found).
        member this.FalcoNotFound(handler : HttpHandler) : WebApplication =
            this.Run(handler = HttpHandler.toRequestDelegate handler) |> ignore
            this

    type FalcoExtensions =
        /// Registers a `Falco.HttpHandler` as global exception handler.
        static member UseFalcoExceptionHandler
            (exceptionHandler : HttpHandler)
            (app : IApplicationBuilder) =
            app.UseFalcoExceptionHandler exceptionHandler
