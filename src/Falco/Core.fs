[<AutoOpen>]
module Falco.Core

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.FileProviders
open Falco.StringUtils

let inline internal continueWith (continuation : Task<'a> -> 'b) (task : Task<'a>) =  
    task.ContinueWith(continuation, TaskContinuationOptions.OnlyOnRanToCompletion)

let inline internal continueWithTask (continuation : Task<'a> -> Task<'b>) (task : Task<'a>) =
    let taskResult = task |> continueWith continuation
    taskResult.Wait () 
    taskResult.Result

let inline internal onCompleteWithUnitTask (continuation : Task<'a> -> Task) (task : Task<'a>) =
    let taskResult = task |> continueWith continuation
    taskResult.Wait () 
    taskResult.Result

let inline internal onCompleteFromUnitTask (continuation : Task -> Task) (task : Task) =  
    let taskResult = task.ContinueWith(continuation, TaskContinuationOptions.OnlyOnRanToCompletion)
    taskResult.Wait ()
    taskResult.Result

// ------------
// Constants
// ------------
module Constants =
    let defaultJsonOptions =
        let options = Text.Json.JsonSerializerOptions()
        options.AllowTrailingCommas <- true
        options.PropertyNameCaseInsensitive <- true 
        options

// ------------
// Errors
// ------------

/// Represents a missing dependency, thrown on request
exception InvalidDependencyException of string

// ------------
// HTTP
// ------------

/// Http verb
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

    override x.ToString() =
        match x with
        | GET     -> HttpMethods.Get
        | HEAD    -> HttpMethods.Head
        | POST    -> HttpMethods.Post
        | PUT     -> HttpMethods.Put
        | PATCH   -> HttpMethods.Patch
        | DELETE  -> HttpMethods.Delete
        | OPTIONS -> HttpMethods.Options
        | TRACE   -> HttpMethods.Trace
        | ANY     -> String.Empty
 
module HttpVerb = 
    let toHttpMethodMetadata verb = 
        let verbStr = verb.ToString()
        match verb with 
        | ANY -> HttpMethodMetadata [||]
        | _   -> HttpMethodMetadata [|verbStr|]       

/// The eventual return of asynchronous HttpContext processing
type HttpHandler = 
    HttpContext -> Task

module HttpHandler =
    /// Convert HttpHandler to a RequestDelegate
    let toRequestDelegate (handler : HttpHandler) =        
        new RequestDelegate(handler)

/// In-and-out processing of a HttpContext
type HttpResponseModifier = HttpContext -> HttpContext

/// Specifies an association of a route pattern to a collection of HttpEndpointHandler
type HttpEndpoint = 
    { Pattern  : string   
      Handlers : (HttpVerb * HttpHandler) list }

/// The process of associating a route and handler
type MapHttpEndpoint = string -> HttpHandler -> HttpEndpoint

[<Sealed>]
type internal FalcoEndpointDatasource(httpEndpoints : HttpEndpoint list) =
    inherit EndpointDataSource()

    [<Literal>]
    let defaultOrder = 0

    let endpoints = 
        [| for endpoint in httpEndpoints do            
            let routePattern = Patterns.RoutePatternFactory.Parse endpoint.Pattern

            for (verb, handler) in endpoint.Handlers do                   
                let requestDelegate = HttpHandler.toRequestDelegate handler 
                let verbStr = verb.ToString()           
                let displayName = if strEmpty verbStr then endpoint.Pattern else strConcat [|verbStr; " "; endpoint.Pattern|]                
                let httpMethod = HttpVerb.toHttpMethodMetadata verb                                       
                let metadata = EndpointMetadataCollection(httpMethod)                
                RouteEndpoint(requestDelegate, routePattern, defaultOrder, metadata, displayName) :> Endpoint |]

    override _.Endpoints = endpoints :> _
    override _.GetChangeToken() = NullChangeToken.Singleton :> _