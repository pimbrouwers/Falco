[<RequireQualifiedAccess>]
module Falco.Request

open System.IO
open System.Text
open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http
open Falco.Multipart
open Falco.Security
open Falco.StringUtils

let private httpPipe
    (prepare :  HttpContext -> 'T)
    (next : 'T -> HttpHandler) : HttpHandler = fun ctx ->
    next (prepare ctx) ctx

let internal httpPipeTask
    (prepare :  HttpContext -> Task<'T>)
    (next : 'T -> HttpHandler) : HttpHandler = fun ctx ->
    task {
        let! x = prepare ctx
        return! next x ctx
    }

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

/// Stream the request body into a string.
let getBodyString (ctx : HttpContext) : Task<string> =
    task {
        use reader = new StreamReader(ctx.Request.Body, Encoding.UTF8)
        return! reader.ReadToEndAsync()
    }

/// Retrieve the cookie from the request as an instance of
/// CookieCollectionReader.
let getCookie (ctx : HttpContext) : CookieCollectionReader =
    CookieCollectionReader(ctx.Request.Cookies)

/// Retrieve a specific header from the request.
let getHeaders (ctx : HttpContext) : HeaderCollectionReader  =
    HeaderCollectionReader(ctx.Request.Headers)

/// Retrieve all route values from the request as RouteCollectionReader.
let getRoute (ctx : HttpContext) : RouteCollectionReader =
    RouteCollectionReader(ctx.Request.RouteValues, ctx.Request.Query)

/// Retrieve the query string from the request as an instance of
/// QueryCollectionReader.
let getQuery (ctx : HttpContext) : QueryCollectionReader =
    QueryCollectionReader(ctx.Request.Query)

/// Retrieve the form collection from the request as an instance of
/// FormCollectionReader.
let getForm (ctx : HttpContext) : Task<FormCollectionReader> =
    task {
        let! form = ctx.Request.ReadFormAsync()
        let files = if isNull(form.Files) then None else Some form.Files
        return FormCollectionReader(form, files)
    }

/// Attempt to bind request body using System.Text.Json and provided
/// JsonSerializerOptions.
let getJsonOptions<'T>
    (options : JsonSerializerOptions)
    (ctx : HttpContext) : Task<'T> =
    JsonSerializer.DeserializeAsync<'T>(ctx.Request.Body, options).AsTask()

/// Stream the form collection for multipart form submissions.
let streamForm
    (ctx : HttpContext) : Task<FormCollectionReader> =
    task {
        let! form = ctx.Request.StreamFormAsync()
        let files = if isNull(form.Files) then None else Some form.Files
        return FormCollectionReader(form, files)
    }

// ------------
// Handlers
// ------------

/// Buffer the current HttpRequest body into a
/// string and provide to next HttpHandler.
let bodyString
    (next : string -> HttpHandler) : HttpHandler =
    httpPipeTask getBodyString next

/// Project CookieCollectionReader onto 'T and provide
/// to next HttpHandler.
let mapCookie
    (map : CookieCollectionReader -> 'T)
    (next : 'T -> HttpHandler) : HttpHandler =
    httpPipe getCookie (map >> next)

/// Project HeaderCollectionReader onto 'T and provide
/// to next HttpHandler.
let mapHeader
    (map : HeaderCollectionReader -> 'T)
    (next : 'T -> HttpHandler) : HttpHandler =
    httpPipe getHeaders (map >> next)

/// Project RouteCollectionReader onto 'T and provide
/// to next HttpHandler.
let mapRoute
    (map : RouteCollectionReader -> 'T)
    (next : 'T -> HttpHandler) : HttpHandler =
    httpPipe getRoute (map >> next)

/// Project QueryCollectionReader onto 'T and provide
/// to next HttpHandler.
let mapQuery
    (map : QueryCollectionReader -> 'T)
    (next : 'T -> HttpHandler) : HttpHandler =
    httpPipe getQuery (map >> next)


/// Project FormCollectionReader onto 'T and provide
/// to next HttpHandler.
let mapForm
    (map : FormCollectionReader -> 'T)
    (next : 'T -> HttpHandler) : HttpHandler =
    httpPipeTask getForm (map >> next)

/// Stream multipart/form-data into FormCollectionReader and project onto 'T
/// provide to next HttpHandler.
///
/// Important: This is intended to be used with multipart/form-data submissions
/// and will not work if this content-type is not present.
let mapFormStream
    (map : FormCollectionReader -> 'T)
    (next : 'T -> HttpHandler) : HttpHandler =
    httpPipeTask streamForm (map >> next)

/// Validate the CSRF of the current request.
let validateCsrfToken
    (handleOk : HttpHandler)
    (handleInvalidToken : HttpHandler) : HttpHandler = fun ctx ->
    task {
        let! isValid = Xss.validateToken ctx

        let respondWith =
            match isValid with
            | true  -> handleOk
            | false -> handleInvalidToken

        return! respondWith ctx
    }

/// Project FormCollectionReader onto 'T and provide
/// to next HttpHandler.
let mapFormSecure
    (map : FormCollectionReader -> 'T)
    (next : 'T -> HttpHandler)
    (handleInvalidToken : HttpHandler) : HttpHandler =
    validateCsrfToken
        (mapForm map next)
        handleInvalidToken

/// Stream multipart/form-data into FormCollectionReader and project onto 'T
/// provide to next HttpHandler.
///
/// Important: This is intended to be used with multipart/form-data submissions
/// and will not work if this content-type is not present.
let mapFormStreamSecure
    (map : FormCollectionReader -> 'T)
    (next : 'T -> HttpHandler)
    (handleInvalidToken : HttpHandler) : HttpHandler = fun ctx ->
    validateCsrfToken
        (mapFormStream map next)
        handleInvalidToken
        ctx

/// Project JSON using custom JsonSerializerOptions
/// onto 'T and provide to next Httphandler, throws
/// JsonException if errors occurs during deserialization.
let mapJsonOption
    (options : JsonSerializerOptions)
    (next : 'T -> HttpHandler) : HttpHandler =
    httpPipeTask (getJsonOptions options) next

let internal defaultJsonOptions =
    let options = JsonSerializerOptions()
    options.AllowTrailingCommas <- true
    options.PropertyNameCaseInsensitive <- true
    options

/// Project JSON onto 'T and provide to next
/// Httphandler, throws JsonException if errors
/// occurs during deserialization.
let mapJson
    (next : 'T -> HttpHandler) : HttpHandler =
    mapJsonOption defaultJsonOptions next

// ------------
// Authentication
// ------------

/// Attempt to authenticate the current request using the provided
/// scheme and pass AuthenticateResult into next HttpHandler.
let authenticate
    (scheme : string)
    (next : AuthenticateResult -> HttpHandler) : HttpHandler =
    httpPipeTask (Auth.authenticate scheme) next

/// Proceed if the authentication status of current IPrincipal is true.
let ifAuthenticated
    (handleOk : HttpHandler)
    (handleError : HttpHandler) : HttpHandler =
    httpPipe
        Auth.isAuthenticated
        (function true -> handleOk | false -> handleError)

/// Proceed if the authentication status of current IPrincipal is true
/// and they exist in a list of roles.
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
/// and has a specific scope.
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

/// Proceed if the authentication status of current IPrincipal is false.
let ifNotAuthenticated
    (handleOk : HttpHandler)
    (handleError : HttpHandler) : HttpHandler =
    httpPipe
        Auth.isAuthenticated
        (function true -> handleError | false -> handleOk)