[<RequireQualifiedAccess>]
module Falco.Request

open System
open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Falco.Multipart
open Falco.Security
open Falco.StringUtils

/// Obtain the HttpVerb of the request
let getVerb (ctx : HttpContext) : HttpVerb =
    match ctx.Request.Method with
    | m when strEquals m HttpMethods.Get     -> GET
    | m when strEquals m HttpMethods.Head    -> HEAD
    | m when strEquals m HttpMethods.Post    -> POST
    | m when strEquals m HttpMethods.Put     -> PUT
    | m when strEquals m HttpMethods.Patch   -> PATCH
    | m when strEquals m HttpMethods.Delete  -> DELETE
    | m when strEquals m HttpMethods.Options -> OPTIONS
    | m when strEquals m HttpMethods.Trace   -> TRACE
    | _ -> ANY

/// Retrieve a specific header from the request
let getHeaders (ctx : HttpContext) : HeaderCollectionReader  =
    HeaderCollectionReader(ctx.Request.Headers)

/// Retrieve all route values from the request as RouteCollectionReader
let getRoute (ctx : HttpContext) : RouteCollectionReader =
    RouteCollectionReader(ctx.Request.RouteValues, ctx.Request.Query)

let tryBindRoute
    (binder : RouteCollectionReader -> Result<'a, 'b>)
    (ctx : HttpContext) : Result<'a, 'b> =
    getRoute ctx
    |> binder

/// Retrieve the query string from the request as an instance of QueryCollectionReader
let getQuery (ctx : HttpContext) : QueryCollectionReader =
    QueryCollectionReader(ctx.Request.Query)

/// Attempt to bind query collection
let tryBindQuery
    (binder : QueryCollectionReader -> Result<'a, 'b>)
    (ctx : HttpContext) : Result<'a, 'b> =
    getQuery ctx
    |> binder

/// Retrieve the form collection from the request as an instance of FormCollectionReader
let getForm (ctx : HttpContext) : Task<FormCollectionReader> =    
    let continuation (formTask : Task<IFormCollection>) = 
        let form = formTask.Result
        let files = if isNull(form.Files) then None else Some form.Files
        FormCollectionReader(form, files)

    ctx.Request.ReadFormAsync ()
    |> continueWith continuation

/// Attempt to bind the form collection
let tryBindForm 
    (binder : FormCollectionReader -> Result<'a, 'b>)
    (ctx : HttpContext) : Task<Result<'a, 'b>> =         
    let continuation (formTask : Task<FormCollectionReader>) = binder formTask.Result                

    getForm ctx
    |> continueWith continuation

/// Attempt to stream the form collection for multipart form submissions
let tryStreamForm
    (ctx : HttpContext) : Task<Result<FormCollectionReader, string>> =
    ctx.Request.TryStreamFormAsync()

/// Stream the form collection for multipart form submissions
let streamForm
    (ctx : HttpContext) : Task<FormCollectionReader> =
    ctx.Request.StreamFormAsync()

/// Attempt to bind the form collection
let tryBindFormStream
    (binder : FormCollectionReader -> Result<'a, 'b>)
    (ctx : HttpContext) : Task<Result<'a, 'b>> =        
    let continuation (formTask : Task<FormCollectionReader>) = binder formTask.Result

    streamForm ctx
    |> continueWith continuation
    

/// Retrieve the cookie from the request as an instance of CookieCollectionReader
let getCookie (ctx : HttpContext) : CookieCollectionReader =
    CookieCollectionReader(ctx.Request.Cookies)

/// Attempt to bind cookie collection
let tryBindCookie
    (binder : CookieCollectionReader -> Result<'a, 'b>)
    (ctx : HttpContext) : Result<'a, 'b> =
    getCookie ctx
    |> binder

