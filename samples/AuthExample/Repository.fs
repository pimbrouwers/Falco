module AuthExample.Repository

open AuthExample.Domain

type MemoryStorage() =
    let mutable values = [
        { Id = "d19bc3e4-4b72-488b-a739-df812bd892c9"; Username = "user1"; Name = "John"; Surname = "Doe" }
        { Id = "11beebd6-6a0b-42f7-bf70-56b168cdd55c"; Username = "user2"; Name = "Mario"; Surname = "Rossi" }
        { Id = "096527f3-ceed-4d27-bcd9-d4c1da7798ab"; Username = "user3"; Name = "Stephen"; Surname = "Knight" }
    ]
    interface IStorage with
        member _.GetAll() = 
            values |> Seq.map id |> Ok
        member _.Add(id : string) (userDto : UserDto) = 
            let user = { Id = id; Username = userDto.Username; Name = userDto.Name; Surname = userDto.Surname }
            values <- List.append values [user]
            Ok user
        member _.Update(id: string) (userDto: UserDto) = 
            let user = { Id = id; Username = userDto.Username; Name = userDto.Name; Surname = userDto.Surname }
            values <- values |> List.map (fun u -> if u.Id = id then user else u)
            Ok user
        member _.Remove(id: string) = 
            let user = values |> List.find (fun u -> u.Id = id)
            values <- values |> List.filter (fun u -> u.Id <> id)
            Ok user

