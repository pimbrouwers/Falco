module Falco.Multipart

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers

/// Represents the accumulation of form fields and binary data
type MultipartFormData = 
    {
        FormData : KeyValueAccumulator
        FormFiles : FormFileCollection
    }

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

