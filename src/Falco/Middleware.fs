[<AutoOpen>]
module Falco.Middleware

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http

type IApplicationBuilder with
    /// Activate Falco integration with IEndpointRouteBuilder
    member this.UseHttpEndPoints (endPoints : HttpEndpoint list) =
        this.UseEndpoints(fun r -> 
                for e in endPoints do            
                    let rd = createRequestDelete e.Handler
                    
                    match e.Verb with
                    | GET     -> r.MapGet(e.Pattern, rd)
                    | HEAD    -> r.MapMethods(e.Pattern, [ HttpMethods.Head ], rd)
                    | POST    -> r.MapPost(e.Pattern, rd)
                    | PUT     -> r.MapPut(e.Pattern, rd)
                    | PATCH   -> r.MapMethods(e.Pattern, [ HttpMethods.Patch ], rd)
                    | DELETE  -> r.MapDelete(e.Pattern, rd)
                    | OPTIONS -> r.MapMethods(e.Pattern, [ HttpMethods.Options ], rd)
                    | TRACE   -> r.MapMethods(e.Pattern, [ HttpMethods.Trace ], rd)
                    | ANY     -> r.Map(e.Pattern, rd)
                    |> ignore)
        
    /// Enable Falco exception handling middleware
    member this.UseExceptionMiddleware (exceptionHandler : ExceptionHandler) =
        this.UseMiddleware<ExceptionHandlingMiddleware> exceptionHandler
        
    /// Enable Falco not found handler (this handler is terminal)
    member this.UseNotFoundHandler (notFoundHandler : HttpHandler) =
        this.Run(createRequestDelete notFoundHandler)

