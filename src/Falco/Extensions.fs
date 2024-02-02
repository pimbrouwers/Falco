namespace Falco

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers
open Falco.Multipart

/// Represents a missing dependency, thrown on request.
exception InvalidDependencyException of string

[<AutoOpen>]
module Extensions =
    type IEndpointRouteBuilder with
        /// Activates Falco Endpoint integration.
        member x.UseFalcoEndpoints (endpoints : HttpEndpoint list) =
            let dataSource = FalcoEndpointDatasource(endpoints)
            x.DataSources.Add(dataSource)

    type IApplicationBuilder with
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

    type HttpRequest with
        /// Determines if the content type contains multipart.
        member private x.IsMultipart () : bool =
            x.ContentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0

        member private x.GetBoundary() =
            // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
            // The spec at https://tools.ietf.org/html/rfc2046#section-5.1 states that 70 characters is a reasonable limit.
            let lengthLimit = 70
            let contentType = MediaTypeHeaderValue.Parse(StringSegment(x.ContentType))
            let boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
            match boundary with
            | b when isNull b -> None
            | b when b.Length > lengthLimit -> None
            | b -> Some b

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
    static member UseFalcoExceptionHandler
        (exceptionHandler : HttpHandler)
        (app : IApplicationBuilder) =
        app.UseFalcoExceptionHandler exceptionHandler