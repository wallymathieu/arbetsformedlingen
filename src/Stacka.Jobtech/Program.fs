open System
open System.IO
open System.Net

open Stacka
open Stacka.IO
open Stacka.Languages
open Stacka.Repositories
open Stacka.Jobtech
open Stacka.AdsAndLanguages
open Stacka.Jobtech.Integration

open FSharp.Data
open FSharpPlus.Data
open FSharpPlus.Operators
open Stacka.Jobtech.Integration

module JobtechDev=
  let fetchLatest dir = async{
    let datadir = Path.Combine(dir,"jobtech")
    let maxDateTime = Directory.GetFiles datadir
                      |> choose DateTime.fromFileName
                      |> Array.max
    let nextFileName = DateTime.toFileName DateTime.UtcNow
    let! next = Platsannonser.stream (maxDateTime.AddMinutes -1.0 ) // slight overlap
    do! File.writeAllTextAsync (Path.Combine(datadir, nextFileName)) next.RawStream
  }
  let data dir = async{
    let ads = ResizeArray()
    let notRemoved = not << Annons.isRemoved
    for file in Directory.GetFiles(Path.Combine(dir, "jobtech"), "*.json") do
      let! content = File.readAllTextAsync file
      let parsed = JsonT.Parse content |> Array.filter notRemoved |> Array.map Annons.mapFromJobtech
      parsed |> ads.AddRange
    return ads |> Seq.distinctBy (fst >> Annons.id) |> Seq.toList
  }
  let batch dir = async{
    let! maybeLangs = AdAndLanguage.ofFile dir
    match maybeLangs with
    | Ok langs->
      let ids=langs |> List.map AdAndLanguage.adId |> Set.ofList
      let knownId id = Set.contains id ids
      let notKnownId = not << knownId << (fst >> Annons.id)
      let! all = data dir
      let adAndLanguages = all |> List.filter notKnownId |> List.map AdAndLanguage.ofAnnonsWithText
      do! AdAndLanguage.toFile dir (List.append langs (List.ofSeq adAndLanguages))
    | Error err->printfn "Failed to retreive langs %A" err
  }
  let sum dir= AdAndLanguage.sumDir dir

// fsharplint:disable
[<Diagnostics.CodeAnalysis.SuppressMessage("*", "EnumCasesNames")>]
type Cmd=
  |fetch=0
  |batch=1
  |sum=2
// fsharplint:enable
type CmdArgs =
  { Command: Cmd option; Dir: string option; }
open FSharpPlus
open System.Linq

let (|Cmd|_|) : _-> Cmd option = tryParse
[<EntryPoint>]
let main argv =
  let defaultDir = Directory.GetCurrentDirectory()
  let defaultArgs = { Command = None; Dir = None; }
  let usage =
   ["Usage:"
    sprintf "    --dir     DIRECTORY  where to store data (Default: %s)" defaultDir
    sprintf "    COMMAND    one of [%s]" (Enum.GetValues( typeof<Cmd> ).Cast<Cmd>() |> Seq.map string |> String.concat ", " )]
    |> String.concat Environment.NewLine
  let rec parseArgs b args =
    match args with
    | [] -> Ok b
    | "--dir" :: dir :: xs -> parseArgs { b with Dir = Some dir } xs
    | Cmd cmd :: xs-> parseArgs { b with Command = Some cmd } xs
    | invalidArgs ->
      sprintf "error: invalid arguments %A" invalidArgs |> Error

  match argv |> List.ofArray |> parseArgs defaultArgs with
  | Ok args->
    let runSynchronouslyAndPrintResult fn=
      match Async.RunSynchronously fn  with
        | Ok v->
          Console.WriteLine (string v)
          0
        | Error e ->
          Console.Error.WriteLine (string e)
          1

    match args with
    | { Command=Some command; } ->
      let dir = Option.defaultValue defaultDir args.Dir
      match command with
      | Cmd.fetch ->
        Async.RunSynchronously( JobtechDev.fetchLatest dir)
        0
      | Cmd.batch ->
        Async.RunSynchronously( JobtechDev.batch dir)
        0
      | Cmd.sum ->
         runSynchronouslyAndPrintResult (JobtechDev.sum dir)
      | _ ->
        printfn "error: Expected valid command"
        printfn "%s" usage
        1
    | _ ->
      printfn "error: Expected command"
      printfn "%s" usage
      1
  | Error err->
      printfn "%s" err
      printfn "%s" usage
      1
