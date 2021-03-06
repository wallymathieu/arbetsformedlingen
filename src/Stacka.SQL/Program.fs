﻿open System
open Stacka.SQL

type CmdArgs =
  { connection : string
    processor : string
    operation : string
    version : int64
  }

[<EntryPoint>]
let main argv =
    let defaultArgs =
      { connection= "Server=localhost;Database=stacka;User id=stacka;Password=STACKA_TEST_PASSWORD;Port=5432;CommandTimeout=200;"
        processor = "PostgreSQL"
        operation = "migrate"
        version = 0L
      }
    let printHelp () =
      printfn "Usage:"
      printfn "    --connection connection_string (Default: %s)" defaultArgs.connection
      printfn "    --processor processor_id (Default: %s)" defaultArgs.processor
      printfn "    --operation operation (Default: %s)" defaultArgs.operation
      exit 1
    let rec parseArgs b args =
      match args with
      | [] -> b
      | "--connection" :: connection :: xs -> parseArgs { b with connection = connection } xs
      | "--processor" :: processor :: xs -> parseArgs { b with processor = processor } xs
      | "--operation" :: operation :: xs -> parseArgs { b with operation = operation } xs
      | invalidArgs ->
        printfn "error: invalid arguments %A" invalidArgs
        printHelp()

    let args = argv
              |> List.ofArray
              |> parseArgs defaultArgs

    // Instantiate the runner
    let runner = MigrationRunner.create args.connection args.processor
    match args.operation with
    | "migrate" ->
      runner.MigrateUp()
      0
    | "down" ->
      runner.MigrateDown(args.version)
      0
    | _ ->
      printHelp()
      1 // return an integer exit code
