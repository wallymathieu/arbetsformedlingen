open System
open System.IO
open System.Net

open Stacka
open Stacka.Languages
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

type AdAndLanguage = {adId:string ; languages: string list}
with
  static member OfJson json =
        match json with
        | JArray arr when arr.Count=2 ->
          match ofJson (arr.Item 0), ofJson (arr.Item 1) with
          | (Ok adId),(Ok languages)->
            Ok { adId = adId; languages= languages }
          | (Error err),_ -> Error err
          | _,(Error err) -> Error err
        | x -> Decode.Fail.arrExpected x
  static member ToJson ({adId=adId;languages=languages}) =
    JArray [ toJson adId; toJson languages ]
  (*
  static member JsonObjCodec =
      fun id l -> { adId = id; languages=l }
      <!> jreq  "id" (Some << fun x -> x.adId)
      <*> jreq  "langs"  (Some << fun x -> x.languages)
  *)
module AdAndLanguage=
  /// sum ad and language list to count of languages
  let sumList (adAndLanguages:AdAndLanguage list)=
    adAndLanguages
    |> List.collect (fun {languages=list}->list)
    |> List.groupBy (fun s->s.ToLower())
    |> List.map (fun (s,l)->(s, l.Length))
    //
    |> List.sortByDescending (fun (_,l)->l)

  let ofAnnonsWithText ((a,text): WithText<Annons>)=
    let splitOnChars = Text.splitOnWSAndPunctuationChars
    let onlyLangs = List.filter (fun s->Set.contains s ProgrammingLanguages.wikipediaSet )
    {adId= a.id
     languages= splitOnChars (ProgrammingLanguages.translateVariants a.title)
               @ splitOnChars (ProgrammingLanguages.translateVariants text)
     |> List.map (fun s->s.ToLower())
     |> onlyLangs |> List.distinct}

  let toFile dir (adAndLanguages:AdAndLanguage list)=
    Async.AwaitTask (File.WriteAllTextAsync (Path.Combine(dir,"langs.json"), toJson adAndLanguages |> string))
  let ofFile dir :AdAndLanguage list ParseResult Async= async {
    let path = Path.Combine(dir,"langs.json")
    let! content= Async.AwaitTask (File.ReadAllTextAsync path)
    return parseJson content
  }


type WordCount=string * int
module Polly=
  let execute (policy : #AsyncPolicy) f  =async {
      let! res = policy.ExecuteAndCaptureAsync(fun () -> f() |> Async.StartAsTask) |> Async.AwaitTask
      return res.Result
  }
module AdRepository=

  let tryFetchAdToAndPersist a (repository:IAdRepository)=async{
    let id = Annons.id a
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
      .WaitAndRetryAsync([TimeSpan.FromMilliseconds(100.0);TimeSpan.FromMilliseconds(500.0);TimeSpan.FromSeconds(3.0);TimeSpan.FromSeconds(7.0);])
   
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
  let sum dir=async {
    let! content = Async.AwaitTask (File.ReadAllTextAsync (Path.Combine(dir,"langs.json") ))
    let maybeAdAndLanguages : AdAndLanguage list ParseResult = ofJson (JsonValue.Parse content)
    match maybeAdAndLanguages with
    | Ok adAndLanguages ->
      let l= AdAndLanguage.sumList adAndLanguages
      return Ok (toJson l |> string)
    | Error err-> return Error err
  }

module Old=

  [<Obsolete("!")>]
  let writeLangCount repository dir=async{
    let! adAndLanguages = AdRepository.getLangCount repository
    let langTags = adAndLanguages
                   |> List.map (fun {adId=id;languages=list}-> sprintf "%s : %s" id (String.concat ", " list) )
                   |> String.concat "\n"
    do! Async.AwaitTask (File.WriteAllTextAsync (Path.Combine(dir,"langs.txt"), langTags))
    let txt =adAndLanguages
              |> AdAndLanguage.sumList
              |> List.map (fun (s,l)-> sprintf "%d : %s" l s )
              |> String.concat "\n"
    do! Async.AwaitTask (File.WriteAllTextAsync (Path.Combine(dir,"list-langs.txt"), txt))
    let! wordCounts = AdRepository.getWordCount repository
    let txt =wordCounts
              |> List.filter ((<) 1 << snd)
              |> List.sortByDescending snd
              |> List.map (fun (s,l)-> sprintf "%d : %s" l s )
              |> String.concat "\n"
    do! Async.AwaitTask (File.WriteAllTextAsync (Path.Combine(dir, "list-words.txt"), txt))
  }
type CmdArgs =
  { Command : string option; Dir: string }
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
  let runSynchronouslyAndPrintResult fn=
    match Async.RunSynchronously fn  with
      | Ok v->
        Console.WriteLine (string v)
        0
      | Error e ->
        Console.Error.WriteLine (string e)
        1

  match args with
  | { Command=Some command; Dir=dir } ->
    match command with
    | "fetch" ->
      let r = Repositories.fileSystem dir
      Async.RunSynchronously( AdRepository.fetchListAndAds r dir)
      0
    | "batch" ->
      let r = Repositories.fileSystem dir
      Async.RunSynchronously( AdRepository.batchCount r dir)
      0
    | "sum" ->
      runSynchronouslyAndPrintResult (AdRepository.sum dir)
    | "write-lang-count" ->
      let r = Repositories.fileSystem dir
      Async.RunSynchronously( Old.writeLangCount r dir)
      0
    | _ ->
      printfn "error: Expected command"
      printfn "%s" usage
      1
  | _ ->
    printfn "error: Expected command"
    printfn "%s" usage
    1
