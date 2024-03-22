namespace Falco

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.FSharp.Core.Operators
open Falco.StringPatterns

[<Sealed>]
type FormData(form : IFormCollection, files : IFormFileCollection option) =
    member _.Values =
        form
        |> Seq.map (fun kvp -> kvp.Key, Seq.cast kvp.Value)
        |> FormValueParser.parseKeyValues

    member _.TryGetFile(name : string) =
        match files, name with
        | _, IsNullOrWhiteSpace _
        | None, _ -> None
        | Some files, name ->
            match files.GetFile name with
            | f when isNull f -> None
            | f -> Some f

/// Represents a readable collection of parsed form value.
[<Sealed>]
type FormCollectionReader(form : IFormCollection, files : IFormFileCollection option) =
    inherit StringCollectionReader(
        form
        |> Seq.map (fun kvp -> kvp.Key, kvp.Value :> string seq)
        |> dict)

    /// The IFormFileCollection submitted in the request.
    ///
    /// Note: Only Some if form enctype="multipart/form-data".
    member _.Files = files

    /// Safely retrieves the named IFormFile option from the IFormFileCollection.
    member x.TryGetFormFile (name : string) =
        if StringUtils.strEmpty name then None
        else
            match x.Files with
            | None       -> None
            | Some files ->
                let file = files.GetFile name

                if isNull(file) then None else Some file


/// Represents a readable collection of parsed HTTP header values.
[<Sealed>]
type HeaderCollectionReader(headers : IHeaderDictionary) =
    inherit StringCollectionReader(
        headers
        |> Seq.map (fun kvp -> kvp.Key, kvp.Value :> string seq)
        |> dict)
    
/// Represents a readable collection of query string values.
[<Sealed>]
type QueryCollectionReader(query : IQueryCollection) =
    inherit StringCollectionReader(
        query
        |> Seq.map (fun kvp -> kvp.Key, kvp.Value :> string seq)
        |> dict)

/// Represents a readable collection of route values.
[<Sealed>]
type RouteCollectionReader(route : RouteValueDictionary, query : IQueryCollection) =
    inherit StringCollectionReader(
        route
        |> Seq.map (fun kvp ->
            kvp.Key,
            seq { Convert.ToString(kvp.Value, Globalization.CultureInfo.InvariantCulture) })
        |> dict)

    member _.Query = QueryCollectionReader(query)
    

/// Represents a readable collection of cookie values.
[<Sealed>]
type CookieCollectionReader(cookies: IRequestCookieCollection) =
    inherit StringCollectionReader(
        cookies
        |> Seq.map (fun kvp -> kvp.Key, seq { kvp.Value })
        |> dict)
