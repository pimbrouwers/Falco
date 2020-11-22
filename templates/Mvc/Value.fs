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
                
module View =    
    let create value errors =
        [
            UI.Common.pageTitle "New Value"
            UI.Common.errorSummary errors

            Elem.form [ Attr.method "post" ] [
                UI.Forms.label "description" "Description"
                UI.Forms.inputText "description" value.Description [ Attr.placeholder "Ex: Value 1" ]                

                UI.Forms.submit "Submit"
            ]

            Elem.br []
            Elem.a [ Attr.href Urls.``/`` ] [ Text.raw "Cancel" ]
        ]
        |> UI.Layouts.master "Value Create"

    let index values errors = 
        // partial view for values
        let valueElems values = 
            Elem.div [] 
                (values |> List.map (fun value -> 
                        Elem.div [ Attr.class' "mb3" ] [ Text.raw value.Description ]))

        [           
            UI.Common.pageTitle "Values"            
            UI.Common.errorSummary errors

            Elem.a [ Attr.class' "db mb3"; Attr.href Urls.``/value/create`` ] [ Text.raw "Create New" ]

            valueElems values
        ]
        |> UI.Layouts.master "Value Index"

module Controller =
    open Model

    /// HTTP GET /value/create
    let create : HttpHandler =
        View.create Value.Empty []
        |> Response.ofHtml

    /// HTTP POST /value/create
    let createSubmit : HttpHandler =        
        let handleValue value =
            match AddNew.handle (ValueProvider.add)value with 
            | Ok _ -> 
                Response.redirect Urls.``/`` false

            | _ -> 
                View.create value [ "Already exists" ] 
                |> Response.ofHtml

        let formBinder (form : FormCollectionReader) =
            { 
                Description = form.GetString "description" "" 
            }
        
        Request.mapForm formBinder handleValue
    
    /// HTTP GET /
    let index : HttpHandler = 
        fun ctx ->
            let view = 
                match GetAll.handle (ValueProvider.getAll) () with
                | Ok values ->
                    View.index values []
                | _ ->
                    View.index [] [ "Empty values" ]
            
            Response.ofHtml view ctx