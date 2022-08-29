[<RequireQualifiedAccess>]
module Falco.Request

open System
open System.Text.Json
open System.Threading.Tasks
#if NETCOREAPP3_1 || NET5_0
    open FSharp.Control.Tasks
#endif
open Microsoft.AspNetCore.Authentication
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

[<Obsolete("Use Request.getRoute")>]
let tryBindRoute
    (binder : RouteCollectionReader -> Result<'a, 'b>)
    (ctx : HttpContext) : Result<'a, 'b> =
    getRoute ctx
    |> binder

/// Retrieve the query string from the request as an instance of QueryCollectionReader
let getQuery (ctx : HttpContext) : QueryCollectionReader =
    QueryCollectionReader(ctx.Request.Query)

/// Attempt to bind query collection
[<Obsolete("Use Request.getQuery")>]
let tryBindQuery
    (binder : QueryCollectionReader -> Result<'a, 'b>)
    (ctx : HttpContext) : Result<'a, 'b> =
    getQuery ctx
    |> binder

/// Retrieve the form collection from the request as an instance of FormCollectionReader
let getForm (ctx : HttpContext) : Task<FormCollectionReader> =
    task {
        let! form = ctx.Request.ReadFormAsync ()
        let files = if isNull(form.Files) then None else Some form.Files
        return FormCollectionReader(form, files)
    }

/// Attempt to bind the form collection
[<Obsolete("Use Request.getForm")>]
let tryBindForm
    (binder : FormCollectionReader -> Result<'a, 'b>)
    (ctx : HttpContext) : Task<Result<'a, 'b>> =
    task {
        let! form = getForm ctx
        return binder form
    }

/// Attempt to stream the form collection for multipart form submissions
let tryStreamForm
    (ctx : HttpContext) : Task<Result<FormCollectionReader, string>> =
    ctx.Request.TryStreamFormAsync()

/// Stream the form collection for multipart form submissions
let streamForm
    (ctx : HttpContext) : Task<FormCollectionReader> =
    ctx.Request.StreamFormAsync()

/// Attempt to bind the form collection
[<Obsolete("Use Request.tryStreamForm")>]
let tryBindFormStream
    (binder : FormCollectionReader -> Result<'a, 'b>)
    (ctx : HttpContext) : Task<Result<'a, 'b>> =
    task {
        let! form = streamForm ctx
        return binder form
    }

/// Retrieve the cookie from the request as an instance of CookieCollectionReader
let getCookie (ctx : HttpContext) : CookieCollectionReader =
    CookieCollectionReader(ctx.Request.Cookies)

/// Attempt to bind cookie collection
[<Obsolete("Use Request.getCookie")>]
let tryBindCookie
    (binder : CookieCollectionReader -> Result<'a, 'b>)
    (ctx : HttpContext) : Result<'a, 'b> =
    getCookie ctx
    |> binder

/// Attempt to bind request body using System.Text.Json and provided JsonSerializerOptions
let getJsonOptions<'a>
    (options : JsonSerializerOptions)
    (ctx : HttpContext) : Task<'a> =
    JsonSerializer.DeserializeAsync<'a>(ctx.Request.Body, options).AsTask()

/// Attempt to bind request body using System.Text.Json
[<Obsolete("Use Request.getJsonOptions Constants.defaultJsonOptions")>]
let getJson<'a>
    (ctx : HttpContext) : Task<'a> =
    getJsonOptions Constants.defaultJsonOptions ctx

/// Attempt to bind request body using System.Text.Json and provided JsonSerializerOptions
[<Obsolete("Use Request.getJsonOptions")>]
let tryBindJsonOptions<'a>
    (options : JsonSerializerOptions)
    (ctx : HttpContext) : Task<Result<'a, string>> =
    task {
        try
            let! result = JsonSerializer.DeserializeAsync<'a>(ctx.Request.Body, options).AsTask()
            return Ok result
        with
        :? JsonException as ex -> return Error ex.Message
    }

/// Attempt to bind request body using System.Text.Json
[<Obsolete("Use Request.getJsonOptions Constants.defaultJsonOptions")>]
let tryBindJson<'a>
    (ctx : HttpContext) : Task<Result<'a, string>> =
    tryBindJsonOptions Constants.defaultJsonOptions ctx

// ------------
// Handlers
// ------------

