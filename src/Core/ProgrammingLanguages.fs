module ProgrammingLanguages
open System
open FSharp.Data
module private Internal=
    type T =HtmlProvider<"./Timeline-of-programming-languages-Wikipedia.htm">
    let t =T.Load("./Timeline-of-programming-languages-Wikipedia.htm")
    let inline getName(r:^a) = ( ^a : ( member get_Name: unit->String ) (r) )
    // since go is a word commonly used, it's difficult to pick out
    let probNotRelevant = ["e";"it";"small";"go";"clean"; "name"
                           "s"; "k"; "q"; "p"; "red"; "rapid"; "d"
                           "b"; "ml"; "spark"; "processing"; "basic"; "sas"; "io"; "george"; "pure"; "tutor"; "crystal"; "fp"
                           "print"; "ring"; "links"; "scratch"; "factor"
                           "pilot"; "plus"; "actor"; "joy"; "fact"; "hack"]

open Internal
let names =
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
         |> List.map (fun s->s.ToLower())
         |> List.filter (fun s-> not <| List.contains s probNotRelevant )
         |> List.append ["golang"; "nodejs"]
         |> set

