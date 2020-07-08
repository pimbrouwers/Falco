[<RequireQualifiedAccess>]
module Falco.Response

open System.Text
open System.Text.Json
open System.Threading.Tasks
open Falco.Markup
open Microsoft.AspNetCore.Http
open Microsoft.Net.Http.Headers

let setHeader 
    (header : string)
    (content : string) : HttpResponseModifier =
    fun ctx ->
        ctx.Response.SetHeader header content
        ctx

let setContentType
    (contentType : string) : HttpResponseModifier =
    setHeader HeaderNames.ContentType contentType         

let withStatusCode
    (statusCode : int) : HttpResponseModifier =
    fun ctx ->
        ctx.Response.SetStatusCode statusCode
        ctx

let redirect     
    (url : string) 
    (permanent : bool) : HttpHandler =
    fun ctx ->
        ctx.Response.Redirect(url, permanent)        
        Task.CompletedTask

let ofString
    (encoding : Encoding)
    (str : string) : HttpHandler =
    fun ctx ->
        ctx.Response.WriteString encoding str

let ofPlainText    
    (str : string) : HttpHandler =
    setContentType "text/plain; charset=utf-8" 
    >> ofString Encoding.UTF8 str
                
let ofHtml     
    (html : XmlNode) : HttpHandler =    
    let html = renderHtml html
    setContentType "text/html; charset=utf-8"
    >> ofString Encoding.UTF8 html

let ofJson    
    (obj : 'a) : HttpHandler =    
    setContentType "application/json; charset=utf-8"
    >> fun ctx -> JsonSerializer.SerializeAsync(ctx.Response.Body, obj)

let ofJsonWithOptions
    (obj : 'a) 
    (options : JsonSerializerOptions) : HttpHandler =
    setContentType "application/json; charset=utf-8"
    >> fun ctx -> JsonSerializer.SerializeAsync(ctx.Response.Body, obj, options = options)