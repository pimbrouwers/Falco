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
type IterationCounter () =
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
type Envelope<'a> = { Error : string; IterationNumber : int; Data : 'a}

// Shared error handler
let rateLimitReached input _ : HttpHandler =
    Response.withStatusCode 429 
    >> Response.ofJson  { Error = "Rate limit reached"; IterationNumber = -1; Data = input }

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
        | i when i <= 5 -> Ok { Error = ""; IterationNumber = i; Data = input }
        | _ -> 
            root.Counter.Reset() 
            Error ()

    let handleOk = Response.ofJson

    Workflow.ofQuery Root.ofHttpContext queryBinder workflow handleOk rateLimitReached

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
        | i when i <= 5 -> Ok { Error = ""; IterationNumber = i; Data = input }
        | _ -> 
            root.Counter.Reset() 
            Error ()

    let handleOk = Response.ofJson 

    Workflow.ofForm Root.ofHttpContext formBinder workflow handleOk rateLimitReached

// Read from JSON
type JsonInput = { Title : string; Author : string; ISBN : int} 

let readJson : HttpHandler = 
    let workflow (root : Root) (input : JsonInput ) =
        let i = root.Counter.Increment ()        
        match i with
        | i when i <= 5 -> Ok { Error = ""; IterationNumber = i; Data = input }
        | _ -> 
            root.Counter.Reset() 
            Error ()

    let handleOk = Response.ofJson 

    Workflow.ofJson Root.ofHttpContext workflow handleOk rateLimitReached


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