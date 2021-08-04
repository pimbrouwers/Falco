module Falco.Multipart

open System
open System.IO
open System.Net
open System.Threading.Tasks
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers

/// Represents the accumulation of form fields and binary data
type MultipartFormData = 
    { FormData : KeyValueAccumulator
      FormFiles : FormFileCollection }

type MultipartSection with
    /// Attempt to obtain encoding from content type, default to UTF8
    static member GetEncondingFromContentType (section : MultipartSection) =
        match MediaTypeHeaderValue.TryParse(StringSegment(section.ContentType)) with
        | false, _     -> System.Text.Encoding.UTF8
        | true, parsed -> parsed.Encoding

    /// Safely obtain the content disposition header value
    static member TryGetContentDisposition(section : MultipartSection) =                        
        match ContentDispositionHeaderValue.TryParse(StringSegment(section.ContentDisposition)) with
        | false, _     -> None
        | true, parsed -> Some parsed

type HttpRequest with     
    /// Determines if the content type contains multipart
    member this.IsMultipart () : bool =
        this.ContentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0

    /// Attempt to stream the HttpRequest body into IFormCollection
    member this.TryStreamFormAsync () : Task<Result<FormCollectionReader, string>> =      
        let getBoundary(request : HttpRequest) =
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
            let isMultipart = this.IsMultipart ()
            let boundary = this |> getBoundary

            match isMultipart, boundary with 
            | true, Some boundary ->         
                let formAcc = { FormData = new KeyValueAccumulator(); FormFiles = new FormFileCollection()  } 
                let multipartReader = new MultipartReader(boundary, this.Body)

                let! form = streamForm formAcc multipartReader
                let formCollection = FormCollection(form.FormData.GetResults(), form.FormFiles)
                let formReader = FormCollectionReader(formCollection, Some formCollection.Files)
                return Ok formReader
                                
            | _, None  -> return Error "No boundary found"

            | false, _ -> return Error "Not a multipart request"
        }
        

    member this.StreamFormAsync() : Task<FormCollectionReader> = 
        task {
            let! result = this.TryStreamFormAsync()
            let form = 
                match result with
                | Error _ -> FormCollectionReader(FormCollection.Empty, None)
                | Ok form -> form
            return form 
        }        
        
    