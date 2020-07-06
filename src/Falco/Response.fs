module Falco.Response

open System.Text
open System.Text.Json
open System.Threading.Tasks
open Falco.ViewEngine
open Microsoft.AspNetCore.Http

let ofPlainText
    (str : string) 
    (ctx : HttpContext) : Task =    
    ctx.Response.SetContentType "text/plain; charset=utf-8"
    ctx.Response.WriteString Encoding.UTF8 str

let ofHtml 
    (html : XmlNode) 
    (ctx : HttpContext) : Task = 
    ctx.Response.SetContentType "text/html; charset=utf-8"
    let html = renderHtml html 
    ctx.Response.WriteString Encoding.UTF8 html

let ofJson
    (obj : 'a)
    (ctx : HttpContext) : Task =
    ctx.Response.SetContentType "application/json; charset=utf-8"            
    JsonSerializer.SerializeAsync(ctx.Response.Body, obj)
      
let redirect 
    (url : string) 
    (permanent : bool)
    (ctx : HttpContext) =
    ctx.Response.Redirect(url, permanent)
