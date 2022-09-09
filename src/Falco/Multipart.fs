namespace Falco

open System
open System.IO
open System.Net
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers

module Multipart =
    type private MultipartSectionData =
        | NoMultipartData
        | FormValueData of key : string * value : string
        | FormFileData of FormFile

    type private MultipartSection with
        /// Attempt to obtain encoding from content type, default to UTF8.
        static member private GetEncondingFromContentType(section : MultipartSection) =
            match MediaTypeHeaderValue.TryParse(StringSegment(section.ContentType)) with
            | false, _     -> System.Text.Encoding.UTF8
            | true, parsed -> parsed.Encoding

        /// Safely obtain the content disposition header value.
        static member private TryGetContentDisposition(section : MultipartSection) =
            match ContentDispositionHeaderValue.TryParse(StringSegment(section.ContentDisposition)) with
            | false, _     -> None
            | true, parsed -> Some parsed

        member private x.StreamSectionAsync() =
            task {
                match MultipartSection.TryGetContentDisposition(x) with
                | Some cd when cd.IsFileDisposition() ->
                    use str = new MemoryStream()
                    do! x.Body.CopyToAsync(str, 8192)

                    let safeFileName = WebUtility.HtmlEncode cd.FileName.Value
                    let file = new FormFile(str, int64 0, str.Length, cd.Name.Value, safeFileName)

                    // necessary to prevent null reference exception when setting
                    // properties below. See:
                    // https://github.com/dotnet/aspnetcore/blob/ca2238e75173d1f04ff0664c53dc443716a01b9d/src/Http/Http/src/FormFile.cs#L48
                    file.Headers <- new HeaderDictionary()

                    file.ContentType <- x.ContentType
                    file.ContentDisposition <- x.ContentDisposition

                    return FormFileData file

                | Some cd when cd.IsFormDisposition() ->
                    let key = HeaderUtilities.RemoveQuotes(cd.Name).Value
                    let encoding = MultipartSection.GetEncondingFromContentType(x)
                    use str = new StreamReader(x.Body, encoding, true, 1024, true)
                    let! formValue = str.ReadToEndAsync()

                    return FormValueData (key, formValue)

                | Some _
                | None ->
                    return NoMultipartData
            }

    type MultipartReader with
        member x.StreamFormAsync() =
            task {
                let formData = new KeyValueAccumulator()
                let formFiles = new FormFileCollection()

                let mutable shouldContinue = true

                while shouldContinue do
                    let! section = x.ReadNextSectionAsync()

                    match isNull section with
                    | true ->
                        shouldContinue <- false

                    | false ->
                        let! sectionData = section.StreamSectionAsync()

                        match sectionData with
                        | FormFileData file          -> formFiles.Add(file)
                        | FormValueData (key, value) -> formData.Append(key, value)
                        | NoMultipartData            -> shouldContinue <- false

                let formCollection = FormCollection(formData.GetResults(), formFiles) :> IFormCollection
                return formCollection
            }


    type private HttpRequest with
        /// Determines if the content type contains multipart.
        member private x.IsMultipart () : bool =
            x.ContentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0

        member private x.GetBoundary() =
            // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
            // The spec at https://tools.ietf.org/html/rfc2046#section-5.1 states that 70 characters is a reasonable limit.
            let lengthLimit = 70
            let contentType = MediaTypeHeaderValue.Parse(StringSegment(x.ContentType))
            let boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
            match boundary with
            | b when isNull b               -> None
            | b when b.Length > lengthLimit -> None
            | b                             -> Some b

        /// Attempt to stream the HttpRequest body into IFormCollection.
        member x.TryStreamFormAsync () : Task<Result<IFormCollection, string>> =
            task {
                match x.IsMultipart(), x.GetBoundary() with
                | true, Some boundary ->
                    let multipartReader = new MultipartReader(boundary, x.Body)
                    let! formCollection = multipartReader.StreamFormAsync()
                    return Ok formCollection

                | _, None  -> return Error "No boundary found"

                | false, _ -> return Error "Not a multipart request"
            }