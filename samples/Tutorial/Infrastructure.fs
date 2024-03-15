namespace FalcoTutorial.Infrastructure

[<AutoOpen>]
module DbExtensions = 
    open System
    open System.Data
    
    type IDbConnection with
        member x.CreateCommandText(commandText : string) =
            let cmd = x.CreateCommand()
            cmd.CommandType <- CommandType.Text
            cmd.CommandText <- commandText
            cmd

    type IDbCommand with
        member x.AddParameter(name : string, value : obj) : unit =
            let p = x.CreateParameter()
            p.ParameterName <- name
            if isNull value then p.Value <- DBNull.Value
            else p.Value <- value
            x.Parameters.Add p |> ignore

        member x.Execute() : unit =
            x.ExecuteNonQuery() |> ignore

        member x.Query(map : IDataRecord -> 'a) : 'a list =
            use rd = x.ExecuteReader()
            [ while rd.Read() do
                yield map rd ]

[<AutoOpen>]
module Abstractions = 
    open System.Data
    open Microsoft.Data.Sqlite

    type IDbConnectionContext = 
        abstract member ConnectionString : string

    type IDbConnectionFactory =
        abstract member Create : unit -> IDbConnection

    type SqliteDbConnectionFactory(ctx : IDbConnectionContext) =
        interface IDbConnectionFactory with
            member _.Create() = 
                let conn = new SqliteConnection(ctx.ConnectionString)
                conn.Open()
                conn

module EntryStore =
    open System
    open System.Data
    open FalcoTutorial.Domain.EntryModel

    let save (conn : IDbConnection) (entry : Entry) : unit =
        use cmd = conn.CreateCommandText("
            INSERT OR REPLACE INTO entry (entry_id, html_content, text_content, modified_date)
            VALUES (@entry_id, @html_content, @text_content, DATETIME('now'));")

        cmd.AddParameter ("entry_id", entry.EntryId)
        cmd.AddParameter ("html_content", entry.HtmlContent)
        cmd.AddParameter ("text_content", entry.TextContent)

        cmd.Execute()

    let get (conn : IDbConnection) (entryId : Guid) : Entry option =
        use cmd = conn.CreateCommandText("
            SELECT    entry_id
                    , html_content
                    , text_content
            FROM      entry
            WHERE     entry_id = @entry_id")

        cmd.AddParameter ("entry_id", entryId)

        cmd.Query (fun rd ->
            { EntryId = rd.GetGuid(rd.GetOrdinal("entry_id"))
              HtmlContent = rd.GetString(rd.GetOrdinal("html_content"))
              TextContent = rd.GetString(rd.GetOrdinal("text_content")) })
        |> Seq.tryHead

    let getAll (conn : IDbConnection) : EntrySummary list =
        use cmd = conn.CreateCommandText("
            SELECT    entry_id
                    , modified_date AS entry_date
                    , SUBSTR(text_content, 0, 50) AS summary
            FROM      entry
            ORDER BY  modified_date DESC")

        cmd.Query (fun rd ->
            { EntryId = rd.GetGuid(rd.GetOrdinal("entry_id"))
              EntryDate = rd.GetDateTime(rd.GetOrdinal("entry_date"))
              Summary = rd.GetString(rd.GetOrdinal("summary")) })
