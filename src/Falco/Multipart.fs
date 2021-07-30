module Falco.Multipart

open System
open System.IO
open System.Net
open System.Threading.Tasks
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
    member this.IsMultipart () =
        this.ContentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0
    
    /// Attempt to stream the HttpRequest body into IFormCollection
    member this.TryStreamFormAsync() : Task<Result<FormCollectionReader, string>> =      
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

        let rec streamForm (form : MultipartFormData) (rd : MultipartReader) : Task<MultipartFormData> =
            let continuation (sectionTask : Task<MultipartSection>) =                
                let section = sectionTask.Result
                
                match section with
                | null    -> Task.FromResult form
                | section -> 
                    match MultipartSection.TryGetContentDisposition(section) with
                    | None -> 
                        // Drain any remaining section body that hasn't been consumed and
                        // read the headers for the next section.
                        streamForm form rd                                        

                    | Some cd when cd.IsFileDisposition() ->
                        let str = new MemoryStream()
                        section.Body.CopyToAsync(str) |> Async.AwaitTask |> Async.RunSynchronously
                    
                        let safeFileName = WebUtility.HtmlEncode cd.FileName.Value                                
                        let file = new FormFile(str, int64 0, str.Length, cd.Name.Value, safeFileName)
                        file.Headers <- this.Headers
                        file.ContentType <- section.ContentType
                        file.ContentDisposition <- section.ContentDisposition                        
                    
                        form.FormFiles.Add(file)                        
                            
                        streamForm form rd

                    | Some cd when cd.IsFormDisposition() ->                        
                        let key = HeaderUtilities.RemoveQuotes(cd.Name).Value
                        let encoding = MultipartSection.GetEncondingFromContentType(section)
                        use str = new StreamReader(section.Body, encoding, true, 1024, true)
                        let formValue = str.ReadToEndAsync() |> Async.AwaitTask |> Async.RunSynchronously
                    
                        form.FormData.Append(key, formValue)       
                    
                        streamForm form rd                        

                    | _ -> Task.FromResult form

            rd.ReadNextSectionAsync()
            |> continueWithTask continuation
            
        match this.IsMultipart(), getBoundary this with 
        | true, Some boundary ->                                
            let continuation (formTask : Task<MultipartFormData>) = 
                let form = formTask.Result
                let formCollection = FormCollection(form.FormData.GetResults(), form.FormFiles)
                let formReader = FormCollectionReader(formCollection, Some formCollection.Files)
                Ok formReader

            let formAcc = { FormData = new KeyValueAccumulator(); FormFiles = new FormFileCollection()  } 
            let multipartReader = new MultipartReader(boundary, this.Body)
            streamForm formAcc multipartReader
            |> continueWith continuation             

        | _, None  -> Error "No boundary found" |> Task.FromResult

        | false, _ -> Error "Not a multipart request" |> Task.FromResult
        

    member this.StreamFormAsync() = 
        let continuation (formTask : Task<Result<FormCollectionReader, string>>) =
            match formTask.Result with
            | Error _ -> FormCollectionReader(FormCollection.Empty, None)
            | Ok form -> form
        
        this.TryStreamFormAsync ()
        |> continueWith continuation
        
    