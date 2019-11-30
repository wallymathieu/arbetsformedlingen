// Learn more about F# at http://fsharp.org

open System

open FluentMigrator
open FluentMigrator
[<Migration(201911301052L)>]
type AddTablePlatsannonser()=
    inherit AutoReversingMigration()

    default self.Up()=
        self.Create.Table("Platsannonser")
            .WithColumn("annons_id").AsString().NotNullable().PrimaryKey()
            .WithColumn("annons_url").AsString().NotNullable()
            .WithColumn("annons_rubrik").AsString().NotNullable()
            .WithColumn("annons_text").AsString().NotNullable()
            .WithColumn("annons_yrkesbenamning").AsString().NotNullable()
            .WithColumn("annons_yrkesid").AsInt64().NotNullable()
            .WithColumn("annons_publiceraddatum").AsDateTime().NotNullable()
            .WithColumn("annons_antal_platser").AsInt32().NotNullable()
            .WithColumn("annons_kommunnamn").AsString().NotNullable()
            .WithColumn("annons_kommunkod").AsInt64().NotNullable()
            .WithColumn("annons_anstallningstyp").AsString().NotNullable()
            .WithColumn("villkor_varaktighet").AsString().NotNullable()
            .WithColumn("villkor_arbetstid").AsString().NotNullable()
            .WithColumn("villkor_lonetyp").AsString().NotNullable()
            .WithColumn("ansokan_referens").AsString().NotNullable()
            .WithColumn("ansokan_ovrigt_om_ansokan").AsString().NotNullable()
            .WithColumn("arbetsplats_arbetsplatsnamn").AsString().NotNullable()
            .WithColumn("arbetsplats_postnummer").AsString().NotNullable()
            .WithColumn("arbetsplats_postadress").AsString().NotNullable()
            .WithColumn("arbetsplats_postort").AsString().NotNullable()
            .WithColumn("arbetsplats_postland").AsString().NotNullable()
            .WithColumn("arbetsplats_land").AsString().NotNullable()
            .WithColumn("arbetsplats_besoksadress").AsString().NotNullable()
            .WithColumn("arbetsplats_hemsida").AsString().NotNullable()
            .WithColumn("arbetsplats_besoksort").AsString().NotNullable()
            |> ignore
module MigrationRunner=
  open FluentMigrator.Runner
  open FluentMigrator.Runner.Processors
  open Microsoft.Extensions.DependencyInjection
  open Microsoft.Extensions.Logging

  let create (connection:string) processor=
      let serviceProvider = ServiceCollection()
                              .AddLogging(fun lb -> lb.AddDebug().AddFluentMigratorConsole() |> ignore)
                              .AddFluentMigratorCore()
                              .ConfigureRunner(
                                  fun builder -> builder
                                                  .AddSQLite()
                                                  .AddSqlServer()
                                                  .AddPostgres()
                                                  .WithGlobalConnectionString(connection)
                                                  .ScanIn(typeof<AddTablePlatsannonser>.Assembly)
                                                      .For.Migrations() |> ignore)
                              .Configure(
                                  fun (opt:SelectingProcessorAccessorOptions) -> opt.ProcessorId <- processor)
                              .BuildServiceProvider()

      // Instantiate the runner
      serviceProvider.GetRequiredService<IMigrationRunner>()
module EF=
  open Microsoft.EntityFrameworkCore
  open System.Linq
  open System.Runtime.CompilerServices
  open System.Threading.Tasks
  open FSharp.Control.Tasks.Builders
  open Microsoft.EntityFrameworkCore.Storage.ValueConversion
  open Microsoft.FSharp.Linq.RuntimeHelpers
  open System.Linq.Expressions
  [<AllowNullLiteral>]
  type Platsannonser()=
    member val annons_id : String = "" with get, set
    member val annons_url : String = "" with get, set
    member val annons_rubrik : String = "" with get, set
    member val annons_text : String = "" with get, set
    member val annons_yrkesbenamning : String = "" with get, set
    member val annons_yrkesid : Int64 = 0L with get, set
    member val annons_publiceraddatum : DateTime = DateTime() with get, set
    member val annons_antal_platser : Int32 = 0 with get, set
    member val annons_kommunnamn : String = "" with get, set
    member val annons_kommunkod : Int64 = 0L with get, set
    member val annons_anstallningstyp : String = "" with get, set
    member val villkor_varaktighet : String = "" with get, set
    member val villkor_arbetstid : String = "" with get, set
    member val villkor_lonetyp : String = "" with get, set
    member val ansokan_referens : String = "" with get, set
    member val ansokan_ovrigt_om_ansokan : String = "" with get, set
    member val arbetsplats_arbetsplatsnamn : String = "" with get, set
    member val arbetsplats_postnummer : String = "" with get, set
    member val arbetsplats_postadress : String = "" with get, set
    member val arbetsplats_postort : String = "" with get, set
    member val arbetsplats_postland : String = "" with get, set
    member val arbetsplats_land : String = "" with get, set
    member val arbetsplats_besoksadress : String = "" with get, set
    member val arbetsplats_hemsida : String = "" with get, set
    member val arbetsplats_besoksort : String = "" with get, set

  [<Interface>]
  type ICoreDbContext=
      abstract member Platsannonser: DbSet<Platsannonser> with get
      abstract member SaveChangesAsync: unit->Task<unit>
      abstract member AddAsync<'t> : 't -> Task<unit>
  type CoreDbContext(options:DbContextOptions)=
      inherit DbContext(options)
      default __.OnModelCreating(modelBuilder:ModelBuilder)=
          modelBuilder.Entity<Platsannonser>().HasKey("annons_id") |> ignore
          base.OnModelCreating(modelBuilder)

      [<DefaultValue>]val mutable private platsannonser: DbSet<Platsannonser>
      member this.Platsannonser with get()=this.platsannonser and set v = this.platsannonser<-v

      interface ICoreDbContext with
        member this.SaveChangesAsync() = task { let! _ = this.SaveChangesAsync()
          return () }
        member this.AddAsync(t) = task { let! _ = this.AddAsync t
          return () }
        member this.Platsannonser = this.platsannonser

type CmdArgs =
  { connection : string
    processor : string
    operation : string
  }

[<EntryPoint>]
let main argv =
    let defaultArgs =
      { connection= Environment.GetEnvironmentVariable("STACKA_DB_CONN")
        processor = "SqlServer2016"
        operation = "migrate"
      }
    let printHelp () =
      printfn "Usage:"
      printfn "    --connection connection_string (Default: %s)" defaultArgs.connection
      printfn "    --processor processor_id (Default: %s)" defaultArgs.processor
      printfn "    --operation operation (Default: %s)" defaultArgs.operation
      exit 1
    let rec parseArgs b args =
      match args with
      | [] -> b
      | "--connection" :: connection :: xs -> parseArgs { b with connection = connection } xs
      | "--processor" :: processor :: xs -> parseArgs { b with processor = processor } xs
      | "--operation" :: operation :: xs -> parseArgs { b with operation = operation } xs
      | invalidArgs ->
        printfn "error: invalid arguments %A" invalidArgs
        printHelp()

    let args = argv
              |> List.ofArray
              |> parseArgs defaultArgs

    // Instantiate the runner
    let runner = MigrationRunner.create args.connection args.processor
    match args.operation with
    | "migrate" ->
      runner.MigrateUp()
      0
    | _ ->
(* NOTE: To create db, run
CREATE DATABASE [ef-core-studies-fsharp]
*)
      printHelp()
      1 // return an integer exit code
