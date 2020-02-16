namespace Stacka.SQL.Migrations
open FluentMigrator
open Stacka.SQL.Extensions
[<Migration(202002162037L)>]
type AddTables()=
    inherit AutoReversingMigration()

    default self.Up()=
        self.Create.Table("Ads")
            .InSchema("Arbetsformedlingen")
            .WithColumn("Id").AsAnsiString().NotNullable().PrimaryKey()
            .WithVersionColumn()
            .WithColumn("Source").AsString()
            |> ignore