/// Attempt to bind request body using System.Text.Json and provided JsonSerializerOptions
let tryBindJsonOptions<'a>
    (options : JsonSerializerOptions)
    (ctx : HttpContext) : Task<Result<'a, string>> = 
    let continuation (jsonTask : Task<'a>) = Ok jsonTask.Result        
    let jsonValueTask = JsonSerializer.DeserializeAsync<'a>(ctx.Request.Body, options)
        
    if jsonValueTask.IsCompletedSuccessfully then 
        jsonValueTask.AsTask() |> continueWith continuation        
    else 
        Error "Invalid JSON" |> Task.FromResult

/// Attempt to bind request body using System.Text.Json
let tryBindJson<'a>
    (ctx : HttpContext) : Task<Result<'a, string>> =
    tryBindJsonOptions Constants.defaultJsonOptions ctx

// ------------
// Handlers
// ------------

/// Attempt to bind JSON request body onto 'a and provide
/// to handleOk, otherwise provide handleError with error string
let bindJson
    (handleOk : 'a -> HttpHandler)
    (handleError : string -> HttpHandler) : HttpHandler = fun ctx ->                
    let continuation (bindTask : Task<Result<'a, string>>) =                        
        let handler = 
            match bindTask.Result with
            | Error error -> handleError error 
            | Ok form -> handleOk form

        handler ctx

    tryBindJson ctx |> onCompleteWithUnitTask continuation
    

/// Attempt to bind the route values map onto 'a and provide
/// to handleOk, otherwise provide handleError with 'b
let bindRoute
    (binder : RouteCollectionReader -> Result<'a, 'b>)
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler) : HttpHandler = fun ctx ->
    let route = tryBindRoute binder ctx
    let respondWith =
        match route with
        | Error error -> handleError error
        | Ok query -> handleOk query

    respondWith ctx

/// Attempt to bind the QueryCollectionReader onto 'a and provide
/// to handleOk, otherwise provide handleError with 'b
let bindQuery
    (binder : QueryCollectionReader -> Result<'a, 'b>)
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler) : HttpHandler =
    fun ctx ->
        let query = tryBindQuery binder ctx
        let respondWith =
            match query with
            | Error error -> handleError error
            | Ok query -> handleOk query

        respondWith ctx

/// Attempt to bind the CookieCollectionReader onto 'a and provide
/// to handleOk, otherwise provide handleError with 'b
let bindCookie
    (binder : CookieCollectionReader -> Result<'a, 'b>)
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler) : HttpHandler = fun ctx ->
    let query = tryBindCookie binder ctx
    let respondWith =
        match query with
        | Error error -> handleError error
        | Ok query -> handleOk query

    respondWith ctx

/// Validate the CSRF of the current request
let validateCsrfToken
    (handleOk : HttpHandler)
    (handleInvalidToken : HttpHandler) : HttpHandler = fun ctx ->
    let continuation (xssTask : Task<bool>) = 
        let respondWith =
            match xssTask.Result with
            | true  -> handleOk
            | false -> handleInvalidToken

        respondWith ctx

    Xss.validateToken ctx |> onCompleteWithUnitTask continuation    

/// Attempt to bind the FormCollectionReader onto 'a and provide
/// to handleOk, otherwise provide handleError with 'b
let bindForm
    (binder : FormCollectionReader -> Result<'a, 'b>)
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler) : HttpHandler = fun ctx -> 
    let continuation (bindTask : Task<Result<'a, 'b>>) =
        let respondWith =
            match bindTask.Result with
            | Error error -> handleError error
            | Ok form -> handleOk form
        respondWith ctx 

    tryBindForm binder ctx |> onCompleteWithUnitTask continuation

/// Attempt to bind a streamed (i.e., multipart/form-data) 
/// FormCollectionReader  onto 'a and provide to handleOk, 
/// otherwise provide handleError with 'b
let bindFormStream
    (binder : FormCollectionReader -> Result<'a, 'b>)
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler) : HttpHandler = fun ctx -> 
    let continuation (bindTask : Task<Result<'a, 'b>>) =
        let respondWith =
            match bindTask.Result with
            | Error error -> handleError error
            | Ok form -> handleOk form

        respondWith ctx

    tryBindFormStream binder ctx |> onCompleteWithUnitTask continuation
    
/// Validate the CSRF of the current request attempt to bind the
/// FormCollectionReader onto 'a and provide to handleOk,
/// otherwise provide handleError with 'b
let bindFormSecure
    (binder : FormCollectionReader -> Result<'a, 'b>)
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler)
    (handleInvalidToken : HttpHandler) : HttpHandler =
    validateCsrfToken
        (bindForm binder handleOk handleError)
        handleInvalidToken

/// Validate the CSRF of the current request then atempt 
/// to bind a streamed (i.e., multipart/form-data) 
/// FormCollectionReader onto 'a and provide to handleOk, 
/// otherwise provide handleError with 'b
let bindFormStreamSecure
    (binder : FormCollectionReader -> Result<'a, 'b>)
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler)
    (handleInvalidToken : HttpHandler) : HttpHandler =
        bindFormStream 
            binder 
            (fun x -> validateCsrfToken (handleOk x) handleInvalidToken) 
            handleError
        

