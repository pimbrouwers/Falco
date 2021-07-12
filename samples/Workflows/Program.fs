module Workflows.Program

open System
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Http

// Exception Handler
// ------------
let exceptionHandler : HttpHandler =
    Response.withStatusCode 500 
    >> Response.ofPlainText "Server error"

// Dependencies
// ------------
type IterationCounter() =
    let mutable i : int = 0

    member _.Increment = fun () ->
        i <- i + 1
        i    

    member _.Reset = fun () -> 
        i <- 0

type Root = { Counter : IterationCounter } with
    interface IDisposable with
        member x.Dispose() = ()

module Root =
    let ofHttpContext (ctx : HttpContext) =
        let counter = ctx.GetService<IterationCounter>()
        { Counter = counter }
        
// Echo server
// ------------
type Envelope<'a> = { IterationNumber : int; Data : 'a}

// Reading from the Query String
type QueryInput = { Page : int; Frag : string }

let readQuery : HttpHandler = 
    let queryBinder (q : QueryCollectionReader) =
        let page = q.GetInt32 "page" 1
        let frag = q.GetString "frag" ""
        { Page = page; Frag = frag }

    let workflow (root : Root) (input : QueryInput) =
        let i = root.Counter.Increment ()       
        match i with
        | i when i <= 5 -> Ok { IterationNumber = i; Data = input }
        | _ -> 
            root.Counter.Reset() 
            Error ()

    let handleOk = Response.ofJson 

    let handleError _ _ = 
        Response.withStatusCode 500 
        >> Response.ofPlainText "Server Error - could not read query"

    Workflow.ofQuery Root.ofHttpContext queryBinder workflow handleOk handleError

// Read from a form submission
type FormInput = { Email : string; First : string; Last : string } 

let readForm : HttpHandler = 
    let formBinder (f : FormCollectionReader) =
        let email = f.GetString "email" ""
        let first = f.GetString "first" ""
        let last = f.GetString "last" ""
        { Email = email; First = first; Last = last }

    let workflow (root : Root) (input : FormInput) =
        let i = root.Counter.Increment ()        
        match i with
        | i when i <= 5 -> Ok { IterationNumber = i; Data = input }
        | _ -> 
            root.Counter.Reset() 
            Error ()

    let handleOk = Response.ofJson 

    let handleError _ _ = 
        Response.withStatusCode 500 
        >> Response.ofPlainText "Server Error - could not read form"

    Workflow.ofForm Root.ofHttpContext formBinder workflow handleOk handleError

// Read from JSON
type JsonInput = { Title : string; Author : string; ISBN : int} 

let readJson : HttpHandler = 
    let workflow (root : Root) (input : JsonInput ) =
        let i = root.Counter.Increment ()        
        match i with
        | i when i <= 5 -> Ok { IterationNumber = i; Data = input }
        | _ -> 
            root.Counter.Reset() 
            Error ()

    let handleOk = Response.ofJson 

    let handleError _ _ = 
        Response.withStatusCode 500 
        >> Response.ofPlainText "Server Error - could not read json"

    Workflow.ofJson Root.ofHttpContext workflow handleOk handleError


[<EntryPoint>]
let main args =   
    webHost args {
        add_service (fun services -> services.AddSingleton<IterationCounter>())
        
        use_if    FalcoExtensions.IsDevelopment DeveloperExceptionPageExtensions.UseDeveloperExceptionPage
        use_ifnot FalcoExtensions.IsDevelopment (FalcoExtensions.UseFalcoExceptionHandler exceptionHandler)
                
        endpoints [            
            get  "/" (Response.ofPlainText "Hello world")

            get "/query" readQuery

            post "/form" readForm

            post "/json" readJson
        ]
    }
    0