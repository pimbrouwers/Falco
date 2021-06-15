[<RequireQualifiedAccess>]
module Falco.Response

open System
open System.IO
open System.Security.Claims
open System.Text
open System.Text.Json
open Falco.Markup
open Falco.Security
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.Net.Http.Headers

// ------------
// Modifiers
// ------------

/// A helper function which threads the HttpContext through the provided modifier and returns
let modify (modifier : HttpContext -> unit) =
    fun ctx ->
        modifier ctx
        ctx

/// Set ContentLength for response
let withContentLength (contentLength : int64) : HttpResponseModifier =
    modify (fun ctx -> ctx.Response.ContentLength <- Nullable(contentLength))

/// Set specific header for response
let withHeader (header : string) (content : string) : HttpResponseModifier =
    modify (fun ctx -> ctx.Response.SetHeader header content)

/// Set multiple headers for response
let withHeaders (headers : (string * string) list) : HttpResponseModifier =
    modify (fun ctx -> headers |> List.iter (fun (header, content) -> ctx.Response.SetHeader header content))

/// Set ContentType header for response
let withContentType (contentType : string) : HttpResponseModifier =
    withHeader HeaderNames.ContentType contentType

/// Set StatusCode for response
let withStatusCode (statusCode : int) : HttpResponseModifier =
    modify (fun ctx -> ctx.Response.SetStatusCode statusCode)

/// Add cookie to response
let withCookie (key : string) (value : string) : HttpResponseModifier =
    modify (fun ctx -> ctx.Response.AddCookie key value)

// ------------
// Handlers
// ------------

/// Returns a redirect (301 or 302) to client
let redirect (url : string) (permanent : bool) : HttpHandler =
    fun ctx ->
        ctx.Response.Redirect(url, permanent)
        ctx.Response.CompleteAsync ()

/// Returns an inline binary (i.e., Byte[]) response with the specified Content-Type
///
/// Note: Automatically sets "content-disposition: inline"
let ofBinary (contentType : string) (headers : (string * string) list) (bytes : Byte[]) : HttpHandler =     
    let headers = (HeaderNames.ContentDisposition, "inline") :: headers
    
    withContentType contentType
    >> withHeaders headers
    >> fun ctx -> ctx.Response.WriteBytes bytes

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
    >> fun ctx -> ctx.Response.WriteBytes bytes

/// Flushes any remaining response headers or data and returns empty response
let ofEmpty : HttpHandler =
    fun ctx -> ctx.Response.CompleteAsync ()

/// Writes string to response body with provided encoding
let ofString (encoding : Encoding) (str : string) : HttpHandler =
    fun ctx -> ctx.Response.WriteString encoding str

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
    withContentType "application/json; charset=utf-8"
    >> fun ctx -> task {
        use str = new MemoryStream()
        do! JsonSerializer.SerializeAsync(str, obj, options = options)
        str.Flush ()
        do! ctx.Response.WriteBytes (str.ToArray ())
        //return ()
    }

/// Returns a "application/json; charset=utf-8" response with the serialized object provided to the client
let ofJson (obj : 'a) : HttpHandler =
    withContentType "application/json; charset=utf-8"
    >> ofJsonOptions Constants.defaultJsonOptions obj


/// Sign in claim principal for provided scheme, and responsd with a 301 redirect to provided URL
let signInAndRedirect (authScheme : string) (claimsPrincipal : ClaimsPrincipal) (url : string) : HttpHandler =
    fun ctx -> task {
        do! Auth.signIn authScheme claimsPrincipal ctx
        do! redirect url false ctx
    }

/// Terminates authenticated context for provided scheme, and respond with a 301 redirect to provided URL
let signOutAndRedirect (authScheme : string) (url : string) : HttpHandler =
    fun ctx -> task {
        do! Auth.signOut authScheme ctx
        do! redirect url false ctx
    }