/// Bind JSON request body onto 'a and provide to next
/// Httphandler, throws exception if JsonException 
/// occurs during deserialization.
let mapJson (next : 'a -> HttpHandler) : HttpHandler = fun ctx -> 
    let continuation (bindTask : Task<Result<'a, string>>) =
        let respondWith =
            match bindTask.Result with
            | Error error -> failwithf "Could not bind JSON: %s" error
            | Ok json -> next json

        respondWith ctx 

    tryBindJson ctx |> onCompleteWithUnitTask continuation

/// Project RouteCollectionReader onto 'a and provide
/// to next HttpHandler
let mapRoute
    (map : RouteCollectionReader -> 'a)
    (next : 'a -> HttpHandler) : HttpHandler = fun ctx -> 
    next (getRoute ctx |> map) ctx

/// Project QueryCollectionReader onto 'a and provide
/// to next HttpHandler
let mapQuery
    (map : QueryCollectionReader -> 'a)
    (next : 'a -> HttpHandler) : HttpHandler = fun ctx -> 
    next (getQuery ctx |> map) ctx

/// Project CookieCollectionReader onto 'a and provide
/// to next HttpHandler
let mapCookie
    (map : CookieCollectionReader -> 'a)
    (next : 'a -> HttpHandler) : HttpHandler = fun ctx -> 
    next (getCookie ctx |> map) ctx

/// Project FormCollectionReader onto 'a and provide
/// to next HttpHandler
let mapForm
    (map : FormCollectionReader -> 'a)
    (next : 'a -> HttpHandler) : HttpHandler = fun ctx -> 
    let continuation (mapTask : Task<FormCollectionReader>) = next (mapTask.Result |> map) ctx           
    getForm ctx |> onCompleteWithUnitTask continuation
    

/// Project FormCollectionReader a streamed (i.e., multipart/form-data) 
/// onto 'a and provide to next HttpHandler
let mapFormStream
    (map : FormCollectionReader -> 'a)
    (next : 'a -> HttpHandler) : HttpHandler = fun ctx -> 
    let continuation (mapTask : Task<FormCollectionReader>) = next (mapTask.Result |> map) ctx           
    streamForm ctx |> onCompleteWithUnitTask continuation

/// Project FormCollectionReader onto 'a and provide
/// to next HttpHandler
let mapFormSecure
    (map : FormCollectionReader -> 'a)
    (next : 'a -> HttpHandler)
    (handleInvalidToken : HttpHandler) : HttpHandler =
        validateCsrfToken
            (mapForm map next)
            handleInvalidToken

/// Project FormCollectionReader a streamed (i.e., multipart/form-data) 
/// onto 'a and provide to next HttpHandler
let mapFormStreamSecure
    (map : FormCollectionReader -> 'a)
    (next : 'a -> HttpHandler)
    (handleInvalidToken : HttpHandler) : HttpHandler =
        mapFormStream map 
            (fun x -> validateCsrfToken (next x) handleInvalidToken)
         

/// Proceed if the authentication status of current IPrincipal is true
let ifAuthenticated
    (handleOk : HttpHandler)
    (handleError : HttpHandler) : HttpHandler =
    fun ctx ->
        if Auth.isAuthenticated ctx then handleOk ctx
        else handleError ctx

/// Proceed if the authentication status of current IPrincipal is true
/// and they exist in a list of roles
let ifAuthenticatedInRole
    (roles : string list)
    (handleOk : HttpHandler)
    (handleError : HttpHandler) : HttpHandler =
    fun ctx ->
        let isAuthenticated = Auth.isAuthenticated ctx
        let isInRole = Auth.isInRole roles ctx

        match isAuthenticated, isInRole with
        | true, true -> handleOk ctx
        | _          -> handleError ctx
        
/// Proceed if the authentication status of current IPrincipal is true
/// and has a specific scope
let ifAuthenticatedWithScope
    (issuer : string)
    (scope : string)
    (handleOk : HttpHandler)
    (handleError : HttpHandler) : HttpHandler =
    fun ctx ->
        let isAuthenticated = Auth.isAuthenticated ctx
        let hasScope = Auth.hasScope issuer scope ctx
        
        match isAuthenticated, hasScope with
        | true, true -> handleOk ctx
        | _          -> handleError ctx

/// Proceed if the authentication status of current IPrincipal is false
let ifNotAuthenticated
    (handleOk : HttpHandler)
    (handleError : HttpHandler) : HttpHandler =
    fun ctx ->
        if Auth.isAuthenticated ctx then handleError ctx
        else handleOk ctx