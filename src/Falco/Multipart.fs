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
        /// Attempts to obtain encoding from content type, default to UTF8.
        static member private GetEncodingFromContentType(section : MultipartSection) =
            match MediaTypeHeaderValue.TryParse(StringSegment(section.ContentType)) with
            | false, _     -> System.Text.Encoding.UTF8
            | true, parsed -> parsed.Encoding

        /// Safely obtains the content disposition header value.
        static member private TryGetContentDisposition(section : MultipartSection) =
            match ContentDispositionHeaderValue.TryParse(StringSegment(section.ContentDisposition)) with
            | false, _     -> None
            | true, parsed -> Some parsed

        member private x.StreamSectionAsync() =
            task {
                match MultipartSection.TryGetContentDisposition(x) with
                | Some cd when cd.IsFileDisposition() ->
                    let str = new MemoryStream()
                    do! x.Body.CopyToAsync(str)

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
                    let encoding = MultipartSection.GetEncodingFromContentType(x)
                    use str = new StreamReader(x.Body, encoding, true, 1024, true)
                    let! formValue = str.ReadToEndAsync()

                    return FormValueData (key, formValue)

                | Some _
                | None ->
                    return NoMultipartData
            }

    type MultipartReader with
        member x.StreamSectionsAsync() =
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