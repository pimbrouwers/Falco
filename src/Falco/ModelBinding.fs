[<AutoOpen>]
module Falco.ModelBinding

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Reflection
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open Microsoft.FSharp.Reflection

type StringValues with 
    member this.AsString () =
        match this.Count with
        | 0 -> failwith "StringValues is empty"
        | _ -> this.[0]
    
    member this.AsInteger() =
        match this.AsString() |> parseInt with
        | Some v -> v
        | None   -> failwith "Not a valid int"

type StringCollectionReader (c : seq<KeyValuePair<string,StringValues>>) =
    member __.coll = c |> Seq.map (fun (kvp) -> kvp.Key, kvp.Value) |> dict

let (?) (q : StringCollectionReader) (name : string) = 
    match tryParseWith q.coll.TryGetValue name with
    | Some v -> v 
    | None -> failwith (sprintf "Could not find %s" name)

let private constructorCache = ConcurrentDictionary<string, ConstructorInfo>()
let private fieldCache = ConcurrentDictionary<string, PropertyInfo[]>()

let tryBindModel<'a> (modelDict : IDictionary<string, string[]>) =
    let getConstructor (t : Type) = 
        constructorCache.GetOrAdd(t.Name, FSharpValue.PreComputeRecordConstructorInfo(t))

    let getFields (t : Type) = 
        fieldCache.GetOrAdd(t.Name, FSharpType.GetRecordFields(t))
    
    let (|IsOption|_|) (t : Type) =
        if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Option<_>> then Some t else None

    let (|IsSeq|_|) (t : Type) = 
        if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<seq<_>> then Some t else None

    let (|IsList|_|) (t : Type) =
        if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<list<_>> then Some t else None

    let parseTypeValue (t : Type) (value : string) =
        let result = 
            match t.Name with
            | "Int16"          -> Option.map (fun r -> r :> obj) <| parseInt16 value
            | "Int32"          -> Option.map (fun r -> r :> obj) <| parseInt32 value
            | "Int64"          -> Option.map (fun r -> r :> obj) <| parseInt64 value
            | "Boolean"        -> Option.map (fun r -> r :> obj) <| parseBoolean value
            | "Double"         -> Option.map (fun r -> r :> obj) <| parseFloat value
            | "Decimal"        -> Option.map (fun r -> r :> obj) <| parseDecimal value
            | "DateTime"       -> Option.map (fun r -> r :> obj) <| parseDateTime value
            | "DateTimeOffset" -> Option.map (fun r -> r :> obj) <| parseDateTimeOffset value
            | "Guid"           -> Option.map (fun r -> r :> obj) <| parseGuid value
            | "TimeSpan"       -> Option.map (fun r -> r :> obj) <| parseTimeSpan value
            | "String"         -> Some(value :> obj)
            | _                -> None

        match result with 
        | None   -> Error (sprintf "Could not parse %s" value)
        | Some r -> Ok r

    let parseSeqValues (t : Type) (values : #seq<string>) : Result<obj, string> =                                
        let parseResult tryParser = 
            let result = 
                values 
                |> Seq.map tryParser       
                |> Seq.fold (fun acc i -> 
                    match (i, acc) with 
                    | Some i, Ok r -> Ok (Seq.append r [i])
                    | _            -> Error "" ) (Ok Seq.empty)

            match result with 
            | Ok r    -> Ok (r :> obj)
            | Error _ -> Error (sprintf "Could not parse %A" values) 
            
        match t.Name with
        | "Int16"          -> parseResult parseInt16
        | "Int32"          -> parseResult parseInt32 
        | "Int64"          -> parseResult parseInt64 
        | "Boolean"        -> parseResult parseBoolean
        | "Double"         -> parseResult parseFloat 
        | "Decimal"        -> parseResult parseDecimal 
        | "DateTime"       -> parseResult parseDateTime
        | "DateTimeOffset" -> parseResult parseDateTimeOffset
        | "Guid"           -> parseResult parseGuid 
        | "TimeSpan"       -> parseResult parseTimeSpan
        | "String"         -> Ok (values :> obj)
        | _                -> Error (sprintf "Invalid type for %A" values)        
            
    let rec parseModel (e : string option) (parsed : obj list) (fields : PropertyInfo array) =
        match (e, fields) with
        | Some _, _  -> e, []        // error occured short-circuit
        | None, [||] -> None, parsed // parsing succeeded return
        | _          ->              // still have fields to process
            let currentField = fields |> Array.head
            let remainingFields = fields |> Array.tail

            let valueKey = modelDict.Keys |> Seq.tryFind (fun k -> strEquals k currentField.Name)  

            let fieldType = currentField.PropertyType
            let innerType = 
                match fieldType with
                | IsOption _ | IsSeq _ | IsList _ -> fieldType.GenericTypeArguments |> Array.head
                | _                               -> fieldType
            
            match valueKey, fieldType, innerType with
            | Some k, IsSeq _, t  ->
                match parseSeqValues t (modelDict.[k] |> Array.toSeq) with                
                | Error e -> Some e, []
                | Ok r    -> parseModel None (parsed @ [r]) remainingFields
            | Some k, IsList _, t -> 
                match parseSeqValues t (modelDict.[k] |> Array.toList) with                
                | Error e -> Some e, []
                | Ok r    -> parseModel None (parsed @ [r]) remainingFields
            | Some k, _, t ->
                match parseTypeValue t (modelDict.[k] |> Array.head) with
                | Error e -> Some e, []
                | Ok r    -> parseModel None (parsed @ [r]) remainingFields
            | None, IsOption _, _ -> parseModel None (parsed @ [None :> obj]) remainingFields
            | None, IsSeq _, _    -> parseModel None (parsed @ [Seq.empty :> obj]) remainingFields                
            | None, IsList _, _   -> parseModel None (parsed @ [List.empty :> obj]) remainingFields                
            | _                   -> Some (sprintf "Could not find value for member: %s" currentField.Name), []            
                       
    let t = typeof<'a>    
    let modelFields = getFields(t)
    
    match parseModel None [] modelFields with
    | Some e, _                                                       -> Error e
    | None, fieldValues when fieldValues.Length <> modelFields.Length -> Error "Invalid number of fields provided"
    | None, fieldValues                                               -> 
        let c = getConstructor(t)
        let r = c.Invoke(fieldValues |> List.toArray) :?> 'a
        Ok r
     
type HttpContext with
    member this.GetFormAsync () =
        task {
            let! result = this.Request.ReadFormAsync()

            return
                result
                |> Seq.map (fun (kvp) -> kvp.Key, kvp.Value.ToArray())
                |> Map.ofSeq    
        }

    member this.GetQuery () =        
        this.Request.Query
        |> Seq.map (fun (kvp) -> kvp.Key, kvp.Value.ToArray())
        |> Map.ofSeq  
        
    member this.Form ()  = StringCollectionReader(this.Request.Form)

    member this.Query () = StringCollectionReader(this.Request.Query)

    member this.TryBindFormAsync<'a>() =
        task {
            let! form = this.GetFormAsync()            
            return 
                form 
                |> tryBindModel<'a> 
        }

    member this.TryBindQuery<'a>() =
        this.GetQuery() 
        |> tryBindModel<'a>

let tryBindForm<'a> (error : string -> HttpHandler) (success: 'a -> HttpHandler) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->  
        task {
            let! result = ctx.TryBindFormAsync<'a>()
            return! 
                (match result with
                | Error msg -> error msg
                | Ok form   -> success form) next ctx                
        }

let tryBindQuery<'a> (error : string -> HttpHandler) (success: 'a -> HttpHandler) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->  
        let result = ctx.TryBindQuery<'a>()
        (match result with 
        | Error msg -> error msg
        | Ok query  -> success query) next ctx
