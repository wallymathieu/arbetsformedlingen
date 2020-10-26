module Stacka.IO
open System.IO
  module File=
    let readAllTextAsync (file:string) = async{
      use f=File.OpenText file
      return! Async.AwaitTask(f.ReadToEndAsync()) }
    let writeAllTextAsync (file:string) (content:string) =
      Async.AwaitTask ( File.WriteAllTextAsync (file, content) )
    