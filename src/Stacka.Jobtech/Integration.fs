module Stacka.Jobtech.Integration
open System
open System.IO
open FSharp.Data
open Stacka
let commonHeaders = [ "Accept", "application/json"; "Accept-Language", "sv"]
/// also known as Ad
type Annons = Ad
type JsonT=JsonProvider<"./sample_10.json">

module Annons=
  let id (a:Annons)=a.id
  // /platsannonser/
  type JBAd=JsonT.Root
  let isRemoved (a:JBAd) = a.Removed
  let mapFromJobtech (a:JBAd) : Annons WithText= (
    ({id=string a.Id; title=Option.defaultValue "" a.Headline; url=Option.defaultValue "" a.WebpageUrl
      relevans=10; source=Option.defaultValue "" a.SourceType}),
    match a.Description with |Some desc->desc.Text|None->"")

type Platsannonser = {RawStream: string}
with
  member x.Parse = JsonT.Parse x.RawStream
  static member Create res= {RawStream=res}
module Platsannonser=

  let stream (q: DateTime)=async{
    let apiKey=Environment.GetEnvironmentVariable "JOBTECHDEV_APIKEY"
    let u = "https://jobstream.api.jobtechdev.se/stream?date="+q.ToString("s")
    let! res = Http.AsyncRequestString( url= u, httpMethod="GET", headers = ["api-key", apiKey]@commonHeaders)
    return Platsannonser.Create res
  }
