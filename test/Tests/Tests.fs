module Tests

open System
open Xunit
open Stacka

module Fs=
  [<Fact>]
  let ``Get datetime from filename``()=
    let filename = "~/Dropbox/Statistics/Arbetsformedlingen/jobtech/2020-02-28T01_01_01.json"
    let time = DateTime.fromFileName filename
    Assert.Equal(DateTime(2020,2,28,1,1,1) |> Some , time)
  [<Fact>]
  let ``When there is no datetime``()=
    let filename = "~/Dropbox/Statistics/Arbetsformedlingen/jobtech/somethinge_else.json"
    let time = DateTime.fromFileName filename
    Assert.Equal(None, time)


module TermsRelatedToLanguages=
  open Stacka.Languages
  open Stacka.AdsAndLanguages
  module AL= Stacka.AdsAndLanguages.AdAndLanguage
  open FSharpPlus
  type AdS={
    id:string
    title:string}

  let annonserMedTexter= [
    {id="1";title="En glad utvecklare"},"Vi söker en c#-utvecklare till en position. Vi behöver en duktig ERP-specialist med kunskaper om databaser och BI. C#"
    {id="2";title="Enterprise"},"Välkommen till att söka en systemutvecklarposition på ett bolag i framkanten. Java C#"
    {id="3";title="Business"},"Frontendare sökes till spännande uppdrag i framkanten. Javascript, C#, Webb"
  ]

  //
  let assertEq (exp:#seq< 't>,act: seq< 't>)=Assert.Equal(exp , act)

  [<Fact>]
  let ``Given corpus with c# mentioned``()=
    let minNum (_,v) = v > 2
    let adsWithLanguages = List.map AL.ofAnnonsWithText annonserMedTexter
    let adsWithWords = List.map AdAndText.toIdAndWords annonserMedTexter
    let res = AL.countLangAndWords adsWithWords adsWithLanguages |> List.filter minNum
    let exp =[(("c#", "till"), 3); ]
    assertEq(exp , res)
