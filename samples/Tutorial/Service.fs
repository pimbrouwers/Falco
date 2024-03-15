namespace FalcoTutorial.Service

module EntryService =
    open System
    open FalcoTutorial.Infrastructure
    open FalcoTutorial.Domain.EntryModel

    let getAll (dbConnection : IDbConnectionFactory) =
        use conn = dbConnection.Create ()
        EntryStore.getAll conn

    let get (dbConnection : IDbConnectionFactory) (id : Guid) =
        use conn = dbConnection.Create ()
        EntryStore.get conn id

    let save (dbConnection : IDbConnectionFactory) (input : Entry) =
        use conn = dbConnection.Create ()
        EntryStore.save conn input
