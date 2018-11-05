namespace Library
open System
open System.IO
open FSharp.Data
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
        let fn = Path.Combine(dir, "data", Annons.id a)
        if not <| File.Exists fn then
            match Annons.tryDownload a with
            | Ok res -> 
                File.WriteAllText( fn, res)
            | Error err->
                printfn "Couldn't download ad %s due to %O" a.id err

    let fetchListAndAds dir=
      
      fetchList 80 dir
      fetchList 2419 dir
      fetchList 7633 dir
      fetchList 7632 dir
      fetchList 7576 dir
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

module Text=
  let alias =[ 
              ["developer"; "swutvecklare";  "utvecklare"; ], "_developer_"
              ["programming"; "utveckling"; "programmering"; "development" ], "_programming_"
              ["services"; "service" ], "_services_"
              ["js"; "javascript" ], "_javascript_"
              ["linux"; "_linux_miljö"], "_linux_"
              ["windows"; "_windows_miljö"; ], "_windows_"
              ["frontend"; "front-end"; "front end"], "_frontend_"
              ["backend"; "back-end"; "back end"], "_backend_"
              ["fullstack"; "full-stack"; "full stack"], "_fullstack_"
              ["r & d"; ], "_research_and_development_"
              ["c ++"; ], "_c++_"
              ["c #"; "csharp"], "_c#_"
              ["f #"; "fsharp"], "_f#_"
              [".net";], "_dotnet_"
             ]
             //|> List.map (fun (alias,to')->set alias,to')
  let mapAlias (text:string)=
    let mutable res =text
    for (list,to') in alias do
      for v in list do
        res <- res.Replace(v,to')
    res

  let postfixWords = ["_developer_"; "_programming_"; "_service_"]
  let insertPostFix (text:string)=
    let pickPostfix (postfix :string)= 
      let index = text.IndexOf postfix
      if index >0 then Some (postfix,index) else None
    match postfixWords |> List.tryPick pickPostfix with
    | Some (_,index)-> text.Insert(index, "-")
    | None -> text
  let normalize = mapAlias >> insertPostFix

  let whitespaceChars = [
                  '\r'
                  '\n'
                  ' '
                  '\t'
                  '\u00A0'
                  ]
  let punctuationChars = [
                  ';'
                  ':'
                  '.'
                  '!'
                  ','
                  '?'
                  ]
  let andChars = ['&']
  let otherChars = ['/';')';'('; '•';'·';'\'';'"';'’';'-';'_';'*';'–']
  let splitChars = whitespaceChars @ punctuationChars @ andChars @ otherChars |> List.toArray
  let splitOnChars (text:string)=text.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)
             
module Annons=
  //let t = T.Load "./sample_22898479.json"
  let writeLangCount dir=
    let files = Directory.GetFiles(Path.Combine(dir, "data"),"*.json")
    let loadAndMap (f:string)=
      let file = Path.Combine(dir, f)
      let content = File.ReadAllText file
      if String.IsNullOrEmpty content then
        None
      else   
        let t = Annons.Complete.Parse content
        Some <| Annons.mapComplete t.Platsannons.Annons
    let splitAndFilter (text:string)=
      Text.normalize text 
      |> Text.splitOnChars
      |> Array.toList
    let loaded = files 
                    |> Array.choose loadAndMap 
                    |> Array.toList


    let onlyLangs = List.filter (fun s->Set.contains s ProgrammingLanguages.names )
    let splitOnChars (text:string)=
        let splitChars = Text.punctuationChars@ Text.whitespaceChars |>List.toArray
        text.Split(splitChars, StringSplitOptions.RemoveEmptyEntries) |> List.ofArray
    let adAndLanguage =loaded
                       |> List.map (fun (a,text)-> a.id, splitOnChars a.title @ splitOnChars text
                                                  |> List.map (fun s->s.ToLower()) 
                                                  |> onlyLangs |> List.distinct )
    let langTags = adAndLanguage
                   |> List.map (fun (id,list)-> sprintf "%s : %s" id (String.concat ", " list) )
                   |> String.concat "\n"
    File.WriteAllText ("langs.txt", langTags)
    let wordCounts= adAndLanguage
                    |> List.collect (fun (_, langs)-> langs)
                    |> List.groupBy (fun s->s.ToLower())
                    |> List.map (fun (s,l)->(s,l.Length))
                    //|> List.filter(fun (_,l)->l>1)
    let txt =wordCounts
              |> List.sortByDescending (fun (_,l)->l)
              |> List.map (fun (s,l)-> sprintf "%d : %s" l s )
              |> String.concat "\n" 
    File.WriteAllText ("list-langs.txt", txt)
