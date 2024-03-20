namespace Falco

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

module FormValue =
    open Falco

    let parseKeyValues (keyValues : (string * string seq) seq) : FormValue =
        let formValues = dict keyValues
        FormValueParser(formValues).Parse()

    let parse (keyValueString : string) : FormValue =
        let decoded = WebUtility.UrlDecode keyValueString
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

        formValuePairs
        |> Seq.map (fun kvp -> kvp.Key, kvp.Value :> IEnumerable<string>)
        |> parseKeyValues

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

    let inline private convertOrNone ctor formValue =
        match formValue with
        | FNull -> None
        | f -> ctor f

    let inline private convertOrDefault (ctor : FormValue -> 'a option) (defaultValue : 'a) formValue =
        match ctor formValue with
        | Some s -> s
        | None -> defaultValue

    [<Extension>]
    type FormValueExtensions() =
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
            | None -> FNull

        [<Extension>]
        static member AsObject formValue =
            match formValue with
            | FObject properties -> properties
            | _ -> []

        [<Extension>]
        static member AsList formValue =
            match formValue with
            | FList a -> a
            | _ -> []

        [<Extension>]
        static member AsString formValue = convertOrDefault FormValue.asString "" formValue

        [<Extension>]
        static member AsInt16 formValue = convertOrDefault FormValue.asInt16 0s formValue

        [<Extension>]
        static member AsInt32 formValue = convertOrDefault FormValue.asInt32 0 formValue

        [<Extension>]
        static member AsInt formValue = FormValueExtensions.AsInt32 formValue

        [<Extension>]
        static member AsInt64 formValue = convertOrDefault FormValue.asInt64 0L formValue

        [<Extension>]
        static member AsFloat formValue = convertOrDefault FormValue.asFloat 0. formValue

        [<Extension>]
        static member AsDecimal formValue = convertOrDefault FormValue.asDecimal 0.M formValue

        [<Extension>]
        static member AsDateTime formValue = convertOrDefault FormValue.asDateTime DateTime.MinValue formValue

        [<Extension>]
        static member AsDateTimeOffset formValue = convertOrDefault FormValue.asDateTimeOffset DateTimeOffset.MinValue formValue

        [<Extension>]
        static member AsTimeSpan formValue = convertOrDefault FormValue.asTimeSpan TimeSpan.MinValue formValue

        [<Extension>]
        static member AsGuid formValue = convertOrNone FormValue.asGuid formValue

        [<Extension>]
        static member AsStringOption formValue = convertOrNone FormValue.asString formValue

        [<Extension>]
        static member AsInt16Option formValue = convertOrNone FormValue.asInt16 formValue

        [<Extension>]
        static member AsInt32Option formValue = convertOrNone FormValue.asInt32 formValue

        [<Extension>]
        static member AsIntOption formValue = FormValueExtensions.AsInt32Option formValue

        [<Extension>]
        static member AsInt64Option formValue = convertOrNone FormValue.asInt64 formValue

        [<Extension>]
        static member AsFloatOption formValue = convertOrNone FormValue.asFloat formValue

        [<Extension>]
        static member AsDecimalOption formValue = convertOrNone FormValue.asDecimal formValue

        [<Extension>]
        static member AsDateTimeOption formValue = convertOrNone FormValue.asDateTime formValue

        [<Extension>]
        static member AsDateTimeOffsetOption formValue = convertOrNone FormValue.asDateTimeOffset formValue

        [<Extension>]
        static member AsTimeSpanOption formValue = convertOrNone FormValue.asTimeSpan formValue

        [<Extension>]
        static member AsGuidOption formValue = convertOrNone FormValue.asGuid formValue

    let inline (?) (formValue : FormValue) (name : string) =
        formValue.Get name

[<Sealed>]
type FormData(form : IFormCollection, files : IFormFileCollection option) =
    member _.Values =
        form
        |> Seq.map (fun kvp -> kvp.Key, Seq.cast kvp.Value)
        |> FormValue.parseKeyValues

    member _.TryGetFile(name : string) =
        match files, name with
        | _, IsNullOrWhiteSpace _
        | None, _ -> None
        | Some files, name ->
            match files.GetFile name with
            | f when isNull f -> None
            | f -> Some f
