[<RequireQualifiedAccess>]
module Falco.Response

open System
open System.IO
open System.Security.Claims
open System.Text
open System.Text.Json
open System.Threading.Tasks
open Falco.Markup
open Falco.Security
#if NETCOREAPP3_1 || NET5_0
    open FSharp.Control.Tasks
#endif
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers

// ------------
// Modifiers
// ------------

let private setHeader (name : string) (content : string) (ctx : HttpContext) =
    if not(ctx.Response.Headers.ContainsKey(name)) then
        ctx.Response.Headers.Add(name, StringValues(content))

/// Set ContentLength for response
let withContentLength (contentLength : int64) : HttpResponseModifier =
    fun ctx ->
        ctx.Response.ContentLength <- Nullable(contentLength)
        ctx

/// Set specific header for response
let withHeader (name : string) (content : string) : HttpResponseModifier =
    fun ctx ->
        setHeader name content ctx
        ctx

/// Set multiple headers for response
let withHeaders (headers : (string * string) list) : HttpResponseModifier =
    fun ctx ->
        headers
        |> List.iter (fun (name, content) -> setHeader name content ctx)
        ctx

/// Set ContentType header for response
let withContentType (contentType : string) : HttpResponseModifier =
    withHeader HeaderNames.ContentType contentType

/// Set StatusCode for response
let withStatusCode (statusCode : int) : HttpResponseModifier =
    fun ctx ->
        ctx.Response.StatusCode <- statusCode
        ctx
    //modify (fun ctx -> ctx.Response.SetStatusCode statusCode)

/// Add cookie to response
let withCookie (key : string) (value : string) : HttpResponseModifier =
    fun ctx ->
        ctx.Response.Cookies.Append(key, value)
        ctx

/// Add a configured cookie to response, via CookieOptions
let withCookieOptions (key : string) (value : string) (options : CookieOptions) : HttpResponseModifier =
    fun ctx ->
        ctx.Response.Cookies.Append(key, value, options)
        ctx

// ------------
// Handlers
// ------------

/// Write bytes to HttpResponse body
let private writeBytes (bytes : byte[]) (ctx : HttpContext) =
    let byteLen = bytes.Length
    ctx.Response.ContentLength <- Nullable<int64>(byteLen |> int64)
    ctx.Response.BodyWriter.WriteAsync(ReadOnlyMemory<byte>(bytes)).AsTask() :> Task

/// Write UTF8 string to HttpResponse body
let private writeString (encoding : Encoding) (httpBodyStr : string) (ctx : HttpContext) =
    if isNull httpBodyStr then
        Task.CompletedTask
    else
        let httpBodyBytes = encoding.GetBytes httpBodyStr
        writeBytes httpBodyBytes ctx

/// Returns a redirect (301 or 302) to client

type private RedirectType =
    | PermanentlyTo of url: string
    | TemporarilyTo of url: string

let private redirect' (redirectType: RedirectType): HttpHandler =
    fun ctx ->
        let (permanent, url) =
            match redirectType with
            | PermanentlyTo url -> (true, url)
            | TemporarilyTo url -> (false, url)
        ctx.Response.Redirect(url, permanent)
        ctx.Response.CompleteAsync()
        
/// Returns a redirect (301 or 302) to client
let redirectPermanentlyTo (url: string) = redirect' (PermanentlyTo url)
/// Returns a redirect (301 or 302) to client
let redirectTemporarilyTo (url: string) = redirect' (TemporarilyTo url)

/// Returns an inline binary (i.e., Byte[]) response with the specified Content-Type
///
/// Note: Automatically sets "content-disposition: inline"
let ofBinary (contentType : string) (headers : (string * string) list) (bytes : Byte[]) : HttpHandler =
    let headers = (HeaderNames.ContentDisposition, "inline") :: headers

    withContentType contentType
    >> withHeaders headers
    >> writeBytes bytes

/// Returns a binary (i.e., Byte[]) attachment response with the specified Content-Type and optional filename
///
/// Note: Automatically sets "content-disposition: attachment" and includes filename if provided
let ofAttachment (filename : string) (contentType : string) (headers : (string * string) list) (bytes : Byte[]) : HttpHandler =
    let contentDisposition =
        if StringUtils.strNotEmpty filename then sprintf "attachment; filename=\"%s\"" filename
        else "attachment"

    let headers = (HeaderNames.ContentDisposition, contentDisposition) :: headers

    withContentType contentType
    >> withHeaders headers
    >> writeBytes bytes

