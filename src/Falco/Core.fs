namespace Falco

[<AutoOpen>]
module Core =
    open System    
    open System.Text
    open System.Threading.Tasks    
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.Logging
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

    /// The default HttpFunc
    let defaultHttpFunc : HttpFunc = 
        Some >> Task.FromResult

    /// Compose ("glue") HttpHandler's together
    let compose (handler1 : HttpHandler) (handler2 : HttpHandler) : HttpHandler =
        fun (fn : HttpFunc) ->
            let next = fn |> handler2 |> handler1
            fun (ctx : HttpContext) ->
                match ctx.Response.HasStarted with
                | true  -> fn ctx
                | false -> next ctx
        
    let (>=>) = compose

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
            let len = bytes.Length
            this.Response.ContentLength <- Nullable<int64>(len |> int64)
            this.Response.Body.WriteAsync(bytes, 0, len).ContinueWith(fun _ -> Some this)
            

        /// Write UTF8 string to HttpResponse body
        member this.WriteString (str : string) =
            this.WriteBytes (Encoding.UTF8.GetBytes str)

module StringUtils = 
    open System 

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
   
module StringParser =
    open System

    /// Helper to wrap .NET tryParser's
    let tryParseWith (tryParseFunc: string -> bool * _) = 
        tryParseFunc >> function
        | true, v    -> Some v
        | false, _   -> None
  
    let parseInt            = tryParseWith Int32.TryParse
    let parseInt16          = tryParseWith Int16.TryParse
    let parseInt32          = parseInt
    let parseInt64          = tryParseWith Int64.TryParse
    let parseBoolean        = tryParseWith Boolean.TryParse
    let parseFloat          = tryParseWith Double.TryParse
    let parseDecimal        = tryParseWith Decimal.TryParse
    let parseDateTime       = tryParseWith DateTime.TryParse
    let parseDateTimeOffset = tryParseWith DateTimeOffset.TryParse
    let parseTimeSpan       = tryParseWith TimeSpan.TryParse
    let parseGuid           = tryParseWith Guid.TryParse

    /// Attempt to parse, or failwith message
    let parseOrFail parser msg v =
        match parser v with 
        | Some v -> v
        | None   -> failwith msg

    /// Attempt to parse array, returns none for failure
    let tryParseArray parser ary =
        ary
        |> Seq.map parser
        |> Seq.fold (fun acc i ->
            match (i, acc) with
            | Some i, Some acc -> Some (Array.append acc [|i|])
            | _ -> None) (Some [||])    
        