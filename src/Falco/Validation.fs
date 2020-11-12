module Falco.Validation

open System

type ValidationErrors = string list

type ValidationResult<'a> = Result<'a, ValidationErrors>

module ValidationResult = 
    let create cond success error : ValidationResult<'a> =
        if cond then Ok success
        else [error] |> Error

    let apply (resultFn : ValidationResult<'a -> 'b>) (result : ValidationResult<'a>) =
        match resultFn, result with
        | Ok fn, Ok x        -> fn x |> Ok
        | Error e, Ok _      -> Error e
        | Ok _, Error e      -> Error e
        | Error e1, Error e2 -> List.concat [e1;e2] |> Error

    let map (fn : 'a -> 'b) (result : ValidationResult<'a>) =
        match result with 
        | Ok x    -> fn x |> Ok
        | Error e -> Error e

    let mapError (errorFn : ValidationErrors -> 'b) (result : ValidationResult<'a>) =
        match result with
        | Ok x -> Ok x
        | Error e -> e |> errorFn |> Error

let (<*>) = ValidationResult.apply
let (<!>) = ValidationResult.map

module Validators =
    module private Messages =         
        let private makeSuffix (suffix : string option) =
            suffix |> Option.map (sprintf " %s") |> Option.defaultValue ""

        let required (fieldName : string) =
            sprintf "%s is required" fieldName

        let betweenSuffix (suffix : string option) (fieldName : string) (min : 'a) (max : 'a) =
            sprintf "%s must be between %A and %O %s" fieldName min max (makeSuffix suffix)

        let gteSuffix (suffix : string option) (fieldName : string) (min : 'a) =
            sprintf "%s must be greater than or equal to %A %s" fieldName min (makeSuffix suffix)

        let between (fieldName : string) (min : 'a) (max : 'a) =
            betweenSuffix None fieldName min max

        let gte (fieldName : string) (min : 'a) =
            gteSuffix None fieldName min

    let optional (fieldName : string) (value : 'a option) (validator : string -> 'a -> ValidationResult<'a>) : ValidationResult<'a option> =  
        match value with
        | Some v -> validator fieldName v |> Result.map (fun v -> Some v)
        | None   -> Ok value

    let required (fieldName : string) (value : 'a option) (validator : string -> 'a -> ValidationResult<'a>) : ValidationResult<'a> =  
        match value with
        | Some v -> validator fieldName v
        | None   -> Error [ Messages.required fieldName ]
    
    module Int = 
        let min (min : int) (fieldName : string) (value : int) =
            ValidationResult.create (value >= min) value (Messages.gte fieldName min)

        let range (min : int) (max : int) (fieldName : string) (value : int) =
            ValidationResult.create (value >= min && value <= max) value (Messages.between fieldName min max)

    module Int64 =
        let min (min : int64) (fieldName : string) (value : int64) =
            ValidationResult.create (value >= min) value (Messages.gte fieldName min)

    module String =
        let minLength (min : int) (fieldName : string) (value : string) =
            ValidationResult.create (value.Length >= min) value (Messages.gteSuffix (Some "characters") fieldName min)
