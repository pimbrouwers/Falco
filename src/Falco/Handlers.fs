[<AutoOpen>]
module Falco.Handlers

open System.Text.Json
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Falco.ViewEngine

// An HttpHandler to stop processing
let shortCircuit : HttpFunc = Some >> Task.FromResult

// Clear current reponse content
let purgeResponse : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        ctx.Response.Clear() 
        next ctx

// An HttpHandler to set status code
let setStatusCode (statusCode : int) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        ctx.SetStatusCode statusCode
        next ctx

// An HttpHandler to redirect (301, 302)
let redirect url perm : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        ctx.Response.Redirect(url, perm)
        shortCircuit ctx
  
 // An HttpHandler to output plain-text
let textOut (str : string) : HttpHandler =    
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        ctx.SetContentType "text/plain; charset=utf-8"
        ctx.WriteString str
    
// An HttpHandler to output JSON
let jsonOut (obj : 'a) : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->   
        ctx.SetContentType "application/json; charset=utf-8"
        ctx.WriteString (JsonSerializer.Serialize(obj))

// An HttpHandler to output HTML
let htmlOut (html : XmlNode) : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        ctx.SetContentType "text/html; charset=utf-8"            
        renderHtml html
        |> ctx.WriteString 