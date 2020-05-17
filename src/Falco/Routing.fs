[<AutoOpen>]
module Falco.Routing

open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Falco.StringParser

/// Specifies an association of an HttpHandler to an HttpVerb and route pattern
type HttpVerb = 
    | GET 
    | HEAD
    | POST 
    | PUT 
    | PATCH
    | DELETE 
    | OPTIONS
    | TRACE
    | ANY

/// Specifies an HttpEndpoint
type HttpEndpoint = 
    {
        Pattern : string   
        Verb  : HttpVerb
        Handler : HttpHandler
    }
       
/// Create a RequestDelegate from HttpHandler
let createRequestDelete (handler : HttpHandler) =
    let fn = handler defaultHttpFunc
    RequestDelegate(fun ctx -> Task.Run(fun _ -> fn ctx |> ignore))

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
            
    /// Enable Falco not found handler (this handler is terminal)
    member this.UseNotFoundHandler (notFoundHandler : HttpHandler) =
        this.Run(createRequestDelete notFoundHandler)

/// Constructor for HttpEndpoint
let route (verb : HttpVerb) (pattern : string) (handler : HttpHandler) =
    { 
        Pattern = pattern
        Verb  = verb
        Handler = handler
    }

/// HttpEndpoint constructor that matches any HttpVerb
let any (pattern : string) (handler : HttpHandler)     = route ANY pattern handler
    
/// GET HttpEndpoint constructor
let get (pattern : string) (handler : HttpHandler)     = route GET pattern handler

/// HEAD HttpEndpoint constructor
let head (pattern : string) (handler : HttpHandler)    = route HEAD pattern handler

/// POST HttpEndpoint constructor
let post (pattern : string) (handler : HttpHandler)    = route POST pattern handler

/// PUT HttpEndpoint constructor
let put (pattern : string) (handler : HttpHandler)     = route PUT pattern handler

/// PATCH HttpEndpoint constructor
let patch (pattern : string) (handler : HttpHandler)   = route PATCH pattern handler

/// DELETE HttpEndpoint constructor
let delete (pattern : string) (handler : HttpHandler)  = route DELETE pattern handler

/// OPTIONS HttpEndpoint constructor
let options (pattern : string) (handler : HttpHandler) = route OPTIONS pattern handler

/// TRACE HttpEndpoint construct
let trace (pattern : string) (handler : HttpHandler)   = route TRACE pattern handler

type HttpContext with        
    /// Obtain Map<string,string> of current route values
    member this.GetRouteValues () =
        this.Request.RouteValues
        |> Seq.map (fun kvp -> kvp.Key, toStr kvp.Value)
        |> Map.ofSeq

    /// The HttpVerb of the current request
    member this.HttpVerb = 
        match this.Request.Method with 
        | m when strEquals m HttpMethods.Get     -> GET
        | m when strEquals m HttpMethods.Head    -> HEAD
        | m when strEquals m HttpMethods.Post    -> POST
        | m when strEquals m HttpMethods.Put     -> PUT
        | m when strEquals m HttpMethods.Patch   -> PATCH
        | m when strEquals m HttpMethods.Delete  -> DELETE
        | m when strEquals m HttpMethods.Options -> OPTIONS
        | m when strEquals m HttpMethods.Trace   -> TRACE
        | _ -> ANY
    
    /// Attempt to safely-acquire route value
    member this.TryGetRouteValue (key : string) =
        let parseRoute = tryParseWith this.Request.RouteValues.TryGetValue             
        match parseRoute key with
        | Some v -> Some (toStr v)
        | None   -> None
        