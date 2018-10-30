module Main
open Library
type CmdArgs = 
  { Command : string option
  }
[<EntryPoint>]
let main argv =
    let defaultArgs = { Command = None }
    let usage =
         ["Usage:"
          "    --command COMMAND    command"] |> String.concat System.Environment.NewLine

    let rec parseArgs b args = 
      match args with
      | [] -> b
      | "--command" :: command :: xs -> parseArgs { b with Command = Some command } xs
      | invalidArgs -> 
        printfn "error: invalid arguments %A" invalidArgs
        printfn "%s" usage
        exit 1
    
    let args = argv |> List.ofArray |> parseArgs defaultArgs
    match args with
    | { Command=Some command } ->
        match command with
        | "fetch-files" -> 
          Utv.fetchListAndAds()
          0
        | "write-lang-count" ->
          Annons.writeLangCount()
          0
        | _ ->
          printfn "error: Expected command"
          printfn "%s" usage
          1
    | _ ->
        printfn "error: Expected command"
        printfn "%s" usage
        1
