namespace Stacka.AdsAndLanguages
open Stacka.Languages
open Stacka
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

module AdAndLanguage=
  /// sum ad and language list to count of languages
  let sumList (adAndLanguages:AdAndLanguage list)=
    adAndLanguages
    |> List.collect (fun {languages=list}->list)
    |> List.groupBy (fun s->s.ToLower())
    |> List.map (fun (s,l)->(s, l.Length))
    //
    |> List.sortByDescending (fun (_,l)->l)

  let inline getId(r:^a) = ( ^a : ( member get_id: unit->string ) (r) )
  let inline getTitle(r:^a) = ( ^a : ( member get_title: unit->string ) (r) )

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