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
    | FNumber of float
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
        | IsFloat x -> FNumber x
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

module FormValueParser =
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
        FormValueParser.parseForm form

    member _.TryGetFile(name : string) =
        match files, name with
        | _, IsNullOrWhiteSpace _
        | None, _ -> None
        | Some files, name ->
            match files.GetFile name with
            | f when isNull f -> None
            | f -> Some f

module FormValue =
    open Falco 

    let private epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
 
    let inline private floatInRange min max (f : float) =
        let _min = float min
        let _max = float max
        f >= _min && f <= _max

    let asString (f : FormValue) =
        match f with
        | FNull -> Some ""
        | FBool b -> Some (if b then "true" else "false")
        | FNumber n -> Some (string n)
        | FString s -> Some s
        | _ -> None

    let asInt16 (f : FormValue) =
        match f with
        | FNumber x when floatInRange Int16.MinValue Int16.MaxValue x -> Some (Convert.ToInt16 x)
        | FString x -> StringParser.parseInt16 x
        | _ -> None

    let asInt32 (f : FormValue) =
        match f with
        | FNumber x when floatInRange Int32.MinValue Int32.MaxValue x -> Some (Convert.ToInt32 x)
        | FString x -> StringParser.parseInt32 x
        | _ -> None

    let asInt f = asInt32 f

    let asInt64 (f : FormValue) =
        match f with
        | FNumber x when floatInRange Int64.MinValue Int64.MaxValue x -> Some (Convert.ToInt64 x)
        | FString x -> StringParser.parseInt64 x
        | _ -> None

    let asFloat (f : FormValue) =
        match f with
        | FNumber x -> Some x
        | FString x -> StringParser.parseFloat x
        | _ -> None

    let asDecimal (f : FormValue) =
        match f with
        | FNumber x -> Some (decimal x)
        | FString x -> StringParser.parseDecimal x
        | _ -> None

    let asDateTime (f : FormValue) =
        match f with
        | FNumber n when floatInRange Int64.MinValue Int64.MaxValue n ->
            Some (epoch.AddMilliseconds(n))
        | FString s ->
            StringParser.parseDateTime s
        | _ -> None

    let asDateTimeOffset (f : FormValue) =
        match f with
        | FNumber n when floatInRange Int64.MinValue Int64.MaxValue n ->
            Some (DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64 n))
        | FString s ->
            StringParser.parseDateTimeOffset s
        | _ -> None

    let asTimeSpan (f : FormValue) =
        match f with
        | FString s -> StringParser.parseTimeSpan s
        | _ -> None

    let asGuid (f : FormValue) =
        match f with
        | FString s -> StringParser.parseGuid s
        | _ -> None

[<AutoOpen>]
module FormValueExtensions =
    open System.Runtime.CompilerServices

    let inline private convertOrFail
        (typeName : string)
        (ctor : FormValue -> 'a option)
        (f : FormValue): 'a =
        match ctor f with
        | Some s -> s
        | None   -> failwithf "%A is not an %s" f typeName

    [<Extension>]
    type FormValueExtensions() =
        [<Extension>]
        static member AsObject (f : FormValue) =
            match f with
            | FObject properties -> properties
            | _ -> []

        [<Extension>]
        static member AsList (f : FormValue) : FormValue list =
            match f with
            | FList a -> a
            | _ -> []

        [<Extension>]
        static member TryGet (this : FormValue, name : string) : FormValue option =
            match this with
            | FObject props ->
                props
                |> List.tryFind (fun (k, _) -> k = name)
                |> Option.map (fun (_, v) -> v)
            | _ -> None

        [<Extension>]
        static member Get (this : FormValue, name : string) : FormValue =
            match this.TryGet name with
            | Some prop -> prop
            | None -> failwithf "Property %s does not exist" name

        [<Extension>]
        static member AsString (f : FormValue) : string = 
            convertOrFail "string" FormValue.asString f

        [<Extension>]
        static member AsInt16 (f : FormValue) : Int16 = 
            convertOrFail "Int16" FormValue.asInt16 f

        [<Extension>]
        static member AsInt32 (f : FormValue) : Int32 = 
            convertOrFail "Int32" FormValue.asInt32 f

        [<Extension>]
        static member AsInt (f : FormValue) : int = 
            convertOrFail "Int" FormValue.asInt f

        [<Extension>]
        static member AsInt64 (f : FormValue) : Int64 = 
            convertOrFail "Int64" FormValue.asInt64 f

        [<Extension>]
        static member AsFloat (f : FormValue) : float = 
            convertOrFail "Float" FormValue.asFloat f

        [<Extension>]
        static member AsDecimal (f : FormValue) : Decimal = 
            convertOrFail "Decimal" FormValue.asDecimal f

        [<Extension>]
        static member AsDateTime (f : FormValue) : DateTime = 
            convertOrFail "DateTime" FormValue.asDateTime f

        [<Extension>]
        static member AsDateTimeOffset (f : FormValue) : DateTimeOffset = 
            convertOrFail "DateTimeOffset" FormValue.asDateTimeOffset f

        [<Extension>]
        static member AsTimeSpan (f : FormValue) : TimeSpan = 
            convertOrFail "TimeSpan" FormValue.asTimeSpan f

        [<Extension>]
        static member AsGuid (f : FormValue) : Guid = 
            convertOrFail "Guid" FormValue.asGuid f

    let (?) (form : FormValue) (name : string) : FormValue =
        form.Get name
