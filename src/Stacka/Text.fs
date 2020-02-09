module Stacka.Text
open System

let whitespaceChars = [ '\r'; '\n'; ' '; '\t'; '\u00A0' ]
let punctuationChars = ";:.!,?".ToCharArray() |> Array.toList
let andChars = ['&']
let otherChars = "/)(•·'\"’-_*–".ToCharArray() |> Array.toList
/// Backslash, Brackets and others
let charSetX= "()”'•\"™".ToCharArray() |> Array.toList
let splitChars = whitespaceChars @ punctuationChars @ andChars @ otherChars |> List.toArray
let splitOnChars (text:string)=text.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)

let splitOnWSAndPunctuationChars (text:string)=
  let splitChars = punctuationChars @ whitespaceChars @ charSetX |>List.toArray
  text.Split(splitChars, StringSplitOptions.RemoveEmptyEntries) |> List.ofArray
