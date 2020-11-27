[<RequireQualifiedAccess>]
module Falco.Response

open System
open System.IO
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

/// Flushes any remaining response headers or data and returns empty response
let ofEmpty : HttpHandler =
    fun ctx -> ctx.Response.CompleteAsync ()

/// Writes string to response body with provided encoding
let ofString (encoding : Encoding) (str : string) : HttpHandler =        
    fun ctx -> 
        ctx.Response.WriteString encoding str
        
/// Returns a "text/plain; charset=utf-8" response with provided string to client
let ofPlainText (str : string) : HttpHandler =
    withContentType "text/plain; charset=utf-8" 
    >> ofString Encoding.UTF8 str
                
/// Returns a "text/html; charset=utf-8" response with provided HTML to client
let ofHtml (html : XmlNode) : HttpHandler =    
    let html = renderHtml html
    withContentType "text/html; charset=utf-8"
    >> ofString Encoding.UTF8 html

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

/// Responsds with a 301, and terminates authenticated context for provided scheme
let signOutAndRedirect (authScheme : string) (url : string) : HttpHandler =
    fun ctx -> task {
        do! Auth.signOut authScheme ctx
        do! redirect url false ctx
    }