namespace Falco

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Routing

/// Represents a missing dependency, thrown on request.
exception InvalidDependencyException of string

[<AutoOpen>]
module Extensions =
    type IEndpointRouteBuilder with
        /// Activates Falco Endpoint integration.
        member x.UseFalcoEndpoints (endpoints : HttpEndpoint seq) =
            let dataSource = FalcoEndpointDatasource(endpoints)
            x.DataSources.Add(dataSource)

    type IApplicationBuilder with
        /// Activates Falco integration with IEndpointRouteBuilder.
        member x.UseFalco (endpoints : HttpEndpoint seq) =
            x.UseEndpoints(fun r -> r.UseFalcoEndpoints(endpoints))

        /// Registers a Falco HttpHandler as exception handler lambda.
        /// See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?#exception-handler-lambda
        member x.UseFalcoExceptionHandler (exceptionHandler : HttpHandler) =
            let configure (appBuilder : IApplicationBuilder) =
                appBuilder.Run(HttpHandler.toRequestDelegate exceptionHandler)

            x.UseExceptionHandler(configure)

type FalcoExtensions =
    static member UseFalcoExceptionHandler
        (exceptionHandler : HttpHandler)
        (app : IApplicationBuilder) =
        app.UseFalcoExceptionHandler exceptionHandler