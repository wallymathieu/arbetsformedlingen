namespace Stacka
open System

/// also known as Annons
type Ad={
  id:string
  title:string
  url:string
  relevans:int
  source:string}

type WithText<'a>=('a*string)

type RawAd = RawAd of string
module Ad =
  let id (a:Ad)=a.id
open System.IO
open System.Globalization
open FSharpPlus

module DateTime=
  let private tupleToOption x = match x with true, value -> Some value | _ -> None
  let fromFileName (filename:string)=
    if String.IsNullOrEmpty filename then None
    else
      /// for example: 2020-02-28T01_01_01.json
      let withoutext = Path.GetFileNameWithoutExtension filename
      DateTime.TryParseExact       (withoutext, [|"yyyy-MM-ddTHH_mm_ss"|], null, DateTimeStyles.RoundtripKind) |> tupleToOption : option<DateTime>
  let toFileName (x:DateTime)=
    let timestr = x.ToString "yyyy-MM-ddTHH_mm_ss"
    sprintf "%s.json" timestr
