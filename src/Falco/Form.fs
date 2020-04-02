[<AutoOpen>]
module Falco.Form

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Linq.Expressions
open FastMember
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives

let private typeAccessorCache : ConcurrentDictionary<string, TypeAccessor> = ConcurrentDictionary()

let private createNewInstance<'a> (t : Type) = 
    let expr =
        Expression.Lambda<Func<'a>>(
            Expression.New(t.GetConstructor(Type.EmptyTypes)))

    expr.Compile()    
    
let tryParseForm<'a> (values : IDictionary<string, StringValues>) =
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

type HttpContext with
    member this.GetFormValues () =
        this.Request.Form
        |> Seq.map (fun (kvp) -> kvp.Key, kvp.Value)
        |> Map.ofSeq

    member this.TryGetFormValue (key : string) =
        let parseForm = tryParseWith this.Request.Form.TryGetValue
        match parseForm key with 
        | Some v -> Some v
        | None   -> None

    member this.TryBindFormAsync<'a>() =
        task {
            let! form = this.Request.ReadFormAsync()
            
            return 
                form
                |> Seq.map (fun (kvp) -> kvp.Key, kvp.Value)
                |> dict
                |> tryParseForm<'a> 
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

