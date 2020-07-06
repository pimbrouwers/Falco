module Falco.Response

open System.Text
open System.Text.Json
open System.Threading.Tasks
open Falco.Markup
open Microsoft.AspNetCore.Http
open Microsoft.Net.Http.Headers

type HttpResponseModifier = HttpContext -> HttpContext

let setHeader 
    (header : string)
    (content : string) : HttpResponseModifier =
    fun ctx ->
        ctx.Response.SetHeader header content
        ctx

let setContentType
    (contentType : string) : HttpResponseModifier =
    setHeader HeaderNames.ContentType contentType         

let redirect     
    (url : string) 
    (permanent : bool) : HttpResponseModifier =
    fun ctx ->
        ctx.Response.Redirect(url, permanent)
        ctx

let withStatusCode
    (statusCode : int) : HttpResponseModifier =
    fun ctx ->
        ctx.Response.SetStatusCode statusCode
        ctx

type HttpResponder = HttpContext -> Task

let ofString
    (encoding : Encoding)
    (str : string) : HttpResponder =
    fun ctx ->
        ctx.Response.WriteString encoding str

let ofPlainText    
    (str : string) : HttpResponder =
    setContentType "text/plain; charset=utf-8" 
    >> ofString Encoding.UTF8 str
                
let ofHtml     
    (html : XmlNode) : HttpResponder =    
    let html = renderHtml html
    setContentType "text/html; charset=utf-8"
    >> ofString Encoding.UTF8 html

let ofJson    
    (obj : 'a) : HttpResponder =    
    setContentType "application/json; charset=utf-8"
    >> fun ctx -> JsonSerializer.SerializeAsync(ctx.Response.Body, obj)     