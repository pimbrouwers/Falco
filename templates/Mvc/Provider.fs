module AppName.Provider

open System.IO
open AppName.Domain

module Todo = 
    let private newId () = Path.GetRandomFileName().Replace(".", "")
    let private store : ResizeArray<Todo> = ResizeArray<Todo>()

    let add (newTodo : NewTodo) : unit = 
        let todo = 
            { 
                TodoId = newId ()
                Description = newTodo.Description
                Completed = false 
            }

        store.Add(todo)

    let getAll () : Todo list = Seq.toList store