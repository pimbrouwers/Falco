namespace Falco

[<AutoOpen>]
module Extensions =
    open System
    open System.Reflection
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
        member this.MapFalcoEndpoints(configure : FalcoEndpointBuilder -> unit) : IEndpointConventionBuilder =
            let dataSource = FalcoEndpointDatasource([])
            let falcoEndpointBuilder = FalcoEndpointBuilder(dataSource)
            configure falcoEndpointBuilder
            this.DataSources.Add(dataSource)
            dataSource

        member this.MapFalcoEndpoints(endpoints : HttpEndpoint seq) : IEndpointConventionBuilder =
            this.MapFalcoEndpoints(fun endpointBuilder ->
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

    type IApplicationBuilder with
        /// Apply `fn` to `WebApplication :> IApplicationBuilder` if `predicate` is true.
        member this.UseIf(predicate : bool, fn : IApplicationBuilder -> IApplicationBuilder) : IApplicationBuilder =
            if predicate then fn this |> ignore
            this

        /// Analagous to `IApplicationBuilder.Use` but returns `WebApplication`.
        member this.Use(fn : IApplicationBuilder -> IApplicationBuilder) : IApplicationBuilder =
            this.UseIf(true, fn)

        /// Activates Falco integration with IEndpointRouteBuilder.
        member this.UseFalco(configure : FalcoEndpointBuilder -> unit) : IApplicationBuilder =
            this.UseRouting()
                .UseEndpoints(fun endpointBuilder ->
                    endpointBuilder.MapFalcoEndpoints(configure)
                    |> ignore)
                |> ignore
            this

        /// Activates Falco integration with IEndpointRouteBuilder.
        ///
        /// This is the default way to enable the package.
        member this.UseFalco(endpoints : HttpEndpoint seq) : IApplicationBuilder =
            this.UseRouting()
                .UseEndpoints(fun endpointBuilder ->
                    endpointBuilder.MapFalcoEndpoints(endpoints)
                    |> ignore)
                |> ignore
            this

        member this.UseFalco() : IApplicationBuilder =
            // discover endpoints via attribution
            let exportedTypes = Assembly.GetEntryAssembly().GetExportedTypes()
            let endpoints =
                exportedTypes
                |> Seq.collect (fun x -> x.GetMethods())
                |> Seq.filter (fun x -> x.ReturnType.Name = "HttpEndpoint")
                |> Seq.map (fun x -> x.Invoke(null, [||]) :?> HttpEndpoint)
                |> List.ofSeq

            this.UseFalco(endpoints)

        /// Registers a `Falco.HttpHandler` as terminal middleware (i.e., not found).
        member this.FalcoNotFound(handler : HttpHandler) : IApplicationBuilder =
            this.Run(handler = HttpHandler.toRequestDelegate handler) |> ignore
            this

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
            (this :> IApplicationBuilder).UseIf(predicate, fn) |> ignore
            this

        /// Analagous to `IApplicationBuilder.Use` but returns `WebApplication`.
        member this.Use(fn : IApplicationBuilder -> IApplicationBuilder) : WebApplication =
            this.UseIf(true, fn)

        /// Activates Falco integration with IEndpointRouteBuilder.
        member this.UseFalco(configure : FalcoEndpointBuilder -> unit) : WebApplication =
            (this :> IApplicationBuilder).UseFalco(configure) |> ignore
            this

        /// Activates Falco integration with IEndpointRouteBuilder.
        ///
        /// This is the default way to enable the package.
        member this.UseFalco(endpoints : HttpEndpoint seq) : WebApplication =
            (this :> IApplicationBuilder).UseFalco(endpoints) |> ignore
            this

        member this.UseFalco() : WebApplication =
            (this :> IApplicationBuilder).UseFalco() |> ignore
            this

        /// Registers a `Falco.HttpHandler` as exception handler lambda.
        /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
        member this.UseFalcoExceptionHandler(exceptionHandler : HttpHandler) : WebApplication =
            (this :> IApplicationBuilder).UseFalcoExceptionHandler(exceptionHandler) |> ignore
            this

        /// Registers a `Falco.HttpHandler` as terminal middleware (i.e., not found).
        member this.FalcoNotFound(handler : HttpHandler) : WebApplication =
            (this :> IApplicationBuilder).FalcoNotFound(handler) |> ignore
            this

    type FalcoExtensions =
        /// Registers a `Falco.HttpHandler` as global exception handler.
        static member UseFalcoExceptionHandler
            (exceptionHandler : HttpHandler)
            (app : IApplicationBuilder) =
            app.UseFalcoExceptionHandler exceptionHandler
