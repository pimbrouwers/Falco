module AuthExample.Domain

open System

// ------------
// Entities 
// ------------
type User = 
    { Id        : string;
      Username  : string;
      Name      : string;
      Surname   : string; }
      
type UserDto = 
    { Username  : string;
      Name      : string;
      Surname   : string; }

type Error =
    { Code      : string
      Message   : string }

// ------------
// Repositories 
// ------------
type IStorage =
    abstract member GetAll : unit   -> Result<User seq, Error>
    abstract member Add    : string -> UserDto -> Result<User, Error>
    abstract member Update : string -> UserDto -> Result<User, Error>
    abstract member Remove : string -> Result<User, Error>

// ------------
// Interactors 
// ------------
let getAllUsers (storage : IStorage) () =
    storage.GetAll()

let createUser (storage : IStorage) (userDto : UserDto) =
    let id = Guid.NewGuid().ToString()
    storage.Add id userDto

let updateUser (storage : IStorage) (id : string) (userDto : UserDto) =
    let checkUserExist users =
        users
        |> Seq.tryFind (fun user -> user.Id = id)
        |> function
            | Some user -> Result.Ok user
            | None      -> Result.Error { Code = "123"; Message = "User to update not found!" }
    storage.GetAll()
    |> Result.bind checkUserExist
    |> Result.bind (fun _ -> storage.Update id userDto)

let deleteUser (storage : IStorage) (id : string) =
    let checkUserExist users =
        users
        |> Seq.tryFind (fun user -> user.Id = id)
        |> function
            | Some user -> Result.Ok user
            | None      -> Result.Error { Code = "456"; Message = "User to delete not found!" }
    storage.GetAll()
    |> Result.bind checkUserExist
    |> Result.bind (fun _ -> storage.Remove id)