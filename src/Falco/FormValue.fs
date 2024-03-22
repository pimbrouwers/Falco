namespace Falco

open System
open System.Collections.Generic
open System.Net
open Microsoft.FSharp.Core.Operators
open Falco.StringPatterns

type FormValue =
    | FNull
    | FBool of bool
    | FNumber of float
    | FString of string
    | FList of elements : FormValue list
    | FObject of properties : (string * FormValue) list

module FormValue =
    let tryGetBind name bind formValue =
        match formValue with
        | FObject props ->
            props
            |> List.tryFind (fun (k, _) -> String.Equals(k, name, StringComparison.OrdinalIgnoreCase))
            |> Option.bind (fun (_, v) -> bind v)
        | _ -> None

    let tryGet name formValue =
        tryGetBind name (id >> Some) formValue

    let get name formValue =
        match tryGet name formValue with
        | Some v -> v
        | None -> FNull

    let private epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)

    let inline private floatInRange min max (f : float) =
        let _min = float min
        let _max = float max
        f >= _min && f <= _max

    let asPairs formValue =
        match formValue with
        | FObject properties -> Some properties
        | _ -> None

    let asList formValue =
        match formValue with
        | FList a -> Some a
        | _ -> None

    let asString (f : FormValue) =
        match f with
        | FNull -> Some ""
        | FBool b -> Some (if b then "true" else "false")
        | FNumber n -> Some (string n)
        | FString s -> Some s
        | _ -> None

    let asStringNonEmpty (f : FormValue) =
        match f with
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

    let asBoolean (f : FormValue) =
        match f with
        | FBool x when x -> Some true
        | FBool x when not x -> Some false
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

    let tryBindList bind formValue =
        match formValue with
        | FList slist ->
            slist
            |> List.fold (fun (acc : List<'b>) sv ->
                // accumulate successful parses
                match bind sv with
                | Some b ->
                    acc.Add(b) |> ignore
                    acc
                | None -> acc) (List<'b>())
            |> List.ofSeq
        | _ -> []

    let asStringList (f : FormValue) =
        tryBindList asString f

    let asStringNonEmptyList (f : FormValue) =
        tryBindList asStringNonEmpty f

    let asInt16List (f : FormValue) =
        tryBindList asInt16 f

    let asInt32List (f : FormValue) =
        tryBindList asInt32 f

    let asIntList f =
        asInt32List f

    let asInt64List (f : FormValue) =
        tryBindList asInt64 f

    let asBooleanList (f : FormValue) =
        tryBindList asBoolean f

    let asFloatList (f : FormValue) =
        tryBindList asFloat f

    let asDecimalList (f : FormValue) =
        tryBindList asDecimal f

    let asDateTimeList (f : FormValue) =
        tryBindList asDateTime f

    let asDateTimeOffsetList (f : FormValue) =
        tryBindList asDateTimeOffset f

    let asTimeSpanList (f : FormValue) =
        tryBindList asTimeSpan f

    let asGuidList (f : FormValue) =
        tryBindList asGuid f

module FormValueParser =
    let rec private parseInternal (formValues : IDictionary<string, string seq>) =
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

    and private parseNested (acc : Dictionary<string, FormValue>) (keys : string list) (values : string seq) =
        match keys with
        | [] -> ()
        | [IsListKey key] ->
            // list of primitives
            values
            |> parseFormPrimitiveList
            |> fun x -> acc.TryAdd(key, x) |> ignore

        | [IsIndexedListKey (index, key)] ->
            if acc.ContainsKey key then
                match acc[key] with
                | FList formList ->
                    let lstAccLen = if index >= formList.Length then index + 1 else formList.Length
                    let lstAcc : FormValue array = Array.zeroCreate (lstAccLen)
                    for i = 0 to lstAccLen - 1 do
                        let lstFormValue =
                            if i <> index then
                                match List.tryItem i formList with
                                | Some x -> x
                                | None -> FNull
                            else
                                parseFormPrimitiveSingle values

                        lstAcc[i] <- lstFormValue

                    acc[key] <- FList (List.ofArray lstAcc)
                | _ -> ()
            elif index = 0 then
                acc.TryAdd(key, FList [ parseFormPrimitiveSingle values ]) |> ignore
            else
                let lstAcc : FormValue array = Array.zeroCreate (index + 1)
                for i = 0 to index do
                    lstAcc[i] <- if i <> index then FNull else parseFormPrimitiveSingle values
                acc.TryAdd(key, FList (List.ofArray lstAcc)) |> ignore

        | [key] ->
            // primitive
            values
            |> parseFormPrimitiveSingle
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

    and private parseFormPrimitive (x : string) =
        let decoded = WebUtility.UrlDecode x
        match decoded with
        | IsNullOrWhiteSpace _ -> FNull
        | IsTrue x
        | IsFalse x -> FBool x
        | IsFloat x -> FNumber x
        | x -> FString x

    and private parseFormPrimitiveList values =
        values
        |> Seq.map parseFormPrimitive
        |> List.ofSeq
        |> FList

    and private parseFormPrimitiveSingle values =
        values
        |> Seq.tryHead
        |> Option.map parseFormPrimitive
        |> Option.defaultValue FNull

    and private (|IsListKey|_|) (x : string) =
        if x.EndsWith("[]") then Some (x.Substring(0, x.Length - 2))
        else None

    and private (|IsIndexedListKey|_|) (x : string) =
        if x.EndsWith("]") then
            match Text.RegularExpressions.Regex.Match(x, @".\[(\d+)\]$") with
            | m when Seq.length m.Groups = 2 ->
                let capture = m.Groups[1].Value
                Some (int capture, x.Substring(0, x.Length - capture.Length - 2))
            | _ -> None
        else None

    and private newFormAcc () =
        Dictionary<string, FormValue>()

    and private formAccToValues (x : Dictionary<string, FormValue>) =
        x |> Seq.map (fun (kvp) -> kvp.Key, kvp.Value) |> List.ofSeq |> FObject

    and private formValuesToAcc (x : (string * FormValue) list) =
        let acc = newFormAcc()
        for (key, value) in x do
            acc.TryAdd(key, value) |> ignore
        acc

    let parseKeyValues (keyValues : (string * string seq) seq) : FormValue =
        let formValues = dict keyValues
        parseInternal formValues

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
        static member TryGetMap (this : FormValue, name : string, bind) : 'a option =
            FormValue.tryGetBind name bind this

        [<Extension>]
        static member TryGet (this : FormValue, name : string) : FormValue option =
            FormValue.tryGet name this

        [<Extension>]
        static member Get (this : FormValue, name : string) : FormValue =
            FormValue.get name this

        [<Extension>]
        static member AsObject formValue =
            convertOrDefault FormValue.asPairs [] formValue

        [<Extension>]
        static member AsList formValue =
            convertOrDefault FormValue.asList [] formValue

        [<Extension>]
        static member AsString formValue =
            convertOrDefault FormValue.asString "" formValue

        [<Extension>]
        static member AsInt16 formValue =
            convertOrDefault FormValue.asInt16 0s formValue

        [<Extension>]
        static member AsInt32 formValue =
            convertOrDefault FormValue.asInt32 0 formValue

        [<Extension>]
        static member AsInt formValue =
            FormValueExtensions.AsInt32 formValue

        [<Extension>]
        static member AsInt64 formValue =
            convertOrDefault FormValue.asInt64 0L formValue

        [<Extension>]
        static member AsBoolean formValue =
            convertOrDefault FormValue.asBoolean false formValue

        [<Extension>]
        static member AsFloat formValue =
            convertOrDefault FormValue.asFloat 0. formValue

        [<Extension>]
        static member AsDecimal formValue =
            convertOrDefault FormValue.asDecimal 0.M formValue

        [<Extension>]
        static member AsDateTime formValue =
            convertOrDefault FormValue.asDateTime DateTime.MinValue formValue

        [<Extension>]
        static member AsDateTimeOffset formValue =
            convertOrDefault FormValue.asDateTimeOffset DateTimeOffset.MinValue formValue

        [<Extension>]
        static member AsTimeSpan formValue =
            convertOrDefault FormValue.asTimeSpan TimeSpan.MinValue formValue

        [<Extension>]
        static member AsGuid formValue =
            convertOrNone FormValue.asGuid formValue

        [<Extension>]
        static member AsStringOption formValue =
            convertOrNone FormValue.asString formValue

        [<Extension>]
        static member AsInt16Option formValue =
            convertOrNone FormValue.asInt16 formValue

        [<Extension>]
        static member AsInt32Option formValue =
            convertOrNone FormValue.asInt32 formValue

        [<Extension>]
        static member AsIntOption formValue =
            FormValueExtensions.AsInt32Option formValue

        [<Extension>]
        static member AsInt64Option formValue =
            convertOrNone FormValue.asInt64 formValue

        [<Extension>]
        static member AsBooleanOption formValue =
            convertOrNone FormValue.asBoolean formValue

        [<Extension>]
        static member AsFloatOption formValue =
            convertOrNone FormValue.asFloat formValue

        [<Extension>]
        static member AsDecimalOption formValue =
            convertOrNone FormValue.asDecimal formValue

        [<Extension>]
        static member AsDateTimeOption formValue =
            convertOrNone FormValue.asDateTime formValue

        [<Extension>]
        static member AsDateTimeOffsetOption formValue =
            convertOrNone FormValue.asDateTimeOffset formValue

        [<Extension>]
        static member AsTimeSpanOption formValue =
            convertOrNone FormValue.asTimeSpan formValue

        [<Extension>]
        static member AsGuidOption formValue =
            convertOrNone FormValue.asGuid formValue

    let inline (?) (formValue : FormValue) (name : string) =
        formValue.Get name