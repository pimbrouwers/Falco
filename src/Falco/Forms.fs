namespace Falco.Forms

open System
open System.Collections.Generic
open System.Net
open Microsoft.AspNetCore.Http
open Microsoft.FSharp.Core.Operators
open Falco.StringPatterns

type FormValue =
    | FNull
    | FBool of bool
    | FInt of int
    | FFloat of float
    | FString of string
    | FList of FormValue list
    | FObject of (string * FormValue) list

type internal FormValueParser(formValues : IDictionary<string, string seq>) =
    let rec parseKeyValues () =
        let formAcc = newFormAcc()

        for kvp in formValues do            
            let keys = 
                kvp.Key 
                |> WebUtility.UrlDecode 
                |> fun key -> key.Split('.', StringSplitOptions.RemoveEmptyEntries)
                |> List.ofArray
                
            parseNested formAcc keys kvp.Value

        formAcc
        |> formAccToValues

    and parseNested (acc : Dictionary<string, FormValue>) (keys : string list) (values : string seq) =
        match keys with
        | [] -> ()
        | [IsListKey key] ->
            // list of primitives
            values
            |> Seq.map parseFormPrimitive
            |> List.ofSeq
            |> FList
            |> fun x -> acc.TryAdd(key, x) |> ignore

        | [key] ->
            // primitive
            values
            |> Seq.tryHead
            |> Option.map parseFormPrimitive
            |> Option.defaultValue FNull
            |> fun x -> acc.TryAdd(key, x) |> ignore

        | IsListKey key :: remainingKeys ->
            // list of complex types
            if acc.ContainsKey key then
                match acc[key] with
                | FList formList ->
                    formList
                    |> Seq.collect (fun formValue ->
                        match formValue with
                        | FObject formObject ->
                            let formObjectAcc = formValuesToAcc formObject
                            parseNested formObjectAcc remainingKeys values
                            Seq.singleton (formObjectAcc |> formAccToValues)
                        | _ -> Seq.empty)
                    |> List.ofSeq
                    |> FList
                    |> fun x -> acc[key] <- x
                | _ -> ()
            else
                values
                |> Seq.map (fun value ->
                    let listValueAcc = newFormAcc()
                    parseNested listValueAcc remainingKeys (seq { value })
                    listValueAcc
                    |> formAccToValues)
                |> List.ofSeq
                |> FList
                |> fun x -> acc.TryAdd(key, x) |> ignore

        | key :: remainingKeys ->
            // complex type
            if acc.ContainsKey key then
                match acc[key] with
                | FObject formObject ->
                    let formObjectAcc = formValuesToAcc formObject
                    parseNested formObjectAcc remainingKeys values
                    acc[key] <- formObjectAcc |> formAccToValues
                | _ -> ()
            else
                let formObjectAcc = newFormAcc()
                parseNested formObjectAcc remainingKeys values
                acc.TryAdd(key, formObjectAcc |> formAccToValues) |> ignore

    and parseFormPrimitive (x : string) =
        let decoded = WebUtility.UrlDecode x
        match decoded with
        | IsNullOrWhiteSpace _ -> FNull
        | IsTrue x
        | IsFalse x -> FBool x
        | IsInt32 x -> FInt x
        | IsFloat x -> FFloat x
        | x -> FString x

    and (|IsListKey|_|) (x : string) =
        if x.EndsWith("[]") then Some (x.Substring(0, x.Length - 2))
        else None

    and newFormAcc () =
        Dictionary<string, FormValue>()

    and formAccToValues (x : Dictionary<string, FormValue>) =
        x |> Seq.map (fun (kvp) -> kvp.Key, kvp.Value) |> List.ofSeq |> FObject

    and formValuesToAcc (x : (string * FormValue) list) =
        let acc = newFormAcc()
        for (key, value) in x do
            acc.TryAdd(key, value) |> ignore
        acc

    member _.Parse() : FormValue =
        parseKeyValues ()

module FormValue =
    let parse (requestBody : string) : FormValue =
        let decoded = WebUtility.UrlDecode requestBody
        let keyValues = decoded.Split('&')
        let formValuePairs = Dictionary<string, IList<string>>()
        let addOrSet (acc : Dictionary<string, IList<string>>) key value =
            if acc.ContainsKey key then
                acc[key].Add(value)
            else
                acc.Add(key, List<string>(Seq.singleton value))
            ()

        for (kv : string) in keyValues do
            match List.ofArray (kv.Split('=')) with // preserve empty entries
            | [] -> ()
            | [key] -> addOrSet formValuePairs key String.Empty
            | [key; value] -> addOrSet formValuePairs key value
            | key :: values when values.Length = 0 -> addOrSet formValuePairs key String.Empty
            | key :: value :: _ -> addOrSet formValuePairs key value

        let formValues = 
            formValuePairs
            |> Seq.map (fun kvp -> kvp.Key, kvp.Value :> IEnumerable<string>)
            |> dict

        FormValueParser(formValues).Parse()

    let parseForm (formCollection : IFormCollection) : FormValue =
        let formValues =
            formCollection
            |> Seq.map (fun kvp -> kvp.Key, Seq.ofArray (kvp.Value.ToArray()))
            |> dict
        
        FormValueParser(formValues).Parse()

[<Sealed>]
type FormData(form : IFormCollection, files : IFormFileCollection option) =
    member _.Values =
        FormValue.parseForm form

    member _.TryGetFile(name : string) =
        match files, name with
        | _, IsNullOrWhiteSpace _
        | None, _ -> None
        | Some files, name ->
            match files.GetFile name with
            | f when isNull f -> None
            | f -> Some f

[<AutoOpen>]
module FormValueExtensions =
    open System.Runtime.CompilerServices

    [<Extension>]
    type FormValueExtensions() =

        [<Extension>]
        static member TryGet (this : FormValue, name : string) : FormValue option =
            match this with
            | FObject props -> List.tryFind (fst >> (=) name) props |> Option.map snd
            | _ -> None

        [<Extension>]
        static member Get (this : FormValue, name : string) : FormValue =
            match this.TryGet name with
            | Some prop -> prop
            | None -> failwithf "Property %s does not exist" name

    let (?) (form : FormValue) (name : string) : FormValue =
        form.Get name
