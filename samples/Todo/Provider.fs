module Todo.Provider

open System.Collections.Generic
open System.IO
open Todo.Domain

type ProviderResult<'a> = Result<'a, string>

module TodoProvider = 
    let private store : Dictionary<string, Todo> = new Dictionary<string, Todo>()

    type Add = NewTodo -> ProviderResult<unit>
    let add (newTodo : NewTodo) : ProviderResult<unit> = 
        let newId = Path.GetRandomFileName().Replace(".", "")
        let todo = 
            { 
                TodoId = newId
                Description = newTodo.Description
                Completed = false 
            }

        store.Add(newId, todo)
        Ok ()

    type GetAll = unit -> Todo seq
    let getAll () : Todo list = Seq.toList store.Values

    type Update = TodoStatusUpdate -> ProviderResult<unit>
    let updateStatus (todoStatus : TodoStatusUpdate) : ProviderResult<unit> =
        match store.ContainsKey todoStatus.TodoId with
        | true  -> 
            let key = todoStatus.TodoId
            let updatedTodo = { store.[key] with Completed = todoStatus.Completed }
            store.[key] <- updatedTodo
            Ok ()
        | false -> 
            Error "Invalid Todo ID"