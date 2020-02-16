module RepositoriesTests

open System
open Xunit
open Stacka
open System.IO

module Fs=
  [<Fact>]
  let ``Read persisted items``()=Async.StartAsTask(async{
    let adT = typeof<Stacka.Ad>
    let dir = Path.GetFullPath(Path.Combine(adT.Assembly.Location,
                                "..", "..", "..", "..", "..","..",
                                "tmp", "fs", Guid.NewGuid().ToString("N")))
    Directory.CreateDirectory(Path.Combine(dir, "data")) |> ignore
    let _persist = Repositories.fileSystem dir
    for i = 1 to 100 do
      do! _persist.Store(string i, RawAd << string <| i)

    let! list = _persist.List()
    Assert.Equal(100, list |> List.length)
  })