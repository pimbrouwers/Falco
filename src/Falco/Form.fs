[<AutoOpen>]
module Falco.Form

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.IO        
open System.Linq.Expressions
open System.Net
open FastMember
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Net.Http.Headers                
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.Primitives

let private typeAccessorCache : ConcurrentDictionary<string, TypeAccessor> = ConcurrentDictionary()

let private createNewInstance<'a> (t : Type) = 
    let expr =
        Expression.Lambda<Func<'a>>(
            Expression.New(t.GetConstructor(Type.EmptyTypes)))

    expr.Compile()    

let parseForm<'a> (values : IDictionary<string, StringValues>) =
    let t = typeof<'a>    
    let acc = typeAccessorCache.GetOrAdd(t.Name, TypeAccessor.Create(t))
    let newModel = createNewInstance<'a>(t).Invoke()
    let members = acc.GetMembers() |> Array.ofSeq
    let keys = values.Keys |> Seq.toArray

    let parseStringValue tryParser v =
        match tryParser v with
        | Some d -> Ok (d :> obj)
        | None   -> Error (sprintf "Could not parse %s" v) 
        
    let error = 
        members
        |> Array.filter (fun m -> m.CanWrite)
        |> Array.fold (fun (e : string option) (m : Member) ->            
            if e.IsSome then e
            else 
                let parseResult =
                    match keys |> Array.tryFind (fun k -> strEquals k m.Name) with
                    | Some k ->
                        let entry = values.[k]
                        match entry.Count with             
                        | c when c > 1 -> Error (sprintf "Too many StringValues for: %s" m.Name)
                        | _ ->                      
                            let v = entry.Item 0
                            
                            match m.Type.Name with 
                            | "String"         -> Ok (v :> obj)
                            | "Int16"          -> parseStringValue parseInt16 v
                            | "Int32"          -> parseStringValue parseInt32 v            
                            | "Int64"          -> parseStringValue parseInt64 v
                            | "Boolean"        -> parseStringValue parseBoolean v
                            | "Double"         -> parseStringValue parseFloat v
                            | "Decimal"        -> parseStringValue parseDecimal v
                            | "DateTime"       -> parseStringValue parseDateTime v
                            | "DateTimeOffset" -> parseStringValue parseDateTimeOffset v
                            | "Guid"           -> parseStringValue parseGuid v
                            | "TimeSpan"       -> parseStringValue parseTimeSpan v                                
                            | _                -> Error (sprintf "%s is not supported" m.Type.Name)
                    | None -> Error (sprintf "Could not find value for member: %s" m.Name)  

                match parseResult with
                | Error e -> Some e
                | Ok r    -> 
                    acc.[newModel, m.Name] <- r
                    None
           ) None

    match error with
    | Some e -> Error e
    | None   -> Ok newModel

type MultipartFormData = 
    {
        FormData : KeyValueAccumulator
        FormFiles : FormFileCollection
    }

type MultipartSection with
    member this.GetEncoding() =
        // Default to UTF8
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
       
type HttpRequest with                    
    member this.GetBoundary() =
        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // The spec at https://tools.ietf.org/html/rfc2046#section-5.1 states that 70 characters is a reasonable limit.
        let lengthLimit = 70 
        let contentType = MediaTypeHeaderValue.Parse(StringSegment(this.ContentType))
        let boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value; 
        match boundary with
        | b when isNull b                -> None
        | b when b.Length > lengthLimit  -> None
        | b                              -> Some b

    member this.IsMultipart() =
        this.ContentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0

type HttpContext with
    member this.TryBindFormAsync<'a>() =
        task {
            let! form = this.Request.ReadFormAsync()
            
            return 
                form
                |> Seq.map (fun (kvp) -> kvp.Key, kvp.Value)
                |> dict
                |> parseForm<'a> 
        }

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
            match this.Request.IsMultipart(), this.Request.GetBoundary() with 
            | false, _            -> return Error "Not a multipart request"
            | true, None          -> return Error "No boundary found"
            | true, Some boundary ->                    
                let! form = 
                    streamForm 
                        { FormData = new KeyValueAccumulator(); FormFiles = new FormFileCollection()  } 
                        (new MultipartReader(boundary, this.Request.Body))
        
                return Ok (FormCollection(form.FormData.GetResults(), form.FormFiles))
        }

let tryBindForm<'a> (error : string -> HttpHandler) (success: 'a -> HttpHandler) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->        
        task {
            let! result = ctx.TryBindFormAsync<'a>()
            return! 
                (match result with
                | Error msg -> error msg
                | Ok form   -> success form) next ctx                
        }