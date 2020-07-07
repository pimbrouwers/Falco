[<AutoOpen>]
module Falco.Extensions

open System
open System.IO
open System.Net
open System.Security.Claims
open System.Text
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Antiforgery    
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers
open Falco.StringUtils
open Falco.StringParser

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

    /// Retrieve StringCollectionReader for IFormCollection from HttpRequest
    member this.GetFormReaderAsync () = task {
        let! form = this.ReadFormAsync()        
        return FormCollectionReader(form, None)
    }        

    /// Retrieve StringCollectionReader for IQueryCollection from HttpRequest
    member this.GetQueryReader () = 
        StringCollectionReader(this.Query)

    /// Determines if the content type contains multipart
    member this.IsMultipart () =
        this.ContentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0
    
    /// Attempt to stream the HttpRequest body into IFormCollection
    member this.TryStreamFormAsync() =      
        let getBoundary (request : HttpRequest) = 
            // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
            // The spec at https://tools.ietf.org/html/rfc2046#section-5.1 states that 70 characters is a reasonable limit.
            let lengthLimit = 70 
            let contentType = MediaTypeHeaderValue.Parse(StringSegment(request.ContentType))
            let boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value; 
            match boundary with
            | b when isNull b                -> None
            | b when b.Length > lengthLimit  -> None
            | b                              -> Some b

        let rec streamForm (form : MultipartFormData) (rd : MultipartReader) =
            task {  
                let! section = rd.ReadNextSectionAsync()
                match section with
                | null    -> return form                            
                | section -> 
                    match MultipartSection.TryGetContentDisposition(section) with
                    | None -> 
                        // Drain any remaining section body that hasn't been consumed and
                        // read the headers for the next section.
                        return! streamForm form rd                
                    | Some cd when cd.IsFileDisposition() ->
                            let str = new MemoryStream()
                            do! section.Body.CopyToAsync(str)
                    
                            let safeFileName = WebUtility.HtmlEncode cd.FileName.Value                                
                            let file = new FormFile(str, int64 0, str.Length, cd.Name.Value, safeFileName)
                            file.Headers <- this.Headers
                            file.ContentType <- section.ContentType
                            file.ContentDisposition <- section.ContentDisposition                        
                    
                            form.FormFiles.Add(file)                        
                    
                            return! streamForm form rd
                    | Some cd when cd.IsFormDisposition() ->                        
                            let key = HeaderUtilities.RemoveQuotes(cd.Name).Value
                            let encoding = MultipartSection.GetEncondingFromContentType(section)
                            use str = new StreamReader(section.Body, encoding, true, 1024, true)
                            let formValue = str.ReadToEndAsync() |> Async.AwaitTask |> Async.RunSynchronously
                    
                            form.FormData.Append(key, formValue)       
                    
                            return! streamForm form rd
                    | _ -> return form
            }
            
        task {
            match this.IsMultipart(), getBoundary this with 
            | true, Some boundary ->                    
                let! form = 
                    streamForm 
                        { FormData = new KeyValueAccumulator(); FormFiles = new FormFileCollection()  } 
                        (new MultipartReader(boundary, this.Body))
        
                let formCollection = FormCollection(form.FormData.GetResults(), form.FormFiles)
                return Ok (FormCollectionReader(formCollection, Some formCollection.Files))

            | _, None  -> return Error "No boundary found"

            | false, _ -> return Error "Not a multipart request"
        }

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

type HttpContext with       
    // ------------
    // IoC & Logging
    // ------------

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

    // ------------
    // XSS
    // ------------

    /// Returns (and optional creates) csrf tokens for the current session
    member this.GetCsrfToken () =
        let antiFrg = this.GetService<IAntiforgery>()
        antiFrg.GetAndStoreTokens this

    /// Checks the presence and validity of CSRF token 
    member this.ValidateCsrfToken () =
        let antiFrg = this.GetService<IAntiforgery>()        
        antiFrg.IsRequestValidAsync this

    // ------------
    // Auth
    // ------------
    /// Returns the current user (IPrincipal) or None
    member this.GetUser () =
        match this.User with
        | null -> None
        | _    -> Some this.User

    /// Returns authentication status of IPrincipal, false on null
    member this.IsAuthenticated () =
        let isAuthenciated (user : ClaimsPrincipal) = 
            let identity = user.Identity
            match identity with 
            | null -> false
            | _    -> identity.IsAuthenticated

        match this.GetUser () with
        | None      -> false 
        | Some user -> isAuthenciated user

type IApplicationBuilder with
    /// Enable Falco exception handling middleware. 
    ///
    /// It is recommended to specify this BEFORE any other middleware.
    member this.UseExceptionMiddleware (exceptionHandler : ExceptionHandler) =
        this.UseMiddleware<ExceptionHandlingMiddleware> exceptionHandler

    /// Activate Falco integration with IEndpointRouteBuilder
    member this.UseHttpEndPoints (endPoints : HttpEndpoint list) =
        this.UseEndpoints(fun r -> 
                for e in endPoints do            
                    let rd = HttpHandler.toRequestDelegate e.Handler
                    
                    match e.Verb with
                    | GET     -> r.MapGet(e.Pattern, rd)
                    | HEAD    -> r.MapMethods(e.Pattern, [ HttpMethods.Head ], rd)
                    | POST    -> r.MapPost(e.Pattern, rd)
                    | PUT     -> r.MapPut(e.Pattern, rd)
                    | PATCH   -> r.MapMethods(e.Pattern, [ HttpMethods.Patch ], rd)
                    | DELETE  -> r.MapDelete(e.Pattern, rd)
                    | OPTIONS -> r.MapMethods(e.Pattern, [ HttpMethods.Options ], rd)
                    | TRACE   -> r.MapMethods(e.Pattern, [ HttpMethods.Trace ], rd)
                    | ANY     -> r.Map(e.Pattern, rd)
                    |> ignore)
            
    /// Enable Falco not found handler.
    ///
    /// This handler is terminal and must be specified 
    /// AFTER all other middlewar.
    member this.UseNotFoundHandler (notFoundHandler : HttpHandler) =
        this.Run(HttpHandler.toRequestDelegate notFoundHandler)
