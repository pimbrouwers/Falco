namespace Falco

[<AutoOpen>]
module Extensions =
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.Configuration
    open Microsoft.Extensions.DependencyInjection

    type HttpContext with
        /// Attempts to obtain dependency from IServiceCollection
        /// Throws InvalidDependencyException on missing.
        member x.Plug<'T>() =
            x.RequestServices.GetRequiredService<'T>()

    type WebApplicationBuilder with
        member x.AddIf(predicate : bool, fn : IConfiguration -> IServiceCollection -> IServiceCollection) : WebApplicationBuilder =
            if predicate then fn x.Configuration x.Services |> ignore
            x

    type WebApplication with
        /// Apply `fn` to `WebApplication :> IApplicationBuilder` if `predicate` is true.
        member x.UseIf(predicate : bool, fn : IApplicationBuilder -> IApplicationBuilder) : WebApplication =
            if predicate then fn x |> ignore
            x

        /// Analagous to `IApplicationBuilder.Use` but returns `WebApplication`.
        member x.Use(fn : IApplicationBuilder -> IApplicationBuilder) : WebApplication =
            x.UseIf(true, fn)

        /// Registers a Falco HttpHandler as exception handler lambda.
        /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
        member x.UseFalcoExceptionHandler(exceptionHandler : HttpHandler) : WebApplication =
            let configure (appBuilder : IApplicationBuilder) =
                appBuilder.Run(HttpHandler.toRequestDelegate exceptionHandler)

            x.UseExceptionHandler(configure) |> ignore
            x

        /// Activates Falco integration with IEndpointRouteBuilder.
        member x.UseFalco(?endpoints : HttpEndpoint seq) : WebApplication =
            x.UseRouting()
             .UseEndpoints(fun endpointBuilder ->
                let dataSource = FalcoEndpointDatasource(defaultArg endpoints [])
                endpointBuilder.DataSources.Add(dataSource)) |> ignore
            x

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

        member x.FalcoGet(pattern : string, handler : HttpHandler) : WebApplication =
            x.MapFalco({
                Pattern = pattern
                Handlers = [ GET, handler ] })

        member x.FalcoPost(pattern : string, handler : HttpHandler) : WebApplication =
            x.MapFalco({
                Pattern = pattern
                Handlers = [ POST, handler ] })

        member x.FalcoPut(pattern : string, handler : HttpHandler) : WebApplication =
            x.MapFalco({
                Pattern = pattern
                Handlers = [ PUT, handler ] })

        member x.FalcoDelete(pattern : string, handler : HttpHandler) : WebApplication =
            x.MapFalco({
                Pattern = pattern
                Handlers = [ DELETE, handler ] })

        member x.FalcoPatch(pattern : string, handler : HttpHandler) : WebApplication =
            x.MapFalco({
                Pattern = pattern
                Handlers = [ PATCH, handler ] })

        member x.FalcoAny(pattern : string, handler : HttpHandler) : WebApplication =
            x.MapFalco({
                Pattern = pattern
                Handlers = [ ANY, handler ] })
        
        /// Registers a Falco HttpHandler as terminal middleware (i.e., not found).
        member x.FalcoNotFound(handler : HttpHandler) : WebApplication =
            x.Run(handler = HttpHandler.toRequestDelegate handler) |> ignore
            x

    type FalcoExtensions =
        static member UseFalcoExceptionHandler
            (exceptionHandler : HttpHandler)
            (app : WebApplication) =
            app.UseFalcoExceptionHandler exceptionHandler
