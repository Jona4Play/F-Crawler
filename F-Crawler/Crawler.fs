module Crawler
open System
open Parser
open NUglify
open System.Net.Http
open Postgres
open FileHandle
open FSharp.Data
open DataStructures
open System.Diagnostics
open System.Collections.Generic

let client = new HttpClient()
client.Timeout <- TimeSpan.FromSeconds(5)

let line = "------------------------------------"

let printline() = printfn "%s" line

let getHtml(link:string) : (string * int64 * string) option =
    let sw = Stopwatch.StartNew()
    async {
        try
            let! htmlContent = Async.AwaitTask(client.GetStringAsync(link))
            return Some (htmlContent, sw.ElapsedMilliseconds, link)
        with
            | _ -> return None
    } |> Async.RunSynchronously

    
let extractdata (basedata: string * int64 * string) : Document * string list =
    let (data, responsetime, baseurl) = basedata
    let doc = HtmlDocument.Parse(data)
    let links = linksof(doc,baseurl)
    let prioritylinks = getprioritylinksof(links, baseurl)
    let jslinks = (doc,baseurl) |> searchJSReferences
    let csslinks = (doc,baseurl) |> searchCSSReferences
    let images = (doc,baseurl) |> extractImageInfo
    let metatags = doc |> extractMetaTags
    let emails = data |> extractEmails |> Array.ofSeq

    GC.Collect()

    ({URL=baseurl; Emails=emails; Imagedata=images; CSSlinks=csslinks; JSlinks=jslinks; CrawlDate=DateTime.Now; ResponseTime=responsetime; Links=links; HTMLstring=data; Metatags=metatags}, prioritylinks)




let startCrawling(links: string list) : Document array * string list =
    let results = 
        links 
        |> Array.ofList 
        |> Array.Parallel.choose getHtml
        |> Array.Parallel.map extractdata
    let docs = results |> Array.map fst
    let prioritylinks = results |> Array.map snd |> List.concat |> shuffleList |> List.truncate 1000
    (docs, prioritylinks)



[<EntryPoint>]
let main(args: string array) =
    let connectionstring = readsecrets()
    printfn "Welcome to the F# web crawler: Maindata has %d entries" (getRowCount connectionstring)
    let rec itemmenu(links: string list) =
        printline()
        printfn "Select an action to perform\n%s\n1. Start crawling from start link\n2. Load link from buffer\n3. Load latest links\n4. Reset Buffer\n5. Reset DB\n%s" line line
        try
            match int(Console.ReadLine()) with
            | 1 -> 
                let (data, links) = startCrawling(links)
                printfn "Crawled a cumulative of %d websites and found %d new links to crawl" data.Length links.Length
                let sw = Stopwatch.StartNew()
                let rowsinserted = Postgres.pushdata(data, connectionstring)
                sw.Stop()
                printfn "Finished pushing %d rows in %dms" rowsinserted sw.ElapsedMilliseconds
                writeBufferToJson(links,"latest.json")
                itemmenu(links)
            | 2 -> 
                itemmenu(readBufferFromJson("buffer.json"))
            | 3 -> itemmenu(readBufferFromJson("latest.json"))
            | 4 -> 
                writeBufferToJson("", "buffer.json")
                itemmenu(links)
            | 5 ->
                resetdata(connectionstring) |> ignore
                printfn "Reset maindata and scrapped all data"
                itemmenu(links)
            | _ -> 
                printfn "Invalid input. Please input a valid operation number"
                itemmenu(links)
        with
        | :? System.FormatException -> 
            printfn "Invalid input"
            itemmenu(links)
        | ex -> 
             printfn "An error occurred: %s" ex.Message
             itemmenu(links)
    itemmenu(
        [
            "https://www.wikipedia.org/"
            "https://www.reddit.com/"
            "https://dmoz-odp.org/"
            "https://www.cnn.com/" 
            "https://www.bbc.com/"
            "https://www.nytimes.com/"
            "https://www.twitter.com/"
            "https://www.instagram.com/"
            "https://www.quora.com/"
            "https://stackexchange.com/"
            "https://arxiv.org/"
        ])
    0
