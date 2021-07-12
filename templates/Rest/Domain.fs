module AppName.Domain

open System

type Value = { Description : string } with
    static member Empty = { Description = String.Empty }