/// Flushes any remaining response headers or data and returns empty response
let ofEmpty : HttpHandler =
    fun ctx -> ctx.Response.CompleteAsync ()

/// Writes string to response body with provided encoding
let ofString (encoding : Encoding) (str : string) : HttpHandler =
    fun ctx -> writeString encoding str ctx

/// Returns a "text/plain; charset=utf-8" response with provided string to client
let ofPlainText (str : string) : HttpHandler =
    withContentType "text/plain; charset=utf-8"
    >> ofString Encoding.UTF8 str

/// Returns a "text/html; charset=utf-8" response with provided HTML string to client
let ofHtmlString (html : string) : HttpHandler =
    withContentType "text/html; charset=utf-8"
    >> ofString Encoding.UTF8 html

/// Returns a "text/html; charset=utf-8" response with provided HTML to client
let ofHtml (html : XmlNode) : HttpHandler =
    renderHtml html
    |> ofHtmlString

/// Returns a CSRF token-dependant "text/html; charset=utf-8" response with provided HTML to client
let ofHtmlCsrf (view : AntiforgeryTokenSet -> XmlNode) : HttpHandler =
    let withCsrfToken handleToken : HttpHandler =
        fun ctx ->
            let csrfToken = Xss.getToken ctx
            handleToken csrfToken ctx

    withCsrfToken (fun token -> token |> view |> ofHtml)

/// Returns an optioned "application/json; charset=utf-8" response with the serialized object provided to the client
let ofJsonOptions (options : JsonSerializerOptions) (obj : 'a) : HttpHandler =
    let jsonHandler : HttpHandler = fun ctx ->
        #if NETCOREAPP3_1 || NET5_0
        unitTask {
        #else
        task {
        #endif
            use str = new MemoryStream()
            do! JsonSerializer.SerializeAsync(str, obj, options = options)            
            let bytes = str.ToArray ()
            let byteLen = bytes.Length
            ctx.Response.ContentLength <- Nullable<int64>(byteLen |> int64)
            let! _ = ctx.Response.BodyWriter.WriteAsync(ReadOnlyMemory<byte>(bytes))
            return ()
        }

    withContentType "application/json; charset=utf-8"
    >> jsonHandler

/// Returns a "application/json; charset=utf-8" response with the serialized object provided to the client
let ofJson (obj : 'a) : HttpHandler =
    withContentType "application/json; charset=utf-8"
    >> ofJsonOptions Constants.defaultJsonOptions obj

/// Sign in claim principal for provided scheme then respond with a 301 redirect to provided URL
let signInAndRedirect
    (authScheme : string)
    (claimsPrincipal : ClaimsPrincipal)
    (url : string) : HttpHandler = fun ctx ->
    #if NETCOREAPP3_1 || NET5_0
    unitTask {
    #else
    task {
    #endif
        do! Auth.signIn authScheme claimsPrincipal ctx
        do! redirectTemporarilyTo url ctx
    }

/// Sign in claim principal for provided scheme and options then respond with a 301 redirect to provided URL
let signInOptionsAndRedirect
    (authScheme : string)
    (claimsPrincipal : ClaimsPrincipal)
    (options : AuthenticationProperties)
    (url : string) : HttpHandler = fun ctx ->
    #if NETCOREAPP3_1 || NET5_0
    unitTask {
    #else
    task {
    #endif
        do! Auth.signInOptions authScheme claimsPrincipal options ctx
        do! redirectTemporarilyTo url ctx
    }

/// Terminates authenticated context for provided scheme then respond with a 301 redirect to provided URL
let signOutAndRedirect
    (authScheme : string)
    (url : string) : HttpHandler = fun ctx ->
    #if NETCOREAPP3_1 || NET5_0
    unitTask {
    #else
    task {
    #endif
        do! Auth.signOut authScheme ctx
        do! redirectTemporarilyTo url ctx
    }

/// Challenge the specified authentication scheme.
/// An authentication challenge can be issued when an unauthenticated user requests an endpoint that requires authentication.
/// Then given redirectUri is forwarded to the authentication handler for use after authentication succeeds.
let challengeWithRedirect
    (authScheme : string)
    (redirectUri : string) : HttpHandler = fun ctx ->
    #if NETCOREAPP3_1 || NET5_0
    unitTask {
    #else
    task {
    #endif
        let properties = AuthenticationProperties(RedirectUri = redirectUri)
        do! Auth.challenge authScheme properties ctx
    }
