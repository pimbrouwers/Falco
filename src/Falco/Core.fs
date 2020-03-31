namespace Falco

[<AutoOpen>]
module Core =
    open System
    open System.Text
    open System.Threading.Tasks
    open FSharp.Control.Tasks.V2.ContextInsensitive
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.Primitives
    open Microsoft.Net.Http.Headers

    exception InvalidDependencyException of string

    type HttpFuncResult = Task<HttpContext option>

    type HttpFunc = HttpContext -> HttpFuncResult

    type HttpHandler = HttpFunc -> HttpFunc    

    type HttpVerb = GET | POST | PUT | DELETE | ANY

    type HttpEndpoint = 
        {
            Pattern : string   
            Verb  : HttpVerb
            Handler : HttpHandler
        }
            
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

    // strings    
    let toStr x = x.ToString()

    let strJoin (sep : string) (lst : string array) = String.Join(sep, lst)
        
    // parsing
    let tryParseWith (tryParseFunc: string -> bool * _) = tryParseFunc >> function
        | true, v    -> Some v
        | false, _   -> None
            
    let parseInt = tryParseWith Int32.TryParse
    
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

        member this.RouteValues () =
            this.Request.RouteValues
            |> Seq.map (fun kvp -> kvp.Key, toStr kvp.Value)
            |> Map.ofSeq
            
        member this.RouteValue (key : string) =
            let parseRoute = tryParseWith this.Request.RouteValues.TryGetValue             
            match parseRoute key with
            | Some v -> Some (toStr v)
            | None   -> None

        member this.GetService<'a> () =
            let t = typeof<'a>
            match this.RequestServices.GetService t with
            | null    -> raise (InvalidDependencyException t.Name)
            | service -> service :?> 'a