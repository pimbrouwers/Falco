[<AutoOpen>]
module Falco.Handlers

open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Falco.ViewEngine

let shortCircuit : HttpFunc = Some >> Task.FromResult

let setStatusCode (statusCode : int) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        ctx.SetStatusCode statusCode
        next ctx

let redirect url perm : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        ctx.Response.Redirect(url, perm)
        shortCircuit ctx
            
let textOut (str : string) : HttpHandler =    
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        ctx.SetContentType "text/plain; charset=utf-8"
        ctx.WriteString str

let jsonOut (obj : 'a) : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->   
        ctx.SetContentType "application/json; charset=utf-8"
        ctx.WriteString (JsonSerializer.Serialize(obj))

let htmlOut (html : XmlNode) : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        ctx.SetContentType "text/html; charset=utf-8"            
        renderHtml html
        |> ctx.WriteString 