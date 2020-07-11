[<RequireQualifiedAccess>]
module Falco.Response

open System
open System.IO
open System.Text
open System.Text.Json
open System.Threading.Tasks
open Falco.Markup
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Net.Http.Headers

let redirect     
    (url : string) 
    (permanent : bool) : HttpHandler =
    fun ctx -> 
        ctx.Response.Redirect(url, permanent)        
        ctx.Response.CompleteAsync ()

let withHeader 
    (header : string)
    (content : string) : HttpResponseModifier =
    fun ctx ->
        ctx.Response.SetHeader header content
        ctx

let withContentLength 
    (contentLength : int64) : HttpResponseModifier =
    fun ctx ->
        ctx.Response.ContentLength <- Nullable(contentLength)
        ctx

let withContentType
    (contentType : string) : HttpResponseModifier =    
    withHeader HeaderNames.ContentType contentType         

let withStatusCode
    (statusCode : int) : HttpResponseModifier =
    fun ctx ->
        ctx.Response.SetStatusCode statusCode
        ctx

let ofString
    (encoding : Encoding)
    (str : string) : HttpHandler =        
    fun ctx -> 
        ctx.Response.WriteString encoding str
        
let ofPlainText    
    (str : string) : HttpHandler =
    withContentType "text/plain; charset=utf-8" 
    >> ofString Encoding.UTF8 str
                
let ofHtml     
    (html : XmlNode) : HttpHandler =    
    let html = renderHtml html
    withContentType "text/html; charset=utf-8"
    >> ofString Encoding.UTF8 html

let ofJson    
    (obj : 'a) : HttpHandler =    
    withContentType "application/json; charset=utf-8"
    >> fun ctx -> task {
        use str = new MemoryStream()
        do! JsonSerializer.SerializeAsync(str, obj)
        str.Flush ()
        do! ctx.Response.WriteBytes (str.ToArray())
        return ()
    }
        
let ofJsonWithOptions
    (obj : 'a) 
    (options : JsonSerializerOptions) : HttpHandler =
    withContentType "application/json; charset=utf-8"
    >> fun ctx -> task {
        use str = new MemoryStream()
        do! JsonSerializer.SerializeAsync(str, obj, options = options)   
        str.Flush ()
        do! ctx.Response.WriteBytes (str.ToArray())
        return ()
    }