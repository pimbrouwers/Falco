[<AutoOpen>]
module Falco.Routing

open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Falco.StringParser

/// Create a RequestDelegate from HttpHandler
let createRequestDelete (handler : HttpHandler) =
    let fn = handler (Some >> Task.FromResult)
    RequestDelegate(fun ctx -> Task.Run(fun _ -> fn ctx |> ignore))

/// Specifies an association of an HttpHandler to an HttpVerb and route pattern
type HttpVerb = GET | POST | PUT | DELETE | ANY

/// Specifies an HttpEndpoint
type HttpEndpoint = 
    {
        Pattern : string   
        Verb  : HttpVerb
        Handler : HttpHandler
    }
       
type IApplicationBuilder with
    /// Activate Falco integration with IEndpointRouteBuilder
    member this.UseHttpEndPoints (endPoints : HttpEndpoint list) =
        this.UseRouting()
            .UseEndpoints(fun r -> 
                for e in endPoints do            
                    let rd = createRequestDelete e.Handler

                    match e.Verb with
                    | GET    -> r.MapGet(e.Pattern, rd)
                    | POST   -> r.MapPost(e.Pattern, rd)
                    | PUT    -> r.MapPut(e.Pattern, rd)
                    | DELETE -> r.MapDelete(e.Pattern, rd)
                    | _      -> r.Map(e.Pattern, rd)
                    |> ignore)
            
    /// Enable Falco not found handler (this handler is terminal)
    member this.UseNotFoundHandler (notFoundHandler : HttpHandler) =
        this.Run(createRequestDelete notFoundHandler)

/// Constructor for HttpEndpoint
let route method pattern handler = 
    { 
        Pattern = pattern
        Verb  = method
        Handler = handler
    }

/// GET HttpEndpoint constructor
let get pattern handler    = route GET pattern handler

/// POST HttpEndpoint constructor
let post pattern handler   = route POST pattern handler

/// PUT HttpEndpoint constructor
let put pattern handler    = route PUT pattern handler

/// DELETE HttpEndpoint constructor
let delete pattern handler = route DELETE pattern handler

/// HttpEndpoint constructor that matches any HttpVerb
let any pattern handler    = route ANY pattern handler
    
type HttpContext with        
    /// Obtain Map<string,string> of current route values
    member this.GetRouteValues () =
        this.Request.RouteValues
        |> Seq.map (fun kvp -> kvp.Key, toStr kvp.Value)
        |> Map.ofSeq
    
    /// Attempt to safely-acquire route value
    member this.TryGetRouteValue (key : string) =
        let parseRoute = parseWith this.Request.RouteValues.TryGetValue             
        match parseRoute key with
        | Some v -> Some (toStr v)
        | None   -> None
        