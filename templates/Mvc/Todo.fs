module AppName.Todo

open Falco
open Falco.Markup
open AppName.Common
open AppName.Domain

module Model = 
    module GetAll = 
        type Error = EmptyTodos

        let handle (getTodos : unit -> Todo list) : ServiceQuery<Todo list, Error> =
            fun _ -> 
                match getTodos () with
                | []     -> Error EmptyTodos 
                | todos  -> Ok todos
    
module View =    
    open AppName.UI

    let index todos errors = 
        let todoElems = 
            todos
            |> List.map (fun todo -> 
                Elem.div [] [ 
                    Elem.h2 [] [ Text.raw todo.Description ]
                ])
        [
            Elem.h1  [] [ Text.raw "Todos" ]
            Components.errorSummary errors
            Elem.div [] todoElems
        ]
        |> Layouts.master "Todo Index"

module Controller =
    open Model 
    let index : HttpHandler = 
        let handleError error = 
            let errors = 
                match error with 
                | GetAll.EmptyTodos -> [ "Currently, there are no todos" ]

            View.index [] errors
            |> Response.ofHtml

        let handleOk todos = 
            View.index todos []
            |> Response.ofHtml 

        runService
            (GetAll.handle (Provider.Todo.getAll))
            handleOk
            handleError
            ()