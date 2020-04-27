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

    /// Represents a missing dependency, thrown on request
    exception InvalidDependencyException of string

    /// The optional result of work performed against the HttpContext
    type HttpFuncResult = Task<HttpContext option>

    /// Specifies work to be performed against the HttpContext
    type HttpFunc = HttpContext -> HttpFuncResult

    /// Represents in-and-out processing of the HttpContext
    type HttpHandler = HttpFunc -> HttpFunc    

    /// Compose ("glue") HttpHandler's together
    let compose (handler1 : HttpHandler) (handler2 : HttpHandler) : HttpHandler =
        fun (fn : HttpFunc) ->
            let next = fn |> handler2 |> handler1
            fun (ctx : HttpContext) ->
                match ctx.Response.HasStarted with
                | true  -> fn ctx
                | false -> next ctx
        
    let (>=>) = compose

    /// Call obj's ToString()
    let toStr x = 
        x.ToString()

    /// Check if string is null or whitespace
    let strEmpty str =
        String.IsNullOrWhiteSpace(str)

    /// Check if string is not null or whitespace
    let strNotEmpty str =
        not(strEmpty str)

    /// Case & culture insensistive string equality
    let strEquals s1 s2 = 
        String.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase)

    /// Join strings with a separator
    let strJoin (sep : string) (lst : string seq) = 
        String.Join(sep, lst)
   
    type HttpContext with   
        /// Attempt to obtain depedency from IServiceCollection
        /// Throws InvalidDependencyException on null
        member this.GetService<'a> () =
            let t = typeof<'a>
            match this.RequestServices.GetService t with
            | null    -> raise (InvalidDependencyException t.Name)
            | service -> service :?> 'a

        /// Set HttpResponse status code
        member this.SetStatusCode (statusCode : int) =            
            this.Response.StatusCode <- statusCode

        /// Set HttpResponse header
        member this.SetHeader name (content : string) =            
            if not(this.Response.Headers.ContainsKey(name)) then
                this.Response.Headers.Add(name, StringValues(content))

        /// Set HttpResponse ContentType header
        member this.SetContentType contentType =
            this.SetHeader HeaderNames.ContentType contentType

        /// Write bytes to HttpResponse body
        member this.WriteBytes (bytes : byte[]) =        
            task {            
                let len = bytes.Length
                bytes.CopyTo(this.Response.BodyWriter.GetMemory(len).Span)
                this.Response.BodyWriter.Advance(len)
                this.Response.BodyWriter.FlushAsync(this.RequestAborted) |> ignore
                this.Response.ContentLength <- Nullable<int64>(len |> int64)
                return Some this
            }

        /// Write string to HttpResponse body
        member this.WriteString (str : string) =
            this.WriteBytes (Encoding.UTF8.GetBytes str)

module StringParser =
    open System

    /// Helper to wrap .NET tryParser's
    let parseWith (tryParseFunc: string -> bool * _) = 
        tryParseFunc >> function
        | true, v    -> Some v
        | false, _   -> None
  
    let parseInt            = parseWith Int32.TryParse
    let parseInt16          = parseWith Int16.TryParse
    let parseInt32          = parseInt
    let parseInt64          = parseWith Int64.TryParse
    let parseBoolean        = parseWith Boolean.TryParse
    let parseFloat          = parseWith Double.TryParse
    let parseDecimal        = parseWith Decimal.TryParse
    let parseDateTime       = parseWith DateTime.TryParse
    let parseDateTimeOffset = parseWith DateTimeOffset.TryParse
    let parseTimeSpan       = parseWith TimeSpan.TryParse
    let parseGuid           = parseWith Guid.TryParse

    /// Attempt to parse, or failwith message
    let parseOrFail parser msg v =
        match parser v with 
        | Some v -> v
        | None   -> failwith msg

    /// Attempt to parse array, returns none for failure
    let parseArray parser ary =
        ary
        |> Seq.map parser
        |> Seq.fold (fun acc i ->
            match (i, acc) with
            | Some i, Some acc -> Some (Array.append acc [|i|])
            | _ -> None) (Some [||])    
        