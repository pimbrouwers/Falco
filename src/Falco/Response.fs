module Falco.Response

open System.Text
open System.Text.Json
open System.Threading.Tasks
open Falco.ViewEngine
open Microsoft.AspNetCore.Http

let ofPlainText
    (ctx : HttpContext)
    (str : string) : Task =    
    ctx.Response.SetContentType "text/plain; charset=utf-8"
    ctx.Response.WriteString Encoding.UTF8 str

let ofHtml 
    (ctx : HttpContext)
    (html : XmlNode) : Task = 
    ctx.Response.SetContentType "text/html; charset=utf-8"
    let html = renderHtml html 
    ctx.Response.WriteString Encoding.UTF8 html

let ofJson
    (ctx : HttpContext)
    (obj : 'a) : Task =
    ctx.Response.SetContentType "application/json; charset=utf-8"            
    JsonSerializer.SerializeAsync(ctx.Response.Body, obj)
      
let redirect 
    (ctx : HttpContext)
    (url : string) 
    (permanent : bool) : unit =
    ctx.Response.Redirect(url, permanent)
