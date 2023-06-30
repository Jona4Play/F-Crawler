module Postgres

open Npgsql
open NpgsqlTypes
open Npgsql.FSharp
open DataStructures
open Newtonsoft.Json
open System.Diagnostics


let insertquery = "INSERT INTO maindata (url,responsetime,emails,csslinks,jslinks,crawldate,links,htmlstring,metatags,imagedata) VALUES (@url,@responsetime,@emails,@csslinks,@jslinks,@crawldate,@links,@htmlstring,@metatags,@imagedata)"

//let pushdata(docs: Document array, connectionString:string) : int =
//        async {
//            use connection = new NpgsqlConnection(connectionString)
//            use command = new NpgsqlCommand(insertquery, connection)
//            connection.OpenAsync() |> Async.AwaitTask |> ignore
//            command.PrepareAsync() |> Async.AwaitTask |> ignore

//            return docs |> Array.map (fun doc ->
//                connectionString
//                |> Sql.connect
//                |> Sql.query insertquery
//                |> Sql.parameters
//                    [
//                        ("@url", Sql.string doc.URL)
//                        ("@responsetime", Sql.int64 doc.ResponseTime)
//                        ("@emails", Sql.stringArray <| Array.ofList doc.Emails)
//                        ("@csslinks", Sql.stringArray <| Array.ofList doc.CSSlinks)
//                        ("@jslinks", Sql.stringArray <| Array.ofList doc.JSlinks)
//                        ("@crawldate", Sql.date doc.CrawlDate)
//                        ("@links", Sql.stringArray <| Array.ofList doc.Links)
//                        ("@htmlstring", Sql.string doc.HTMLstring)
//                        ("@metatags", Sql.stringArray <| Array.ofList doc.Metatags)
//                        ("@imagedata", Sql.jsonb <| JsonConvert.SerializeObject doc.Imagedata)
//                    ]
//                |> Sql.executeNonQuery)
//        } |> Async.RunSynchronously |> Array.sum



let pushdata(docs: Document array, connectionString:string) : uint64 =
    async {
        use connection = new NpgsqlConnection(connectionString)
        connection.OpenAsync() |> ignore
        use writer = connection.BeginBinaryImport("COPY maindata (url,responsetime,emails,csslinks,jslinks,crawldate,links,htmlstring,metatags,imagedata) FROM STDIN (FORMAT BINARY)")
        
        for doc in docs do
            writer.StartRow()
            writer.Write(doc.URL, NpgsqlDbType.Text)
            writer.Write(doc.ResponseTime, NpgsqlDbType.Integer)
            writer.Write(doc.Emails |> Array.ofList, NpgsqlDbType.Array ||| NpgsqlDbType.Text)
            writer.Write(doc.CSSlinks |> Array.ofList, NpgsqlDbType.Array ||| NpgsqlDbType.Text)
            writer.Write(doc.JSlinks |> Array.ofList, NpgsqlDbType.Array ||| NpgsqlDbType.Text)
            writer.Write(doc.CrawlDate.ToUniversalTime(), NpgsqlDbType.TimestampTz)
            writer.Write(doc.Links |> Array.ofList, NpgsqlDbType.Array ||| NpgsqlDbType.Text)
            writer.Write(doc.HTMLstring, NpgsqlDbType.Text)
            writer.Write(doc.Metatags |> Array.ofList, NpgsqlDbType.Array ||| NpgsqlDbType.Text)
            writer.Write(JsonConvert.SerializeObject(doc.Imagedata), NpgsqlDbType.Jsonb)

        return writer.Complete()
    } |> Async.RunSynchronously



let getentries(connectionString:string) : int64 =
    try
        connectionString
        |> Sql.connect
        |> Sql.query "SELECT COUNT(*) AS RowCount FROM maindata;"
        |> Sql.executeRow (fun read -> read.int64 "RowCount")
    with
        | _ -> 
            printfn "The database doesn't seem to be accessible"
            -1

let getRowCount (connectionString: string) : int64 =
    let query = "SELECT COUNT(*) FROM maindata"
    use connection = new NpgsqlConnection(connectionString)
    use command = new NpgsqlCommand(query, connection)

    connection.Open()
    let result = command.ExecuteScalar()
    match result with
    | :? int64 as count -> count
    | _ -> 0

let resetdata (connectionString:string) : int =
    connectionString
    |> Sql.connect
    |> Sql.query "TRUNCATE TABLE maindata"
    |> Sql.executeNonQuery