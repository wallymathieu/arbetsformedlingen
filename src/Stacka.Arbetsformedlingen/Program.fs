﻿open System
open System.IO
open System.Net

open Stacka
open Stacka.IO
open Stacka.Languages
open Stacka.AdsAndLanguages
open Stacka.Repositories
open Stacka.Arbetsformedlingen
open Stacka.Arbetsformedlingen.Integration

open Fleece
open Fleece.FSharpData
open Fleece.FSharpData.Operators
open FSharp.Data
open Polly
open FSharpPlus.Data

module RawAd=
  /// map to Annons with text
  let toAnnonsWithText (RawAd content)=
    let t = Annons.Complete.Parse content
    Annons.mapComplete t.Platsannons.Annons


type WordCount=string * int
module Polly=
  let execute (policy : #AsyncPolicy) f  =async {
      let! res = policy.ExecuteAndCaptureAsync(fun () -> f() |> Async.StartAsTask) |> Async.AwaitTask
      return res.Result
  }
module AdRepository=

  let tryFetchAdToAndPersist a (repository:IAdRepository)=async{
    let id = Ad.id a
    let! alreadyFetched= repository.Contains(id)
    if not alreadyFetched then
      match! Annons.tryDownload a with
      | Ok res ->
        let info = (id, RawAd res)
        do! repository.Store info
        return Ok <| Choice1Of2 info
      | Error err->
        return Error err
    else
      return Ok <| Choice2Of2 ()
  }
  let retryFourTimes =
    Policy
      .Handle<Exception>()
      .WaitAndRetryAsync([TimeSpan.FromMilliseconds 100.0; TimeSpan.FromMilliseconds 500.0;
                          TimeSpan.FromSeconds 3.0; TimeSpan.FromSeconds 7.0;])

  let fetchListAndAds (repository:IAdRepository) dir = async{
    let fetchList (yrkesId) =
      let q={ Platsannonser.defaultQuery with YrkesId=Some yrkesId; Sida=Some 0; AntalRader=Some 2000 }
      Platsannonser.query q

    let retryFetchList num = Polly.execute retryFourTimes (fun ()-> fetchList num)
    let! fetchLists = Async.Parallel(seq {
      yield retryFetchList 80
      yield retryFetchList 2419
      yield retryFetchList 7633
      yield retryFetchList 7632
      yield retryFetchList 7576
    })
    let matchingsdata = fetchLists
                        |> Array.collect ( fun platsannonser->
                          platsannonser.Matchningslista.Matchningdata
                          |> Array.map Platsannonser.mapToAnnons )
    let persistedAds = ResizeArray()
    for a in matchingsdata do
      let! res = tryFetchAdToAndPersist a repository
      persistedAds.Add res

    let adAndLanguages = ResizeArray()
    // processing new ads
    for ad in persistedAds do
      match ad with
      | Ok (Choice1Of2 (id,rawAd)) ->
        printfn "Fetched ad %A" id
        let lang = rawAd |> RawAd.toAnnonsWithText |> AdAndLanguage.ofAnnonsWithText
        adAndLanguages.Add lang
      | Ok _ -> ()
      | Error err-> printfn "Failed to fetch ad %A" err
    // try to persist the ad language counts
    let! maybeLangs = AdAndLanguage.ofFile dir
    match maybeLangs with
    | Ok langs->
      do! AdAndLanguage.toFile dir (List.append langs (List.ofSeq adAndLanguages))
    | Error err->printfn "Failed to retreive langs %A" err
    return ()
  }

  let getLangCount (repository:IAdRepository) : (AdAndLanguage list) Async=async{
    let! rawAds = repository.List()
    return rawAds |> List.map ( RawAd.toAnnonsWithText >> AdAndLanguage.ofAnnonsWithText )
  }
  let getWordCount (repository:IAdRepository) : (WordCount list) Async=async{
    let! rawAds = repository.List()
    let loaded = rawAds |> List.map RawAd.toAnnonsWithText
    let splitOnChars = Text.splitOnWSAndPunctuationChars
    let wordCounts= loaded
                    |> List.map (fun (a,text)-> a.id, splitOnChars a.title @ splitOnChars text
                                              |> List.map (fun s->s.ToLower())
                                              |> List.distinct )
                    |> List.collect (fun (_, words)-> words)
                    |> List.groupBy (fun s->s.ToLower())
                    |> List.map (fun (s,l)->(s,l.Length))
    return wordCounts
  }

  let batchCount repository dir=async {
    let! adAndLanguages = getLangCount repository
    do! AdAndLanguage.toFile dir adAndLanguages
  }
  let sum dir= AdAndLanguage.sumDir dir
  let syncRepositories (ra:IAdRepository) (rb:IAdRepository)=async{
    let! ids = ra.Ids()
    for id in ids do
      let! contains = rb.Contains id
      if not contains then
        match! ra.Get id with
        | Some ad -> do! rb.Store (id,ad)
        | _ -> ()
  }
