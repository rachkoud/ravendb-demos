using System.Reflection;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations.Revisions;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace RavenDb.Serizalization.Problem;

public class StoreHolder
{
    public StoreHolder(RavenOptions ravenOptions, DocumentConventions documentConventions)
    {
        Store = new DocumentStore
        {
            Urls = ravenOptions.Urls.ToArray(),
            Database = ravenOptions.Database,
            Conventions = documentConventions
        };

        Store.Initialize();
        
        // Try to retrieve a record of this database
        var databaseRecord =
            Store.Maintenance.Server.Send(
                new GetDatabaseRecordOperation(Store.Database));

        if (databaseRecord == null)
        {
            var createDatabaseOperation =
                new CreateDatabaseOperation(
                    new DatabaseRecord(Store.Database));

            Store.Maintenance.Server.Send(createDatabaseOperation);
        }
    }

    public IDocumentStore Store { get; }
}