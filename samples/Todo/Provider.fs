module Todo.Provider

open System.Collections.Generic
open System.IO
open AppName.Domain

type ProviderResult<'a> = ProviderOk of 'a | ProviderError of string

module TodoProvider = 
    let private store : Dictionary<string, Todo> = new Dictionary<string, Todo>()

    let add (newTodo : NewTodo) : ProviderResult<unit> = 
        let newId = Path.GetRandomFileName().Replace(".", "")
        let todo = 
            { 
                TodoId = newId
                Description = newTodo.Description
                Completed = false 
            }

        store.Add(newId, todo)
        ProviderOk ()

    let getAll () : Todo list = Seq.toList store.Values

    let updateStatus (todoStatus : TodoStatusUpdate) : ProviderResult<unit> =
        match store.ContainsKey todoStatus.TodoId with
        | true  -> 
            let key = todoStatus.TodoId
            let updatedTodo = { store.[key] with Completed = todoStatus.Completed }
            store.[key] <- updatedTodo
            ProviderOk ()
        | false -> 
            ProviderError "Invalid Todo ID"