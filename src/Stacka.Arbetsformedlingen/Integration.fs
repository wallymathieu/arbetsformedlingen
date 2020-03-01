module Stacka.Arbetsformedlingen.Integration
open System
open System.IO
open FSharp.Data
open Stacka
let commonHeaders = [ "Accept", "application/json"; "Accept-Language", "sv"]
/// also known as Ad
type Annons = Ad

module Annons=
  // /platsannonser/
  let url (a:Annons)= sprintf "http://api.arbetsformedlingen.se/af/v0/platsannonser/%s" a.id
  let tryDownload a=async{
    let u = url a
    try
      let! res = Http.AsyncRequestString( url= u, httpMethod="GET", headers = commonHeaders)
      return Ok res
    with
    | :? System.Net.WebException as ex->
      return Error (string ex.Status, ex.Message)
  }
  type Complete=JsonProvider<"./sample_22898479.json">

  let mapComplete (a:Complete.Annons) : Annons WithText= (
    ({id=a.Annonsid; title=a.Annonsrubrik; url=a.PlatsannonsUrl; relevans=100; source="AMS"}),
    a.Annonstext)

type Platsannonser= JsonProvider<"./platsannonser_yrkesid_80.json">

module Platsannonser=
  type Query = {
    /// Län id, literally means fief, but in Sweden is referred to as county
    LanId:int option
    /// Municipality
    KommunId:int option
    NyckelOrd:string option
    /// Occupation id
    YrkesId:int option
    /// page
    Sida:int option
    /// number of rows
    AntalRader: int option
  }
  with
    member this.ToQuery()=
      let intOptions =
        [
            "lanid", this.LanId
            "kommunid", this.KommunId
            "yrkesid",this.YrkesId
            "sida",this.Sida
            "antalrader",this.AntalRader
        ]
      let stringOptions =
        [
            "nyckelord", this.NyckelOrd
        ]
      intOptions
      |> List.map (fun (k, v)-> (k, Option.map string v))
      |> List.append stringOptions
      |> List.choose (fun (key,value)-> value |> Option.map(fun v-> key,v))
      |> List.map (fun (key,value)-> sprintf "%s=%s" key (Uri.EscapeDataString value))
      |> String.concat "&"
  let defaultQuery = {LanId=None; YrkesId=None; Sida=None; AntalRader=None; KommunId=None; NyckelOrd=None}
  let query (q: Query)=async{
    //platsannonser/matchning?lanid={M}&kommunid={M}&yrkesid={M}& nyckelord={M}&sida={V}&antalrader={V}
    let u = "http://api.arbetsformedlingen.se/af/v0/platsannonser/matchning?"+q.ToQuery()
    let! res = Http.AsyncRequestString( url= u, httpMethod="GET", headers = commonHeaders)
    return Platsannonser.Parse res
  }
  let mapToAnnons (md:Platsannonser.Matchningdata) : Annons=
    {
      id= md.Annonsid
      title = md.Annonsrubrik
      url = md.Annonsurl
      relevans = md.Relevans
      source = "AMS"}
