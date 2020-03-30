namespace Wildebeest

[<AutoOpen>]
module Core =
    open System
    open System.Text
    open System.Threading.Tasks
    open FSharp.Control.Tasks.V2.ContextInsensitive
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.Primitives
    open Microsoft.Net.Http.Headers

    type HttpFuncResult = Task<HttpContext option>

    type HttpFunc = HttpContext -> HttpFuncResult

    type HttpHandler = HttpFunc -> HttpFunc    

    type HttpVerb = GET | POST | PUT | DELETE | ALL

    type HttpEndpoint = 
        {
            Pattern : string   
            Verb  : HttpVerb
            Handler : HttpHandler
        }
    
    let toStr x = x.ToString()
    
    let tryParseWith (tryParseFunc: string -> bool * _) = tryParseFunc >> function
        | true, v    -> Some v
        | false, _   -> None
                
    let compose (handler1 : HttpHandler) (handler2 : HttpHandler) : HttpHandler =
        fun (final : HttpFunc) ->
            let func = final |> handler2 |> handler1
            fun (ctx : HttpContext) ->
                match ctx.Response.HasStarted with
                | true  -> final ctx
                | false -> func ctx
        
    let (>=>) = compose
    
    let createRequestDelete (handler : HttpHandler) =
        let fn = handler (Some >> Task.FromResult)
        RequestDelegate(fun ctx -> Task.Run(fun _ -> fn ctx |> ignore))

    type HttpContext with    
        member this.SetHeader name (content : string) =
            if not(this.Response.Headers.ContainsKey(name)) then
                this.Response.Headers.Add(name, StringValues(content))

        member this.SetContentType contentType =
            this.SetHeader HeaderNames.ContentType contentType

        member this.WriteBytes (bytes : byte[]) =        
            task {            
                bytes.CopyTo(this.Response.BodyWriter.GetMemory(bytes.Length).Span)
                this.Response.BodyWriter.Advance(bytes.Length)
                this.Response.BodyWriter.FlushAsync(this.RequestAborted) |> ignore
                return Some this
            }

        member this.WriteString (str : string) =
            this.WriteBytes (Encoding.UTF8.GetBytes str)

        member this.RouteValue (key : string) =
            let parseRoute = tryParseWith this.Request.RouteValues.TryGetValue 
            match parseRoute key with
            | Some v -> Some (toStr v)
            | None   -> None