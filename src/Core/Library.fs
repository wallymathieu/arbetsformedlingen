namespace Arbetsformedlingen.Library
open System
open System.IO
open FSharp.Data
open Arbetsformedlingen.Integration
open Arbetsformedlingen

module Utv=
    let fetchList (yrkesId) dir=
        let outPath = Path.Combine(dir, "list", sprintf "platsannonser_yrkesid_%d.json" yrkesId)
        if not <| File.Exists outPath then 
          File.WriteAllText (outPath, "")
        let q={ Platsannonser.defaultQuery with YrkesId=Some yrkesId; Sida=Some 0; AntalRader=Some 2000 }
        let res = Platsannonser.query q
        
        use out= new FileStream(outPath, FileMode.Truncate, FileAccess.Write, FileShare.Read)
        use writer = new StreamWriter(out)
        res.JsonValue.WriteTo(writer, JsonSaveOptions.None)

    let tryFetchAd a dir=
        let fn = Path.Combine(dir, "data", sprintf "%s.json" <| Annons.id a)
        if not <| File.Exists fn then
            match Annons.tryDownload a with
            | Ok res -> 
                File.WriteAllText( fn, res)
            | Error err->
                printfn "Couldn't download ad %s due to %O" a.id err

    let rec retry times fn = 
        if times > 1 then
            try
                fn()
            with 
            | _ -> retry (times - 1) fn
        else
            fn()

    let fetchListAndAds dir=
      let retryFetchList num dir= retry 3 (fun ()->fetchList num dir)
      retryFetchList 80 dir
      retryFetchList 2419 dir
      retryFetchList 7633 dir
      retryFetchList 7632 dir
      retryFetchList 7576 dir
      let listFile = Directory.GetFiles(Path.Combine(dir, "list"),"*.json")
      let loadAndMap (f:string)=
        let file = Path.Combine(dir, f)
        let platsannonser = Platsannonser.Load file
        platsannonser.Matchningslista.Matchningdata
        |> Array.map Platsannonser.mapToAnnons
      let matchingsdata = listFile 
                          |> Array.map loadAndMap
                          |> Array.collect id
      for a in matchingsdata do
          tryFetchAd a dir


             
module Annons=
  //let t = T.Load "./sample_22898479.json"
  type AdAndLanguage = string * string list
  type WordCount=string * int
  let splitAndFilter (text:string)=
    Text.normalize text 
    |> Text.splitOnChars
    |> Array.toList
  let loadAndMap dir (f:string) =
    let file = Path.Combine(dir, f)
    let content = File.ReadAllText file
    if String.IsNullOrEmpty content then
      None
    else   
      let t = Annons.Complete.Parse content
      Some <| Annons.mapComplete t.Platsannons.Annons
  let splitOnWSAndPunctuationChars (text:string)=
      let splitChars = Text.punctuationChars@ Text.whitespaceChars @ ['(';')';'”';'\'';'•';'/';'"';'™'] |>List.toArray
      text.Split(splitChars, StringSplitOptions.RemoveEmptyEntries) |> List.ofArray

  let getLangCount dir : (AdAndLanguage list * WordCount list)=
    let files = Directory.GetFiles(Path.Combine(dir, "data"),"*.json")
    let loadAndMap = loadAndMap dir
    let loaded = files 
                    |> Array.choose loadAndMap 
                    |> Array.toList
    let splitOnChars = splitOnWSAndPunctuationChars
    let onlyLangs = List.filter (fun s->Set.contains s ProgrammingLanguages.names )
    let adAndLanguage =loaded
                       |> List.map (fun (a,text)-> a.id, splitOnChars a.title @ splitOnChars text
                                                  |> List.map (fun s->s.ToLower()) 
                                                  |> onlyLangs |> List.distinct )
    let wordCounts= adAndLanguage
                    |> List.collect (fun (_, langs)-> langs)
                    |> List.groupBy (fun s->s.ToLower())
                    |> List.map (fun (s,l)->(s,l.Length))
    (adAndLanguage, wordCounts)
  let getWordCount dir : (WordCount list)=
    let files = Directory.GetFiles(Path.Combine(dir, "data"),"*.json")
    let loadAndMap = loadAndMap dir
    let loaded = files 
                    |> Array.choose loadAndMap 
                    |> Array.toList
    let splitOnChars = splitOnWSAndPunctuationChars
    let wordCounts= loaded
                    |> List.map (fun (a,text)-> a.id, splitOnChars a.title @ splitOnChars text
                                              |> List.map (fun s->s.ToLower()) 
                                              |> List.distinct )
                    |> List.collect (fun (_, words)-> words)
                    |> List.groupBy (fun s->s.ToLower())
                    |> List.map (fun (s,l)->(s,l.Length))
    wordCounts
  let writeLangCount dir=
    let (adAndLanguage, wordCounts) = getLangCount dir
    let langTags = adAndLanguage
                   |> List.map (fun (id,list)-> sprintf "%s : %s" id (String.concat ", " list) )
                   |> String.concat "\n"
    File.WriteAllText ("langs.txt", langTags)
    let txt =wordCounts
              |> List.sortByDescending (fun (_,l)->l)
              |> List.map (fun (s,l)-> sprintf "%d : %s" l s )
              |> String.concat "\n" 
    File.WriteAllText ("list-langs.txt", txt)
    let wordCounts = getWordCount dir
    let txt =wordCounts
              |> List.filter ((<) 1 << snd)
              |> List.sortByDescending (fun (_,l)->l)
              |> List.map (fun (s,l)-> sprintf "%d : %s" l s )
              |> String.concat "\n" 
    File.WriteAllText ("list-words.txt", txt)
    