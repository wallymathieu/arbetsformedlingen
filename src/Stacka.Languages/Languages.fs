module Stacka.Languages.ProgrammingLanguages
open System
open FSharp.Data
module private Internal=
  type T =HtmlProvider<"./Timeline-of-programming-languages-Wikipedia.htm">
  let t =T.Load("./Timeline-of-programming-languages-Wikipedia.htm")
  let inline getName(r:^a) = ( ^a : ( member get_Name: unit->String ) (r) )
  (*
  // since go is a word commonly used, it's difficult to pick out
  let probNotRelevant = ["e";"it";"small";"go";"clean"; "name"
                         "s"; "k"; "q"; "p"; "red"; "rapid"; "d"
                         "b"; "ml"; "spark"; "processing"; "basic"; "sas"; "io"; "george"; "pure"; "tutor"; "crystal"; "fp"
                         "print"; "ring"; "links"; "scratch"; "factor"
                         "pilot"; "plus"; "actor"; "joy"; "fact"; "hack"]
                         *)
  let listOfLanguages=
    [t.Tables.``1950s``.Rows |> Array.map getName
     t.Tables.``1960s``.Rows |> Array.map getName
     t.Tables.``1970s``.Rows |> Array.map getName
     t.Tables.``1980s``.Rows |> Array.map getName
     t.Tables.``1990s``.Rows |> Array.map getName
     t.Tables.``1990s``.Rows |> Array.map getName
     t.Tables.``2000s``.Rows |> Array.map getName
     t.Tables.``2010s``.Rows |> Array.map getName
    ]
   |> List.collect List.ofArray
open System.Text.RegularExpressions
let conceptOrImplementation=
 ["(concept)";"(notation)";"(implementation)";"(concept published)"; "(Wolfram Language)";"(programming language)"]
 |> List.map Regex.Escape
 |> String.concat "|"
 |> fun v-> Regex( v, RegexOptions.IgnoreCase)

open Internal
let wikipediaVariantsSet=
  let lang1=listOfLanguages |> List.map (fun v-> conceptOrImplementation.Replace(v, "").Trim())
  let trimToLower (s:string)=s.Trim().ToLower()
  let withBaseAndVersion = lang1 |> List.choose (fun v->
    let baseAndVersion = Regex("^(?<base>[a-zA-Z#+]*)[- ]?(?<version>[0-9IVXW]+)$")
    let m = baseAndVersion.Match v
    if m.Success then Some (trimToLower m.Groups.["base"].Value, trimToLower m.Groups.["version"].Value) else None)
  let withStandardPrefix = lang1 |> List.choose (fun v->
    let withPrefix = Regex("^standard (?<base>[a-zA-Z#+]*)$", RegexOptions.IgnoreCase)
    let m = withPrefix.Match v
    if m.Success then Some (trimToLower m.Groups.["base"].Value, "standard") else None)
  withBaseAndVersion
  |> List.append withStandardPrefix
  |> List.filter ( not << String.IsNullOrEmpty << fst)
  |> set
let wikipediaSet=
  let baseLangs = wikipediaVariantsSet |> Set.map fst
  let lang1=listOfLanguages |> List.map (fun v-> conceptOrImplementation.Replace(v, "").Trim().ToLower())
            |> List.filter (fun s->not <| Array.exists (fun v->Set.contains v baseLangs) (s.Split([|' ';'-'|], StringSplitOptions.RemoveEmptyEntries)))
            |> set

  baseLangs
  |> Set.union lang1
  //|> List.filter (fun s-> not <| List.contains s probNotRelevant )
  //|> List.append ["golang"; "nodejs"]

// should have some way of detecting Go as a language

let languageAlias=[
  ["c ++"; "C ++";
   "c++03"; "c++11"; "c++14"; "c++17"
   "C++03"; "C++11"; "C++14"; "C++17"
  ], "c++"
  ["c99"] "c"
  ["c #"; "C #"; "csharp"], "c#"
  ["f #"; "F #"; "fsharp"], "f#"
]

let translateVariants (text:string)=
  let mutable res =text
  for (list,to') in languageAlias do
    for v in list do
      res <- res.Replace(v,to')
  res
