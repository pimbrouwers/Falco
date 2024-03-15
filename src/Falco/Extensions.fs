namespace Falco

[<AutoOpen>]
module Extensions =
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Http
    open Microsoft.AspNetCore.Routing
    open Microsoft.Extensions.DependencyInjection

    type HttpContext with
        /// Attempts to obtain dependency from IServiceCollection
        /// Throws InvalidDependencyException on null.
        member x.GetService<'T>() =
            x.RequestServices.GetRequiredService<'T>()

    type IEndpointRouteBuilder with
        /// Activates Falco Endpoint integration.
        member x.UseFalcoEndpoints(endpoints : HttpEndpoint seq) =
            let dataSource = FalcoEndpointDatasource(endpoints)
            x.DataSources.Add(dataSource)

    type IApplicationBuilder with
        /// Activates Falco integration with IEndpointRouteBuilder.
        member x.UseFalco(endpoints : HttpEndpoint seq) =
            x.UseRouting()
             .UseEndpoints(fun r -> r.UseFalcoEndpoints(endpoints))

        /// Registers a Falco HttpHandler as exception handler lambda.
        /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
        member x.UseFalcoExceptionHandler(exceptionHandler : HttpHandler) =
            let configure (appBuilder : IApplicationBuilder) =
                appBuilder.Run(HttpHandler.toRequestDelegate exceptionHandler)

            x.UseExceptionHandler(configure)

    type WebApplication with
        member x.UseFalco(endpoints : HttpEndpoint seq) =
            (x :> IApplicationBuilder).UseFalco(endpoints) |> ignore
            x

    type FalcoExtensions =
        static member UseFalcoExceptionHandler
            (exceptionHandler : HttpHandler)
            (app : IApplicationBuilder) =
            app.UseFalcoExceptionHandler exceptionHandler
