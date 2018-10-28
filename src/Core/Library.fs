namespace Core
open System
open System.IO
open FSharp.Data

module Utv=
    type Annons={
        id:string
        title:string
        url:string
        relevans:int
    }
    module Annons=
        let file_name (a:Annons) = sprintf "data/%s.json" a.id
        // /platsannonser/
        let url (a:Annons)= sprintf "http://api.arbetsformedlingen.se/af/v0/platsannonser/%s" a.id
// http://api.arbetsformedlingen.se/af/v0/platsannonser/matchning?yrkesid=80&sida=0&antalrader=1000
// Accept : application/json
// Accept-Language : sv
    type Platsannonser= JsonProvider<"./platsannonser_yrkesid_80.json">
    let platsannonser =Platsannonser.Load "./platsannonser_yrkesid_80.json"
    let matchingsdata= platsannonser.Matchningslista.Matchningdata
                        |> Array.filter (fun md->md.YrkesbenamningId = 80 ) 
                        |> Array.map (fun md->{
                                            id= md.Annonsid.JsonValue.AsString()
                                            title = md.Annonsrubrik
                                            url = md.Annonsurl
                                            relevans = md.Relevans
                                         })
                                         
    let tryPull (a:Annons)=
        let fn = Annons.file_name a
        if not <| File.Exists fn then
            let u = Annons.url a
            File.WriteAllText( fn, Http.RequestString( url= u, httpMethod="GET",
                                                       headers = [ "Accept", "application/json"; "Accept-Language", "sv"]
                                                       ))

    let print ()=
        for a in matchingsdata do
            printfn "%s : %s" a.id a.title
    let getAll ()=
        for a in matchingsdata do
            tryPull a
module Annons=
  type T=JsonProvider<"./sample_22898479.json">
  let t = T.Load "./sample_22898479.json"
  
module Main=

    [<EntryPoint>]
    let main argv =
        Utv.getAll()
        0 // return an integer exit code
