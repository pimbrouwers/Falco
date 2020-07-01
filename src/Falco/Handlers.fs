[<AutoOpen>]
module Falco.Handlers

open System.IO
open System.Text
open System.Text.Json
open Falco.ViewEngine
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http

/// An HttpHandler to set status code
let setStatusCode (statusCode : int) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        ctx.SetStatusCode statusCode
        next ctx

/// An HttpHandler to redirect (301, 302). 
/// This is terminal middleware.
let redirect url perm : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        ctx.Response.Redirect(url, perm)
        earlyReturn ctx
  
 /// An HttpHandler to output plain-text.
 /// This is terminal middleware.
let textOut (str : string) : HttpHandler =    
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        task {  
            ctx.SetContentType "text/plain; charset=utf-8"
            do! ctx.WriteString Encoding.UTF8 str
            return! earlyReturn ctx
        }
    
/// An HttpHandler to output JSON.
/// This is terminal middleware.
let jsonOut (obj : 'a) : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->   
        task {
            ctx.SetContentType "application/json; charset=utf-8"
            use s = new MemoryStream()
            do! JsonSerializer.SerializeAsync(s, obj) |> Async.AwaitTask
            let json = Encoding.UTF8.GetString(s.ToArray())
            do! ctx.WriteString Encoding.UTF8 json
            return! earlyReturn ctx
        }

/// An HttpHandler to output HTML
/// This is terminal middleware.
let htmlOut (html : XmlNode) : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        task {
            ctx.SetContentType "text/html; charset=utf-8"            
            do! renderHtml html |> ctx.WriteString Encoding.UTF8
            return! earlyReturn ctx
        }
