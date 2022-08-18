namespace AuthExample

open System

// ------------
// Entities
// ------------
type User =
    { Id        : string
      Username  : string
      Name      : string
      Surname   : string }

type UserDto =
    { Username  : string
      Name      : string
      Surname   : string }

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

type MemoryStorage() =
    let mutable values = [
        { Id = "d19bc3e4-4b72-488b-a739-df812bd892c9"; Username = "user1"; Name = "John"; Surname = "Doe" }
        { Id = "11beebd6-6a0b-42f7-bf70-56b168cdd55c"; Username = "user2"; Name = "Mario"; Surname = "Rossi" }
        { Id = "096527f3-ceed-4d27-bcd9-d4c1da7798ab"; Username = "user3"; Name = "Stephen"; Surname = "Knight" }
    ]
    interface IStorage with
        member _.GetAll() =
            values |> Seq.map id |> Result.Ok
        member _.Add(id : string) (userDto : UserDto) =
            let user = { Id = id; Username = userDto.Username; Name = userDto.Name; Surname = userDto.Surname }
            values <- List.append values [user]
            Result.Ok user
        member _.Update(id: string) (userDto: UserDto) =
            let user = { Id = id; Username = userDto.Username; Name = userDto.Name; Surname = userDto.Surname }
            values <- values |> List.map (fun u -> if u.Id = id then user else u)
            Result.Ok user
        member _.Remove(id: string) =
            let user = values |> List.find (fun u -> u.Id = id)
            values <- values |> List.filter (fun u -> u.Id <> id)
            Result.Ok user

// ------------
// User Storage
// ------------
module UserStorage =
    let getAll (storage : IStorage) () =
        storage.GetAll()

    let create (storage : IStorage) (userDto : UserDto) =
        let id = Guid.NewGuid().ToString()
        storage.Add id userDto

    let update (storage : IStorage) (id : string) (userDto : UserDto) =
        let checkUserExist users =
            users
            |> Seq.tryFind (fun user -> user.Id = id)
            |> function
                | Some user -> Result.Ok user
                | None      -> Result.Error { Code = "123"; Message = "User to update not found!" }

        storage.GetAll()
        |> Result.bind checkUserExist
        |> Result.bind (fun _ -> storage.Update id userDto)

    let delete (storage : IStorage) (id : string) =
        let checkUserExist users =
            users
            |> Seq.tryFind (fun user -> user.Id = id)
            |> function
                | Some user -> Result.Ok user
                | None      -> Result.Error { Code = "456"; Message = "User to delete not found!" }

        storage.GetAll()
        |> Result.bind checkUserExist
        |> Result.bind (fun _ -> storage.Remove id)