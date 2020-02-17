module Stacka.Repositories
open System
open System.IO
open System.Threading.Tasks
open System.Net
open FSharpPlus

type IAdRepository=interface
  abstract member Get: string -> Async<RawAd option>
  abstract member List: unit -> Async<RawAd list>
  abstract member Contains: string -> Async<bool>
  abstract member Store: string*RawAd -> Async<unit>
end
/// Repository in filesystem
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
/// Repository on top of S3 bucket
open Amazon.S3
open Amazon.S3.Model
let s3 bucketName maxKeys (s3Factory:(unit -> AmazonS3Client))=
  let keyname id = sprintf "ad_%s.json" id
  let getObjectReq id=
    let req = GetObjectRequest()
    req.BucketName <- bucketName
    req.Key <- keyname id
    req
  { new IAdRepository with
    member __.Get id = async{
      use s3 = s3Factory()
      let req = getObjectReq id
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

/// Repository on top of SQL
open System.Data
open System.Data.Common

let sql (sqlConnFactory:(unit -> #DbConnection))=
  let addParameter (cmd: #IDbCommand) (name,value: #obj,dbType:DbType) =
    let p=cmd.CreateParameter()
    p.Value <- value
    p.DbType <- dbType
    p.ParameterName <- name
    cmd.Parameters.Add p |> ignore
  let execCmdReader func (cmd: #DbCommand) =async{
    let! reader =Async.AwaitTask(cmd.ExecuteReaderAsync())
    try
      return! func reader
    finally reader.Dispose()
  }
  let readOnce func (reader: #DbDataReader) =async{
    let! any = Async.AwaitTask(reader.ReadAsync())
    if any then
      return Some <| func reader
    else
      return None
  }
  let readAll func (reader: #DbDataReader) =async{
    let mutable any=true
    let array = ResizeArray()
    while any do
      let! read= Async.AwaitTask(reader.ReadAsync())
      any<-read
      if any then
        func reader |> array.Add
    return List.ofSeq array
  }
  let execCmd (cmd: #DbCommand) =
    Async.map ignore <| Async.AwaitTask(cmd.ExecuteNonQueryAsync())

  { new IAdRepository with
    member __.Get id = async{
      use conn = sqlConnFactory()
      use cmd = conn.CreateCommand()
      cmd.CommandText <- "SELECT source FROM arbetsformedlingen.ads WHERE id = @Id"
      addParameter cmd ("@Id", id, DbType.AnsiString)
      let read (reader:DbDataReader)= RawAd <| reader.GetString(0)
      return! execCmdReader (readOnce read) cmd
    }
    member __.List() =async{
      use conn = sqlConnFactory()
      use cmd = conn.CreateCommand()
      cmd.CommandText <- "SELECT source FROM arbetsformedlingen.ads"
      let read (reader:DbDataReader)= RawAd <| reader.GetString(0)
      return! execCmdReader (readAll read) cmd
    }
    member __.Contains id = async{
      use conn = sqlConnFactory()
      use cmd = conn.CreateCommand()
      cmd.CommandText <- "SELECT 1 FROM arbetsformedlingen.ads WHERE id = @Id"
      addParameter cmd ("@Id", id, DbType.AnsiString)
      let! v= execCmdReader (readOnce ignore) cmd
      return Option.isSome v
    }
    member __.Store(id, RawAd ad)= async{
      use conn = sqlConnFactory()
      use cmd = conn.CreateCommand()
      cmd.CommandText <- "INSERT INTO arbetsformedlingen.ads (id, source) VALUES (@Id, @Source)"
      addParameter cmd ("@Id", id, DbType.AnsiString)
      addParameter cmd ("@Source", ad, DbType.String)
      return! execCmd cmd
    }
  }