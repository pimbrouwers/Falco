module Falco.Multipart

open System
open System.IO        
open System.Net
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Net.Http.Headers                
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.Primitives

/// Represents the accumulation of form fields and binary data
type MultipartFormData = 
    {
        FormData : KeyValueAccumulator
        FormFiles : FormFileCollection
    }

type MultipartSection with
    /// Attempt to obtain encoding, default to UTF8
    member this.GetEncoding() =        
        match MediaTypeHeaderValue.TryParse(StringSegment(this.ContentType)) with
        | false, _     -> System.Text.Encoding.UTF8
        | true, parsed -> 
            match System.Text.Encoding.UTF7.Equals(parsed.Encoding) with
            | true -> System.Text.Encoding.UTF8
            | false -> parsed.Encoding

    member this.TryGetContentDisposition() =                        
        match ContentDispositionHeaderValue.TryParse(StringSegment(this.ContentDisposition)) with
        | false, _     -> None
        | true, parsed -> Some parsed

type HttpContext with
    member this.GetBoundary() =
        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // The spec at https://tools.ietf.org/html/rfc2046#section-5.1 states that 70 characters is a reasonable limit.
        let lengthLimit = 70 
        let contentType = MediaTypeHeaderValue.Parse(StringSegment(this.Request.ContentType))
        let boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value; 
        match boundary with
        | b when isNull b                -> None
        | b when b.Length > lengthLimit  -> None
        | b                              -> Some b

    member this.IsMultipart() =
        this.Request.ContentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0

    member this.TryStreamFormAsync() =                
        let rec streamForm (form : MultipartFormData) (rd : MultipartReader) =
            task {  
                let! section = rd.ReadNextSectionAsync()
                match section with
                | null    -> return form                            
                | section -> 
                    match section.TryGetContentDisposition() with
                    | None -> 
                        // Drain any remaining section body that hasn't been consumed and
                        // read the headers for the next section.
                        return! streamForm form rd                
                    | Some cd when cd.IsFileDisposition() ->
                            let str = new MemoryStream()
                            do! section.Body.CopyToAsync(str)
                    
                            let safeFileName = WebUtility.HtmlEncode cd.FileName.Value                                
                            let file = new FormFile(str, int64 0, str.Length, cd.Name.Value, safeFileName)
                            file.Headers <- this.Request.Headers
                            file.ContentType <- section.ContentType
                            file.ContentDisposition <- section.ContentDisposition                        
                    
                            form.FormFiles.Add(file)                        
                    
                            return! streamForm form rd
                    | Some cd when cd.IsFormDisposition() ->                        
                            let key = HeaderUtilities.RemoveQuotes(cd.Name).Value
                            let encoding = section.GetEncoding()
                            use str = new StreamReader(section.Body, encoding, true, 1024, true)
                            let formValue = str.ReadToEndAsync() |> Async.AwaitTask |> Async.RunSynchronously
                    
                            form.FormData.Append(key, formValue)       
                    
                            return! streamForm form rd
                    | _ -> return form
            }
    
        task {
            match this.IsMultipart(), this.GetBoundary() with 
            | false, _            -> return Error "Not a multipart request"
            | true, None          -> return Error "No boundary found"
            | true, Some boundary ->                    
                let! form = 
                    streamForm 
                        { FormData = new KeyValueAccumulator(); FormFiles = new FormFileCollection()  } 
                        (new MultipartReader(boundary, this.Request.Body))
        
                return Ok (FormCollection(form.FormData.GetResults(), form.FormFiles))
        }


/// Safely stream multipart form date into IFormCollection
let tryStreamForm (error: string -> HttpHandler) (success: FormCollection -> HttpHandler) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! result = ctx.TryStreamFormAsync()

            return! 
                (match result with
                | Error msg -> error msg
                | Ok form   -> success form) next ctx
        }