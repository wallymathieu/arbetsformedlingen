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
