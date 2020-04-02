namespace Stacka.SQL.Migrations
open FluentMigrator
[<Migration(202002162037L)>]
type AddTables()=
    inherit AutoReversingMigration()

    default self.Up()=
        self.Create.Schema("arbetsformedlingen") |> ignore

        self.Create.Table("ads")
            .InSchema("arbetsformedlingen")
            .WithColumn("id").AsAnsiString().NotNullable().PrimaryKey()
            .WithColumn("source").AsString()
            |> ignore

