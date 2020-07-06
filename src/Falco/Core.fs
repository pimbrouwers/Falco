[<AutoOpen>]
module Falco.Core

open System
open System.IO
open System.Text
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers
open Falco.StringUtils
open Falco.StringParser

/// Represents a missing dependency, thrown on request
exception InvalidDependencyException of string

/// Specifies an association of an HttpHandler to an HttpVerb and route pattern
type HttpVerb = 
    | GET 
    | HEAD
    | POST 
    | PUT 
    | PATCH
    | DELETE 
    | OPTIONS
    | TRACE
    | ANY

type HttpHandler = 
    HttpContext -> Task

module HttpHandler =
    let toRequestDelegate (handler : HttpHandler) =        
        new RequestDelegate(handler)

type HttpContext with         
    /// Attempt to obtain depedency from IServiceCollection
    /// Throws InvalidDependencyException on null
    member this.GetService<'a> () =
        let t = typeof<'a>
        match this.RequestServices.GetService t with
        | null    -> raise (InvalidDependencyException t.Name)
        | service -> service :?> 'a

    /// Obtain a named instance of ILogger
    member this.GetLogger (name : string) =
        let loggerFactory = this.GetService<ILoggerFactory>()
        loggerFactory.CreateLogger name

type HttpRequest with   
    /// The HttpVerb of the current request
    member this.HttpVerb = 
        match this.Method with 
        | m when strEquals m HttpMethods.Get     -> GET
        | m when strEquals m HttpMethods.Head    -> HEAD
        | m when strEquals m HttpMethods.Post    -> POST
        | m when strEquals m HttpMethods.Put     -> PUT
        | m when strEquals m HttpMethods.Patch   -> PATCH
        | m when strEquals m HttpMethods.Delete  -> DELETE
        | m when strEquals m HttpMethods.Options -> OPTIONS
        | m when strEquals m HttpMethods.Trace   -> TRACE
        | _ -> ANY

    /// Obtain Map<string,string> of current route values
    member this.GetRouteValues () =
        this.RouteValues
        |> Seq.map (fun kvp -> kvp.Key, kvp.Value.ToString())
        |> Map.ofSeq
    
    /// Attempt to safely-acquire route value
    member this.TryGetRouteValue (key : string) =
        let parseRoute = tryParseWith this.RouteValues.TryGetValue             
        match parseRoute key with
        | Some v -> Some (v.ToString())
        | None   -> None

    /// Retrieve the HttpRequest body as string
    member this.GetBodyAsync () = task {
        use rd = new StreamReader(this.Body)
        return! rd.ReadToEndAsync()
    }

    /// Retrieve IFormCollection from HttpRequest
    member this.GetFormAsync () = 
        this.ReadFormAsync ()            
    
    /// Retrieve StringCollectionReader for IFormCollection from HttpRequest
    member this.GetFormReaderAsync () = task {
        let! form = this.GetFormAsync ()
        return StringCollectionReader(form)
    }        
 
    /// Retrieve StringCollectionReader for IQueryCollection from HttpRequest
    member this.GetQueryReader () = 
        StringCollectionReader(this.Query)

type HttpResponse with
    /// Set HttpResponse header
    member this.SetHeader 
        (name : string) 
        (content : string) =            
        if not(this.Headers.ContainsKey(name)) then
            this.Headers.Add(name, StringValues(content))

    /// Set HttpResponse ContentType header
    member this.SetContentType contentType =
        this.SetHeader HeaderNames.ContentType contentType

    member this.SetStatusCode (statusCode : int) =            
        this.StatusCode <- statusCode
            
    /// Write bytes to HttpResponse body
    member this.WriteBytes (bytes : byte[]) =
        let byteLen = bytes.Length
        this.ContentLength <- Nullable<int64>(byteLen |> int64)
        this.Body.WriteAsync(bytes, 0, byteLen)            

    /// Write UTF8 string to HttpResponse body
    member this.WriteString (encoding : Encoding) (httpBodyStr : string) =
        let httpBodyBytes = encoding.GetBytes httpBodyStr
        this.WriteBytes httpBodyBytes
