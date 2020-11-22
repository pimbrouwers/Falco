module AppName.Domain

open System

type Value = 
    {        
        Description : string                
    }
    static member Empty = { Description = String.Empty }