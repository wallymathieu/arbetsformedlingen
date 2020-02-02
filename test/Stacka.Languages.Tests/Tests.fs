module Tests

open System
open Xunit
open Stacka.Languages.ProgrammingLanguages

[<Theory>]
[<InlineData("c++")>]
[<InlineData("java")>]
[<InlineData("c#")>]
[<InlineData("golang")>]
[<InlineData("oberon")>]
[<InlineData("perl")>]
[<InlineData("hack")>]
[<InlineData("ada")>]
[<InlineData("mathematica")>]
[<InlineData("modula")>]
[<InlineData("python")>]
[<InlineData("magik")>]
[<InlineData("ml")>]
[<InlineData("self")>]
[<InlineData("rpg")>]
let ``Should be able to normalize languages from Wikipedia`` (language) =
  Assert.True(Set.contains language wikipediaSet)

[<Theory>]
[<InlineData("c++ 11")>]
[<InlineData("occam 2")>]
[<InlineData("modula-3")>]
let ``Should not contain variants in base set`` (language) =
  Assert.False(Set.contains language wikipediaSet)

[<Theory>]
[<InlineData("c++","03")>]
[<InlineData("c++","standard")>]
[<InlineData("c++","11")>]
[<InlineData("c++","14")>]
[<InlineData("c++","17")>]
[<InlineData("ada","2005")>]
[<InlineData("ada","2012")>]
[<InlineData("oberon", "07")>]
let ``Should be able to get language variants`` (language, variant:string) =
  Assert.True(Set.contains language wikipediaSet)
  Assert.True(Set.contains (language,variant) wikipediaVariantsSet, wikipediaVariantsSet |> Set.toList |> List.map string |> String.concat ",")

[<Theory>]
[<InlineData("c++", "c++")>]
[<InlineData("c++03", "c++")>]
[<InlineData("c++11", "c++")>]
[<InlineData("c++14", "c++")>]
[<InlineData("c++17", "c++")>]
let ``Should be able to translate c++ language variants`` (languageAndVariant, language:string) =
  Assert.Equal(language, translateVariants languageAndVariant)

[<Theory>]
[<InlineData("c #", "c#")>]
[<InlineData("csharp", "c#")>]
[<InlineData("f #", "f#")>]
[<InlineData("fsharp", "f#")>]
[<InlineData("språket Go", "golang")>]
let ``Should be able to translate sharp language variants`` (languageAndVariant, language:string) =
  Assert.Equal(language, translateVariants languageAndVariant)

(*
[<Fact>]
let ``hupp`` () =
  Assert.False(true, wikipediaVariantsSet |> Set.toList |> List.map string |> String.concat ",")
*)

