module Tests

open System
open Xunit
open Stacka.Text

[<Theory>]
[<InlineData("embeddedutvecklare","embedded-_developer_")>]
[<InlineData("fullstackutvecklare","_fullstack_-_developer_")>]
[<InlineData("frontendutvecklare","_frontend_-_developer_")>]
let ``Can use alias`` (from,to') =
  Assert.Equal(to', normalize from)
