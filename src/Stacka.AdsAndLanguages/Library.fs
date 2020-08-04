namespace Stacka.AdsAndLanguages
open Stacka.Languages
open Stacka
open Stacka.IO

open Fleece
open Fleece.FSharpData
open Fleece.FSharpData.Operators

open FSharp.Data
open FSharpPlus.Data
open System.IO

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

module AdAndText=
  let inline toIdAndWords ((a,text): WithText<_>)=
      let splitOnChars = Text.splitOnWSAndPunctuationChars
      let words= splitOnChars (getTitle a)
                 @ splitOnChars text
                 |> List.map (fun s->s.ToLower())
                 |> List.distinct
      (getId a, words)


module AdAndLanguage=
  let adId (a:AdAndLanguage)=a.adId
  let languages (a:AdAndLanguage)=a.languages
  /// sum ad and language list to count of languages
  let sumList (adAndLanguages:AdAndLanguage list)=
    adAndLanguages
    |> List.collect (fun {languages=list}->list)
    |> List.groupBy (fun s->s.ToLower())
    |> List.map (fun (s,l)->(s, l.Length))
    //
    |> List.sortByDescending (fun (_,l)->l)


  let inline ofAnnonsWithText ((a,text): WithText<_>)=
    let splitOnChars = Text.splitOnWSAndPunctuationChars
    let onlyLangs = List.filter (fun s->Set.contains s ProgrammingLanguages.wikipediaSet )
    {adId= getId a
     languages= splitOnChars (ProgrammingLanguages.translateVariants (getTitle a))
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
  let sumDir dir=async {
    let! content = File.readAllTextAsync (Path.Combine(dir,"langs.json") )
    let maybeAdAndLanguages : AdAndLanguage list ParseResult = ofJson (JsonValue.Parse content)
    match maybeAdAndLanguages with
    | Ok adAndLanguages ->
      let l= sumList adAndLanguages
      return Ok (toJson l |> string)
    | Error err-> return Error err
  }

  let inline countLangAndWords (adsWithWords: (string * string List) List) (adsWithLanguages:AdAndLanguage List)=
    let adIdLangsAndWords = query {
      for adWithL in adsWithLanguages do
      join (adId,words) in adsWithWords on (adWithL.adId = adId)
      select (adWithL.adId, adWithL.languages, words)
    }
    let counts = query {
      for (_,langs,words) in adIdLangsAndWords do
      for lang in langs do
      for word in words do
      where (lang <> word)
      groupBy (lang,word) into g
      select (g.Key, Seq.length g)
    }
    counts |> Seq.toList

