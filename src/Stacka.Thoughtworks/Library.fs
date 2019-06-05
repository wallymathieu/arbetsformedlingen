namespace Stacka.Thoughtworks
open System
open FSharp.Data

module private Internal=
  type T =HtmlProvider<"./A-Z_Technology_Radar_ThoughtWorks.html">


module Say =
//A-Z_Technology_Radar_ThoughtWorks.html
    let hello name =
        printfn "Hello %s" name
