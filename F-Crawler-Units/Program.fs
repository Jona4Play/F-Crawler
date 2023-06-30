open System
open Newtonsoft
open System.Net
open System.IO
open Newtonsoft.Json
open System.Net.Http
open System.Diagnostics


let basedir = AppContext.BaseDirectory

let client = new HttpClient()
client.Timeout <- TimeSpan.FromSeconds(5)

let getHtml(link:string) : (string * int64 * string) option =
    let sw = Stopwatch.StartNew()
    async {
        try
            let! htmlContent = Async.AwaitTask(client.GetStringAsync(link))
            return Some (htmlContent, sw.ElapsedMilliseconds, link)
        with
            | _ -> return None
    } |> Async.RunSynchronously

let readBufferFromJson(filename) =
    File.ReadAllText(Path.Combine(basedir, filename)) 
    |> JsonConvert.DeserializeObject<list<string>>



[<EntryPoint>]
let test(args) =
    let links = readBufferFromJson("latest.json")
    let x = links |> Array.ofList |> Array.Parallel.choose getHtml
    0