module Tests

open System
open Xunit
open Amazon.S3
open Amazon
open Stacka
module S3=
  open Amazon.S3.Model

  let createBucket bucketName (s3:AmazonS3Client)=
    let req = PutBucketRequest()
    req.BucketName <- bucketName
    req.UseClientRegion <- true
    s3.PutBucketAsync(req)
  let deleteBucket bucketName (s3:AmazonS3Client)=
    let req = DeleteBucketRequest()
    req.BucketName <- bucketName
    req.UseClientRegion <- true
    s3.DeleteBucketAsync(req)

// https://www.minio.io/kubernetes.html
// https://docs.minio.io/docs/how-to-use-aws-sdk-for-net-with-minio-server.html
  let s3Factory ()=
    let accessKey="testkey"
    let secretKey="secretkey"
    let config = AmazonS3Config()
    // MUST set this before setting ServiceURL and it should match the `MINIO_REGION` enviroment variable.
    config.RegionEndpoint <- RegionEndpoint.USEast1
    // replace http://localhost:9000 with URL of your minio server
    config.ServiceURL <- "http://localhost:9000"
    // MUST be true to work correctly with Minio server
    config.ForcePathStyle <- true
    new AmazonS3Client(accessKey, secretKey, config)

  [<Fact>]
  let ``Read persisted items``()=Async.StartAsTask(async{
    let bucket = "test-1-json-container-"+Guid.NewGuid().ToString("N")
    let! _ =
      use s3 = s3Factory()
      Async.AwaitTask(createBucket bucket s3)

    let _persist = Repositories.s3 bucket 10 s3Factory
    for i = 1 to 100 do
      do! _persist.Store(string i, RawAd << string <| i)

    let! list = _persist.List()
    Assert.Equal(100, list |> List.length)
  })
