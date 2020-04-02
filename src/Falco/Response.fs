[<AutoOpen>]
module Falco.Response

open System
open System.Text
open System.Text.Json
open System.Threading.Tasks
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers
open Falco.ViewEngine

type HttpContext with    
    member this.SetStatusCode (statusCode : int) =            
        this.Response.StatusCode <- statusCode

    member this.SetHeader name (content : string) =            
        if not(this.Response.Headers.ContainsKey(name)) then
            this.Response.Headers.Add(name, StringValues(content))

    member this.SetContentType contentType =
        this.SetHeader HeaderNames.ContentType contentType

    member this.WriteBytes (bytes : byte[]) =        
        task {            
            let len = bytes.Length
            bytes.CopyTo(this.Response.BodyWriter.GetMemory(len).Span)
            this.Response.BodyWriter.Advance(len)
            this.Response.BodyWriter.FlushAsync(this.RequestAborted) |> ignore
            this.Response.ContentLength <- Nullable<int64>(len |> int64)
            return Some this
        }

    member this.WriteString (str : string) =
        this.WriteBytes (Encoding.UTF8.GetBytes str)
    
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