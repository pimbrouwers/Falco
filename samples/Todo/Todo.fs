module Todo.Todo

open System
open Falco
open Falco.Markup
open Falco.Security
open Microsoft.AspNetCore.Antiforgery
open Todo.Common.Urls
open Todo.Domain
open Todo.Provider
open Todo.Service

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
        type Error = 
            | InvalidInput of Input * string list
            | UnexpectedError

        // Steps         
        let validateInput (input : Input) : Result<NewTodo, Error> =            
            if String.IsNullOrWhiteSpace(input.Description) then
                Error (InvalidInput (input, ["Must provide description"]))
            else 
                Ok { Description = input.Description }

        let addNewTodo (createTodo : NewTodo -> ProviderResult<unit>) (newTodo : NewTodo) : Result<unit, Error> =
            match createTodo newTodo with
            | Ok result -> Ok result
            | Error _   -> Error UnexpectedError
        
        // Workflow (receiving dependencies)
        let handle (createTodo : NewTodo -> ProviderResult<unit>) : ServiceCommand<Input, Error> =
            fun input ->
                input
                |> validateInput
                |> Result.bind (addNewTodo createTodo)

    module ChangeStatus =
        // Input
        type InputAction = MarkComplete | MarkIncomplete
        type Input =
            {
                TodoId : string
                Action : InputAction
            }

        // Errors
        type Error = 
            | InvalidInput of Input * string list
            | NonExistentTodo

        // Steps
        let validateInput (input : Input) : Result<TodoStatusUpdate, Error> =            
            if StringUtils.strEmpty input.TodoId then
                Error (InvalidInput (input, ["Invalid ID"]))
            else 
                let completed = 
                    match input.Action with 
                    | MarkComplete   -> true 
                    | MarkIncomplete -> false

                Ok { 
                    TodoId = input.TodoId
                    Completed = completed 
                }

        let changeTodoStatus 
            (updateTodoStatus : TodoStatusUpdate -> ProviderResult<unit>) 
            (todoStatus : TodoStatusUpdate) : Result<unit, Error> =
            match updateTodoStatus todoStatus with
            | Ok _    -> Ok ()
            | Error _ -> Error NonExistentTodo
                
        // Workflow (receiving dependencies)
        let handle (updateTodoStatus : TodoStatusUpdate -> ProviderResult<unit>) : ServiceCommand<Input, Error> =
            fun input ->
                input
                |> validateInput
                |> Result.bind (changeTodoStatus updateTodoStatus)
            
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
        let handle (getTodos : unit -> Todo list) : ServiceHandler<unit, TodoSummary, Error> =
            fun () ->                 
                match getTodos () with
                | []     -> Error EmptyTodos 
                | todos  -> Ok (todos |> mapResult)
                
module View =    
    open Model

    let create (input : AddNew.Input) (errors : string list) (token : AntiforgeryTokenSet) =
        [
            UI.Common.pageTitle "New Todo"
            Elem.form [ Attr.method "post" ] [
                UI.Common.errorSummary errors

                UI.Forms.label "description" "Description"
                UI.Forms.inputText "description" input.Description [ Attr.placeholder "Ex: mow the lawn" ]                

                Xss.antiforgeryInput token
                UI.Forms.submit None "Submit"
            ]
            Elem.br []
            Elem.a [ Attr.href ``/`` ] [ Text.raw "Cancel" ]
        ]
        |> UI.Layouts.master "Todo Create"

    let index todos errors = 
        // partial view for Todos
        let todoElems actionUrl actionLabel todos = 
            todos
            |> List.map (fun todo -> 
                Elem.div [ Attr.class' "mb3 pa2 ba b--black-20" ] [ 
                    Elem.div [] [ Text.raw todo.Description ]
                    Elem.a   [ Attr.class' "db tr"; Attr.href (actionUrl todo.TodoId) ] [ Text.raw actionLabel ]                                                         
                ])

        [           
            UI.Common.pageTitle "My Todos"
            Elem.a [ Attr.href ``/todo/create`` ] [ Text.raw "Create New" ]
            
            UI.Common.errorSummary errors

            if not(List.isEmpty todos.Pending) then
                UI.Common.subTitle "Pending"
                Elem.div [] (todoElems ``/todo/complete/{id}`` "Mark Completed" todos.Pending)

            if not(List.isEmpty todos.Completed) then
                UI.Common.subTitle "Completed"
                Elem.div [] (todoElems ``/todo/incomplete/{id}`` "Mark Incomplete" todos.Completed)
        ]
        |> UI.Layouts.master "Todo Index"

module Controller =
    open Model 
    
    /// HTTP POST /todo/change-status/{id}?complete={true|false}
    let changeStatusSubmit : HttpHandler =
        Response.ofEmpty

    /// HTTP GET /todo/create
    let create : HttpHandler =
        View.create AddNew.Input.Empty []
        |> Response.ofHtmlCsrf

    /// HTTP POST /todo/create
    let createSubmit : HttpHandler =
        let handleError error =            
            match error with
            | AddNew.InvalidInput (input, e) -> View.create input e
            | AddNew.UnexpectedError         -> View.create AddNew.Input.Empty [ "Unexpected error occurred" ]
            |> Response.ofHtmlCsrf

        let handleOk () =
            Response.redirectTemporarily ``/``

        let handleService input = 
            Service.run
                (AddNew.handle (Provider.TodoProvider.add))
                handleOk
                handleError
                input

        let formBinder (form : FormCollectionReader) : AddNew.Input =
            { 
                Description = form.GetString "description" "" 
            }
        
        Request.mapFormSecure
            formBinder
            handleService
            Error.invalidCsrfToken
    
    /// HTTP GET /
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
            (GetAll.handle (Provider.TodoProvider.getAll))
            handleOk
            handleError
            ()