﻿[<RequireQualifiedAccess>]
module Falco.Workflow

open System
open Microsoft.AspNetCore.Http

/// An HttpHandler to execute services, and can help reduce code
/// repetition by acting as a composition root for injecting
/// dependencies for logging, database, smtp etc.
let execute
    (getRoot : HttpContext -> 'root when 'root :> IDisposable)
    (workflow : 'root -> 'input -> Result<'output, 'error>)    
    (handleOk : 'output -> HttpHandler)
    (handleError : 'input -> 'error -> HttpHandler)
    (input : 'input) : HttpHandler =
    fun ctx ->  
        // Gather dependencies
        let root = getRoot ctx
        
        let respondWith = 
            match workflow root input with
            | Ok output -> handleOk output
            | Error error -> handleError input error

        // Dispose dependencies
        root.Dispose()

        respondWith ctx

/// Map a query string and run the provided workflow
let ofQuery getRoot queryBinder workflow handleOk handleError : HttpHandler =            
    Request.mapQuery
        queryBinder 
        (execute getRoot workflow handleOk handleError) 

/// Map route params and run the provided workflow
let ofRoute getRoot routeBinder workflow handleOk handleError : HttpHandler =            
    Request.mapRoute
        routeBinder 
        (execute getRoot workflow handleOk handleError)    

/// Map a form collection and run the provided workflow
let ofForm getRoot formBinder workflow handleOk handleError : HttpHandler=        
    Request.mapForm
        formBinder
        (execute getRoot workflow handleOk handleError)        

/// Securely map a form collection and run the provided workflow
let ofFormSecure getRoot formBinder workflow handleOk handleError handleInvalidCsrfToken : HttpHandler=
    Request.mapFormSecure 
        formBinder
        (execute getRoot workflow handleOk handleError)
        handleInvalidCsrfToken

/// Map a JSON request and run the provided workflow
let ofJson getRoot workflow handleOk handleError : HttpHandler =
    Request.mapJson        
        (execute getRoot workflow handleOk handleError)        