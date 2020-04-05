[<AutoOpen>]
module Falco.ModelBinding

open System.Collections.Generic
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives

type StringCollectionReader (c : seq<KeyValuePair<string,StringValues>>) = 
    let coll = c |> Seq.map (fun (kvp) -> kvp.Key, kvp.Value) |> dict

    member _.TryGetValue (name : string) = 
        match name |> tryParseWith coll.TryGetValue with
        | Some v when v.Count > 0 -> Some v
        | _                       -> None
    
    member this.GetValue (name : string) = 
        match this.TryGetValue name with
        | Some v -> v 
        | None -> failwith (sprintf "Could not find %s" name)

    member this.TryGetString = this.TryGetValue

    member this.TryGetInt16 (name : string) =
        match this.TryGetValue name with 
        | Some v -> parseInt16 v.[0]
        | None   -> None
        
    member this.TryGetInt32 (name : string) =
        match this.TryGetValue name with 
        | Some v -> parseInt32 v.[0]
        | None   -> None
        
    member this.TryGetInt = this.TryGetInt32

    member this.TryGetInt64 (name : string) =
        match this.TryGetValue name with 
        | Some v -> parseInt64 v.[0]
        | None   -> None
        
    member this.TryGetBoolean (name : string) =
        match this.TryGetValue name with 
        | Some v -> parseBoolean v.[0]
        | None   -> None
        
    member this.TryGetFloat (name : string) =
        match this.TryGetValue name with 
        | Some v -> parseFloat v.[0]
        | None   -> None
        
    member this.TryGetDecimal (name : string) =
        match this.TryGetValue name with 
        | Some v -> parseDecimal v.[0]
        | None   -> None
        
    member this.TryGetDateTime (name : string) =
        match this.TryGetValue name with 
        | Some v -> parseDateTime v.[0]
        | None   -> None
        
    member this.TryGetDateTimeOffset (name : string) =
        match this.TryGetValue name with 
        | Some v -> parseDateTimeOffset v.[0]
        | None   -> None
        
    member this.TryGetGuid (name : string) =
        match this.TryGetValue name with 
        | Some v -> parseGuid v.[0]
        | None   -> None
        
    member this.TryGetTimeSpan (name : string) =
        match this.TryGetValue name with 
        | Some v -> parseTimeSpan v.[0]
        | None   -> None
       
let (?) (q : StringCollectionReader) = q.GetValue

type StringValues with 
    member this.AsString () =
        match this.Count with
        | 0 -> failwith "StringValues is empty"
        | _ -> this.[0]
    
    member this.AsInt16() =
        match this.AsString() |> parseInt16 with
        | Some v -> v
        | None   -> failwith "Not a valid Int16"
        
    member this.AsInt32() =
        match this.AsString() |> parseInt32 with
        | Some v -> v
        | None   -> failwith "Not a valid Int32"
       
    member this.AsInt = this.AsInt32

    member this.AsInt64() =
        match this.AsString() |> parseInt64 with
        | Some v -> v
        | None   -> failwith "Not a valid Int64"
        
    member this.AsBoolean() =
        match this.AsString() |> parseBoolean with
        | Some v -> v
        | None   -> failwith "Not a valid Boolean"
        
    member this.AsFloat() =
        match this.AsString() |> parseFloat with
        | Some v -> v
        | None   -> failwith "Not a valid Float"
        
    member this.AsDecimal() =
        match this.AsString() |> parseDecimal with
        | Some v -> v
        | None   -> failwith "Not a valid Decimal"
        
    member this.AsDateTime() =
        match this.AsString() |> parseDateTime with
        | Some v -> v
        | None   -> failwith "Not a valid DateTime"
        
    member this.AsDateTimeOffset() =
        match this.AsString() |> parseDateTimeOffset with
        | Some v -> v
        | None   -> failwith "Not a valid DateTimeOffset"
        
    member this.AsGuid() =
        match this.AsString() |> parseGuid with
        | Some v -> v
        | None   -> failwith "Not a valid Guid"
        
    member this.AsTimeSpan() =
        match this.AsString() |> parseTimeSpan with
        | Some v -> v
        | None   -> failwith "Not a valid TimeSpan"
                
type HttpContext with  
    member this.GetForm () = 
        StringCollectionReader(this.Request.Form)

    member this.GetFormAsync () = 
        task {
            let! form = this.Request.ReadFormAsync()
            return StringCollectionReader(form)
        }

    member this.GetQuery () = 
        StringCollectionReader(this.Request.Query)

let tryBindForm 
    (bind : StringCollectionReader -> Result<'a, string> ) 
    (err : string -> HttpHandler) 
    (success : 'a -> HttpHandler) : HttpHandler =    
    fun (next : HttpFunc) (ctx : HttpContext) ->  
        task {
            let! form = ctx.GetFormAsync()

            return! 
                (match form |> bind with
                | Ok m      -> success m
                | Error msg -> err msg) next ctx
        }

let tryBindQuery
    (bind : StringCollectionReader -> Result<'a, string> ) 
    (err : string -> HttpHandler) 
    (success : 'a -> HttpHandler) : HttpHandler =    
    fun (next : HttpFunc) (ctx : HttpContext) ->  
        (match ctx.GetQuery() |> bind with
        | Ok m      -> success m 
        | Error msg -> err msg) next ctx