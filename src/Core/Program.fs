module Main
open Arbetsformedlingen.Library
open System.IO
type CmdArgs = 
  { Command : string option; Dir: string
  }
[<EntryPoint>]
let main argv =
    let defaultArgs = { Command = None; Dir= Directory.GetCurrentDirectory() }
    let usage =
         ["Usage:"
          sprintf "    --dir     DIRECTORY  where to store data (Default: %s)" defaultArgs.Dir
          "    --command COMMAND    one of [fetch-files, write-lang-count]"] 
          |> String.concat System.Environment.NewLine

    let rec parseArgs b args = 
      match args with
      | [] -> b
      | "--command" :: command :: xs -> parseArgs { b with Command = Some command } xs
      | "--dir" :: dir :: xs -> parseArgs { b with Dir = dir } xs
      | invalidArgs -> 
        printfn "error: invalid arguments %A" invalidArgs
        printfn "%s" usage
        exit 1
    
    let args = argv |> List.ofArray |> parseArgs defaultArgs
    match args with
    | { Command=Some command; Dir=dir } ->
        match command with
        | "fetch-files" -> 
          Utv.fetchListAndAds dir
          0
        | "write-lang-count" ->
          Annons.writeLangCount dir
          0
        | _ ->
          printfn "error: Expected command"
          printfn "%s" usage
          1
    | _ ->
        printfn "error: Expected command"
        printfn "%s" usage
        1
