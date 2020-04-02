module Tests

open System
open Xunit
open Stacka
open Npgsql
module PG=
  let getConnection ()=
    let conn = new NpgsqlConnection("Server=localhost;Database=stacka;User id=stacka;Password=STACKA_TEST_PASSWORD;Port=5432;CommandTimeout=200;")
    conn.Open()
    conn


  [<Fact>]
  let ``Read persisted items``()=Async.StartAsTask(async{
    let _persist = Repositories.sql getConnection
    for i = 1 to 100 do
      do! _persist.Store(string i, RawAd << string <| i)

    let! list = _persist.List()
    Assert.Equal(100, list |> List.length)
  })
