module AppName.Domain

type NewTodo = 
    {
        Description : string        
    }

type Todo = 
    {
        TodoId      : string
        Description : string        
        Completed   : bool
    }

type TodoStatusUpdate =
    {
        TodoId    : string
        Completed : bool
    }