module Old=

  //[<Obsolete("!")>]
  let writeLangCount repository dir=async{
    let! adAndLanguages = AdRepository.getLangCount repository
    let langTags = adAndLanguages
                   |> List.map (fun {adId=id;languages=list}-> sprintf "%s : %s" id (String.concat ", " list) )
                   |> String.concat "\n"
    do! File.writeAllTextAsync (Path.Combine(dir,"langs.txt")) langTags
    let txt =adAndLanguages
              |> AdAndLanguage.sumList
              |> List.map (fun (s,l)-> sprintf "%d : %s" l s )
              |> String.concat "\n"
    do! File.writeAllTextAsync (Path.Combine(dir,"list-langs.txt")) txt
    let! wordCounts = AdRepository.getWordCount repository
    let txt =wordCounts
              |> List.filter ((<) 1 << snd)
              |> List.sortByDescending snd
              |> List.map (fun (s,l)-> sprintf "%d : %s" l s )
              |> String.concat "\n"
    do! File.writeAllTextAsync (Path.Combine(dir, "list-words.txt")) txt
  }
// fsharplint:disable
[<Diagnostics.CodeAnalysis.SuppressMessage("*", "EnumCasesNames")>]
type Cmd=
  |fetch=0
  |batch=1
  |sum=2
  |writeLangCount=3
  |syncsql=4
// fsharplint:enable
type CmdArgs =
  { Command: Cmd option; Dir: string option; PGConn: string option }
open FSharpPlus
open System.Linq
open Npgsql

let (|Cmd|_|) : _-> Cmd option = tryParse
[<EntryPoint>]
let main argv =
  let defaultDir = Directory.GetCurrentDirectory()
  let defaultArgs = { Command = None; Dir = None; PGConn = None }
  let usage =
   ["Usage:"
    sprintf "    --dir     DIRECTORY  where to store data (Default: %s)" defaultDir
    sprintf "    --pgconn  connection string to database"
    sprintf "    COMMAND    one of [%s]" (Enum.GetValues( typeof<Cmd> ).Cast<Cmd>() |> Seq.map string |> String.concat ", " )]
    |> String.concat Environment.NewLine
  let rec parseArgs b args =
    match args with
    | [] -> Ok b
    | "--dir" :: dir :: xs -> parseArgs { b with Dir = Some dir } xs
    | "--pgconn" :: conn :: xs -> parseArgs { b with PGConn = Some conn } xs
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
    let createPgRepository conn =
      let getConnection ()=
        let conn = new NpgsqlConnection(conn)
        conn.Open()
        conn
      Repositories.sql getConnection

    let repositories =
      let repos = [
        Option.map createPgRepository args.PGConn
        Option.map ((flip Repositories.fileSystem) "data") args.Dir
      ]
      let list = List.choose id repos
      if List.isEmpty list then Repositories.fileSystem defaultDir "data"
      else List.reduce Repositories.leftCombine list
    match args with
    | { Command=Some command; PGConn=conn } ->
      let dir = Option.defaultValue defaultDir args.Dir
      match command with
      | Cmd.fetch ->
        Async.RunSynchronously( AdRepository.fetchListAndAds repositories dir)
        0
      | Cmd.batch ->
        Async.RunSynchronously( AdRepository.batchCount repositories dir)
        0
      | Cmd.sum ->
        runSynchronouslyAndPrintResult (AdRepository.sum dir)
      | Cmd.writeLangCount ->
        Async.RunSynchronously( Old.writeLangCount repositories dir)
        0
      | Cmd.syncsql ->
        match conn with
        | Some conn->
          let fsRepository = Repositories.fileSystem dir "data"
          let sqlRepository = createPgRepository conn
          Async.RunSynchronously( AdRepository.syncRepositories fsRepository sqlRepository)
          0
        | _ ->
          printfn "error: Expected a connection string"
          printfn "%s" usage
          1
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
