module AppName.Value

open System
open Falco
open Falco.Markup
open AppName.Domain
open AppName.Provider

module Model =    
    module AddNew =        
        // Errors
        type Error = AlreadyExists

        // Workflow (receiving dependencies)
        let handle (createValue : Value -> Result<unit, string>) =
            fun input ->
                match createValue input with
                | Ok _ -> Ok ()
                | Error _ -> Error AlreadyExists
                   
    module GetAll = 
        // Errors
        type Error = EmptyValues

        // Workflow (receiving dependencies)
        let handle (getValues : unit -> Value list) =            
            fun input ->
                match getValues input with
                | []     -> Error EmptyValues
                | values -> Ok values

module Controller =
    open Model

    /// HTTP POST /value/create
    let createSubmit : HttpHandler =        
        let handleValue value =
            match AddNew.handle (ValueProvider.add)value with 
            | Ok _ -> 
                Response.withStatusCode 204 
                >> Response.ofEmpty

            | _ ->                 
                Response.withStatusCode 409
                >> Response.ofJson { Code = 409; Message = [ "Already exsts" ] }

        let formBinder (form : FormCollectionReader) =
            { 
                Description = form.GetString "description" "" 
            }
        
        Request.mapForm formBinder handleValue
    
    /// HTTP GET /
    let index : HttpHandler = 
        fun ctx ->
            let values = 
                match GetAll.handle (ValueProvider.getAll) () with
                | Ok values -> values
                | _         -> []
            
            Response.ofJson values ctx