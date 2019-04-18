module Stacka.Repositories
open System
open System.IO
open System.Threading.Tasks
open System.Net

open Amazon.S3
open Amazon.S3.Model

type IAdRepository=interface
  abstract member Get: string -> Async<RawAd option>
  abstract member List: unit -> Async<RawAd list>
  abstract member Contains: string -> Async<bool>
  abstract member Store: string*RawAd -> Async<unit>
end

let fileSystem dir=
  let filename id = Path.Combine(dir, "data", sprintf "%s.json" id)
  let readAllTextAsync (file:string) = async{
    use f=File.OpenText file
    return! Async.AwaitTask(f.ReadToEndAsync()) }
  let writeAllTextAsync (file:string, content:string) = async{
    use f=File.OpenWrite file
    use w=new StreamWriter(f)
    return! Async.AwaitTask(w.WriteAsync(content)) }
  { new IAdRepository with
    member __.Get id = async{
      let file = filename id
      if File.Exists file then
        let content = File.ReadAllText file
        return if String.IsNullOrEmpty content then None else Some <| RawAd content
      else
        return None
    }
    member __.List() = async{
      let getFileContent (file:string) = readAllTextAsync file
      let files = ResizeArray()
      for file in Directory.GetFiles(Path.Combine(dir, "data"), "*.json") do
        let! content = getFileContent file
        RawAd content |> files.Add
      return (files |>  Seq.toList)
    }
    member __.Contains id = async{ return File.Exists (filename id) }
    member __.Store(id, RawAd ad) = writeAllTextAsync (filename id, ad)
  }
let s3 bucketName maxKeys (s3Factory:(unit -> AmazonS3Client))=
  let keyname id = sprintf "ad_%s.json" id
  { new IAdRepository with
    member __.Get id = async{
      use s3 = s3Factory()
      let req = GetObjectRequest()
      req.BucketName <- bucketName
      req.Key <- keyname id
      let! object =Async.AwaitTask(s3.GetObjectAsync(req))

      if object.HttpStatusCode = HttpStatusCode.OK then
        use r = new StreamReader( object.ResponseStream)
        let content =r.ReadToEnd()
        return if String.IsNullOrEmpty content then None else Some <| RawAd content
      else
        return None
    }
    member __.List() =
      let readFromStream (s:Stream)=
          use r = new StreamReader(s)
          let content = r.ReadToEnd()
          if String.IsNullOrEmpty content then None else Some <| RawAd content

      let readBatches () =
        let readObjects (req:ListObjectsRequest)=async{
          use s3 = s3Factory()
          let! objects =Async.AwaitTask(s3.ListObjectsAsync(req))
          let! items=
            objects.S3Objects
                  |> Seq.map (fun o->
                    let req = GetObjectRequest()
                    req.BucketName <- o.BucketName
                    req.Key <- o.Key;
                    s3.GetObjectAsync(req))
                  |> Task.WhenAll
                  |> Async.AwaitTask
          let ads = items|> Array.choose (fun i->readFromStream i.ResponseStream)
          if objects.IsTruncated then
            let nextReq = ListObjectsRequest()
            nextReq.BucketName <- req.BucketName
            nextReq.MaxKeys <- req.MaxKeys
            nextReq.Marker <- objects.NextMarker
            nextReq.Prefix <- req.Prefix
            return (ads, Some nextReq)
          else
            return (ads, None)
        }
        async {
          let req = ListObjectsRequest()
          req.Prefix <- "ad_"
          req.BucketName <- bucketName
          req.MaxKeys <- maxKeys

          let! (commands,maybeNext') = readObjects req
          let allCommands= ResizeArray<_>()
          allCommands.Add commands
          let mutable maybeNext = maybeNext'
          while (maybeNext.IsSome) do
            match maybeNext with
            | Some next ->
              let! (commands,maybeNext') = readObjects next
              allCommands.Add commands
              maybeNext <- maybeNext'
            | None -> ()
          return allCommands.ToArray() |> Array.concat |> Array.toList
        }
      readBatches()
    member __.Contains id = async{
      use s3 = s3Factory()
      let req = GetObjectRequest()
      req.BucketName <- bucketName
      req.Key <- keyname id
      let! object =Async.AwaitTask(s3.GetObjectAsync(req))
      return object.HttpStatusCode = HttpStatusCode.OK }
    member __.Store(id, RawAd ad)=async {
      use s3 = s3Factory()
      let req = PutObjectRequest()
      req.BucketName <- bucketName
      req.Key <- keyname id
      let ms = new MemoryStream()
      let w = new StreamWriter(ms)
      w.Write(ad)
      w.Flush()
      ms.Seek(0L, SeekOrigin.Begin) |> ignore
      req.InputStream <- ms
      let! _1 = Async.AwaitTask(s3.PutObjectAsync(req))
      return _1 |> ignore
    }
  }