module AppName.Provider

open AppName.Domain

module Todo = 
    let store : ResizeArray<Todo> = ResizeArray<Todo>()

    let getAll () : Todo list = Seq.toList store

    let add (todo : Todo) : unit = store.Add(todo)