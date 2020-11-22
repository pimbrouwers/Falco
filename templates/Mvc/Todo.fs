module AppName.Todo

open System
open Falco
open Falco.Markup
open Falco.Security
open Microsoft.AspNetCore.Antiforgery
open AppName.Common
open AppName.Domain

module Model =
    type TodoSummary = 
        {
            Completed : Todo list
            Pending   : Todo list
        }
        static member Empty = { Completed = []; Pending = [] }

    module AddNew =
        // Input
        type Input = 
            { 
                Description : string 
            }
            static member Empty = { Description = String.Empty }
        
        // Errors
        type Error = InvalidInput of Input * string list

        // Steps         
        let validateInput (input : Input) : Result<NewTodo, Error> =
            if String.IsNullOrWhiteSpace(input.Description) then
                InvalidInput (input, ["Must provide description"]) 
                |> Error
            else 
                Ok { Description = input.Description }
        
        // Workflow
        let handle (createTodo : NewTodo -> unit) : ServiceCommand<Input, Error> =
            fun input ->
                input
                |> validateInput
                |> Result.bind (createTodo >> Ok)

    module ChangeStatus =
        // Input
        type Input =
            {
                Index    : int
                Complete : bool
            }

        // Errors
        type Error = 
            | InvalidInput of string list
            | NonExistentTodo
            
    module GetAll = 
        // Errors
        type Error = EmptyTodos

        // Steps
        let mapResult (todos : Todo list) : TodoSummary =
            let completed = 
                todos
                |> List.filter (fun t -> t.Completed = true)

            let pending = 
                todos 
                |> List.filter (fun t -> t.Completed = false)

            {
                Completed = completed 
                Pending   = pending
            }

        // Workflow
        let handle (getTodos : unit -> Todo list) : ServiceQuery<TodoSummary, Error> =
            fun _ ->                 
                match getTodos () with
                | []     -> Error EmptyTodos 
                | todos  -> Ok (todos |> mapResult)
                
module View =    
    open AppName.UI
    open Model

    let create (input : AddNew.Input) (errors : string list) (token : AntiforgeryTokenSet) =
        [
            Components.pageTitle "New Todo"
            Elem.form [ Attr.method "post" ] [
                Components.errorSummary errors

                Forms.label "description" "Description"
                Forms.inputText "description" input.Description [ Attr.placeholder "Ex: mow the lawn" ]                

                Xss.antiforgeryInput token
                Forms.submit None "Submit"
            ]
            Elem.br []
            Elem.a [ Attr.href Urls.``/`` ] [ Text.raw "Cancel" ]
        ]
        |> Layouts.master "Todo Create"

    let index todos errors = 
        // partial view for Todos
        let todoElems actionUrl actionLabel todos = 
            todos
            |> List.map (fun todo -> 
                Elem.div [ Attr.class' "mb3 pa2 ba b--black-20" ] [ 
                    Elem.div [] [ Text.raw todo.Description ]
                    Elem.form [ Attr.class' "tr" ] [ 
                        Forms.inputHidden "index" (todo.TodoId) []
                        Forms.submit None actionLabel                        
                    ]                    
                ])

        [           
            Components.pageTitle "My Todos"
            Elem.a [ Attr.href Urls.``/todo/create`` ] [ Text.raw "Create New" ]
            
            Components.errorSummary errors

            if not(List.isEmpty todos.Pending) then
                Components.subTitle "Pending"
                Elem.div [] (todoElems Urls.``/todo/complete/{index:int}`` "Mark Completed" todos.Pending)

            if not(List.isEmpty todos.Completed) then
                Components.subTitle "Completed"
                Elem.div [] (todoElems Urls.``/todo/incomplete/{index:int}`` "Mark Incomplete" todos.Completed)
        ]
        |> Layouts.master "Todo Index"

module Controller =
    open Model 
    let create : HttpHandler =
        View.create AddNew.Input.Empty []
        |> Response.ofHtmlCsrf

    let createSubmit : HttpHandler =
        let handleError error =            
            match error with
            | AddNew.InvalidInput (input, e) -> View.create input e
            |> Response.ofHtmlCsrf

        let handleOk () =
            Response.redirect Urls.``/`` false

        let handleService input = 
            Service.run
                (AddNew.handle (Provider.Todo.add))
                handleOk
                handleError
                input

        let formBinder (form : FormCollectionReader) : AddNew.Input =
            { Description = form.GetString "description" "" }
        
        Request.mapFormSecure
            formBinder
            handleService
            Handlers.invalidCsrfToken
        
    let index : HttpHandler = 
        let handleError error = 
            let errors = 
                match error with 
                | GetAll.EmptyTodos -> [ "Currently, there are no todos" ]

            View.index TodoSummary.Empty errors
            |> Response.ofHtml

        let handleOk todos = 
            View.index todos []
            |> Response.ofHtml 

        Service.run
            (GetAll.handle (Provider.Todo.getAll))
            handleOk
            handleError
            ()