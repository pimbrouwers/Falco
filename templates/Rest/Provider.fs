module AppName.Provider

open System.Collections.Generic
open AppName.Domain

module ValueProvider = 
    let private store : HashSet<Value> = new HashSet<Value>()

    let add (value : Value) : Result<unit, string> =         
        match store.Contains value with
        | true -> Error "Already exists"
        | false -> 
            store.Add(value) |> ignore
            Ok ()

    let getAll () : Value list = 
        Seq.toList store   