/// Attempt to bind JSON request body onto 'a and provide
/// to handleOk, otherwise provide handleError with error string
[<Obsolete("Request.bindJson is obsolete, please use Request.mapJson.")>]
let bindJson
    (handleOk : 'a -> HttpHandler)
    (handleError : string -> HttpHandler) : HttpHandler = fun ctx ->
    #if NETCOREAPP3_1 || NET5_0
    unitTask {
    #else
    task {
    #endif
        let! json = tryBindJson ctx
        let respondWith =
            match json with
            | Error error -> handleError error
            | Ok form -> handleOk form

        return! respondWith ctx
    }


/// Attempt to bind the route values map onto 'a and provide
/// to handleOk, otherwise provide handleError with 'b
[<Obsolete("Request.bindRoute is obsolete, please use Request.mapRoute.")>]
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
[<Obsolete("Request.bindQuery is obsolete, please use Request.mapQuery.")>]
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
[<Obsolete("Request.bindCookie is obsolete, please use Request.mapCookie.")>]
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
    #if NETCOREAPP3_1 || NET5_0
    unitTask {
    #else
    task {
    #endif
        let! isValid = Xss.validateToken ctx

        let respondWith =
            match isValid with
            | true  -> handleOk
            | false -> handleInvalidToken

        return! respondWith ctx
    }

/// Attempt to bind the FormCollectionReader onto 'a and provide
/// to handleOk, otherwise provide handleError with 'b
[<Obsolete("Request.bindForm is obsolete, please use Request.mapForm.")>]
let bindForm
    (binder : FormCollectionReader -> Result<'a, 'b>)
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler) : HttpHandler = fun ctx ->
    #if NETCOREAPP3_1 || NET5_0
    unitTask {
    #else
    task {
    #endif
        let! form = tryBindForm binder ctx
        let respondWith =
            match form with
            | Error error -> handleError error
            | Ok form -> handleOk form

        return! respondWith ctx
    }

/// Attempt to bind a streamed (i.e., multipart/form-data)
/// FormCollectionReader  onto 'a and provide to handleOk,
/// otherwise provide handleError with 'b
[<Obsolete("Request.bindFormStream is obsolete, please use Request.mapFormStream.")>]
let bindFormStream
    (binder : FormCollectionReader -> Result<'a, 'b>)
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler) : HttpHandler = fun ctx ->
    #if NETCOREAPP3_1 || NET5_0
    unitTask {
    #else
    task {
    #endif
        let! form = tryBindFormStream binder ctx
        let respondWith =
            match form with
            | Error error -> handleError error
            | Ok form -> handleOk form

        return! respondWith ctx
    }

/// Validate the CSRF of the current request attempt to bind the
/// FormCollectionReader onto 'a and provide to handleOk,
/// otherwise provide handleError with 'b
[<Obsolete("Request.bindFormSecure is obsolete, please use Request.mapFormSecure.")>]
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
[<Obsolete("Request.bindFormStreamSecure is obsolete, please use Request.mapFormStreamSecure.")>]
let bindFormStreamSecure
    (binder : FormCollectionReader -> Result<'a, 'b>)
    (handleOk : 'a -> HttpHandler)
    (handleError : 'b -> HttpHandler)
    (handleInvalidToken : HttpHandler) : HttpHandler = fun ctx ->
    ctx.Request.EnableBuffering() // allow the request body to be read again

    validateCsrfToken
        (bindFormStream binder handleOk handleError)
        handleInvalidToken
        ctx

/// Bind JSON request body onto 'a and provide to next
/// Httphandler, throws exception if JsonException
/// occurs during deserialization.
let mapJson (next : 'a -> HttpHandler) : HttpHandler = fun ctx ->
    #if NETCOREAPP3_1 || NET5_0
    unitTask {
    #else
    task {
    #endif
        let! json = tryBindJson ctx
        let respondWith =
            match json with
            | Error error -> failwith error
            | Ok json -> next json
        return! respondWith ctx
    }

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
    #if NETCOREAPP3_1 || NET5_0
    unitTask {
    #else
    task {
    #endif
        let! form = getForm ctx
        return! next (form |> map) ctx
    }


/// Project FormCollectionReader a streamed (i.e., multipart/form-data)
/// onto 'a and provide to next HttpHandler
let mapFormStream
    (map : FormCollectionReader -> 'a)
    (next : 'a -> HttpHandler) : HttpHandler = fun ctx ->
    #if NETCOREAPP3_1 || NET5_0
    unitTask {
    #else
    task {
    #endif
        let! form = streamForm ctx
        return! next (form |> map) ctx
    }

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
    (handleInvalidToken : HttpHandler) : HttpHandler = fun ctx ->
        ctx.Request.EnableBuffering()
        validateCsrfToken
            (mapFormStream map next)
            handleInvalidToken
            ctx

/// Attempt to authenticate the current request using the provided
/// scheme and pass AuthenticateResult into next HttpHandler
let authenticate (scheme : string) (next : AuthenticateResult -> HttpHandler) : HttpHandler = fun ctx ->
    #if NETCOREAPP3_1 || NET5_0
    unitTask {
    #else
    task {
    #endif
        let! auth = ctx.AuthenticateAsync(scheme)
        return! next auth ctx
    }

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
