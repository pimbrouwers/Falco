[<AutoOpen>]
module Falco.Core

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http

// ------------
// TaskBuilder.fs overrides 
// ------------
#nowarn "44"
type FSharp.Control.Tasks.TaskBuilder.TaskBuilderV2 with
    /// A bolt-on member to automatically convert Task<unit> result to Task
    member inline __.Run(f : unit -> FSharp.Control.Tasks.TaskBuilder.Step<unit>) = (FSharp.Control.Tasks.TaskBuilder.run f) :> Task

type FSharp.Control.Tasks.TaskBuilder.TaskBuilder with
    /// A bolt-on member to automatically convert Task<unit> result to Task
    member inline __.Run(f : unit -> FSharp.Control.Tasks.TaskBuilder.Step<unit>) = (FSharp.Control.Tasks.TaskBuilder.run f) :> Task

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
    {
        Pattern  : string   
        Handlers : (HttpVerb * HttpHandler) list
    }

/// The process of associating a route and handler
type MapHttpEndpoint = string -> HttpHandler -> HttpEndpoint

// ------------
// Workflows
// ------------

/// Work to be done that has input and will generate output or an error.
type Workflow<'input, 'output, 'error> = 'input -> Result<'output, 'error>

/// Composition root for workflows which is automatically
/// disposed of when exeuction is complete.
type IWorkflowRoot =
    inherit IDisposable    