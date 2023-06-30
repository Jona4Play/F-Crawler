module FileHandle

open System
open System.IO
open Newtonsoft.Json

let basedir = AppContext.BaseDirectory
let bufferdir = "buffer.json"

let readBufferFromJson filename =
    File.ReadAllText(Path.Combine(basedir, filename)) 
    |> JsonConvert.DeserializeObject<list<string>>


let writeBufferToJson(text, filename) =
    let path = Path.Combine(basedir, filename)
    printfn "%s" path
    File.WriteAllText(path, text |> JsonConvert.SerializeObject)

let readsecrets() =
    File.ReadAllText(Path.Combine(basedir, "secrets.txt"))
