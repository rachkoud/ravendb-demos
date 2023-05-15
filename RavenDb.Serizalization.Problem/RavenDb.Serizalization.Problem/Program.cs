// See https://aka.ms/new-console-template for more information

using Microsoft.TeamFoundation.SourceControl.WebApi;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using RavenDb.Serizalization.Problem;

var ravenOptions = new RavenOptions()
{
    Database = "DemoSerializationProblem",
    Urls = new List<string>() { "http://127.0.0.1:8181" }
};

// With this jsonContractResolver we're saying ignore 
// [DataMember(Name = "commitId", EmitDefaultValue = false)] or [JsonProperty("property_x")]
// when serializing
var serializationConventions = new NewtonsoftJsonSerializationConventions();
serializationConventions.JsonContractResolver = new CustomizedRavenJsonContractResolver(serializationConventions);

var storeHolderWithSerializationFix = new StoreHolder(ravenOptions, new DocumentConventions()
{
    Serialization = serializationConventions,
    FindIdentityProperty = x => x.Name == "CommitId" || x.Name == "Id",
    // This is the tricks to fix the query generation and capitalize each letter instead of using the 
    // [DataMember(Name = "commitId", EmitDefaultValue = false)] or [JsonProperty("property_x")]
    FindPropertyNameForDynamicIndex = (indexedType, indexedName, path, prop) =>
    {
        if (indexedType == typeof(GitCommitRef))
        {
            // Capitalize first letter of the prop 
            // author.email becomes Author.Email
            string[] properties = prop.Split('.');
            for (int i = 0; i < properties.Length; i++)
            {
                if (!string.IsNullOrEmpty(properties[i]))
                {
                    properties[i] = char.ToUpper(properties[i][0]) + properties[i].Substring(1);
                }
            }
                    
            return string.Join(".", properties);
        }
                    
        return DocumentConventions.DefaultFindPropertyNameForDynamicIndex(indexedType, indexedName, path, prop);
    }
});
using var session1 = storeHolderWithSerializationFix.Store.OpenAsyncSession();

// Save some data
var email = "author@demo.com";
var comment = "Dummy comment";

var gitRefCommit = new GitCommitRef()
{
    CommitId = Guid.NewGuid().ToString(),
    Comment = comment,
    Author = new GitUserDate()
    {
        Email = email,
        Date = DateTime.Now
    }
};

await session1.StoreAsync(gitRefCommit);
await session1.SaveChangesAsync();

// We have results now with the FindPropertyNameForDynamicIndex fix :
// Search commits query : from 'GitCommitRefs' where Author.Date between $p0 and $p1 and Author.Email = $p2
// Commits by author and date range count : 2
await SearchCommitsAsync(session1);

var storeHolderWithoutConventions = new StoreHolder(ravenOptions, new DocumentConventions());
using var session2 = storeHolderWithoutConventions.Store.OpenAsyncSession();

// We haven't any results without the with the FindPropertyNameForDynamicIndex fix : 
// Search commits query : from 'GitCommitRefs' where author.date between $p0 and $p1 and author.email = $p2
// Commits by author and date range count : 0
await SearchCommitsAsync(session2);

async Task SearchCommitsAsync(IAsyncDocumentSession session)
{
    var commitsQuery = session.Query<GitCommitRef>()
        .Where(w =>
            w.Author.Date >= DateTime.Today.AddDays(-2) &&
            w.Author.Date <= DateTime.Today.AddDays(1) &&
            w.Author.Email == email);

    var commitsQueryString = commitsQuery.ToString();
    Console.WriteLine($"Search commits query : {commitsQueryString}");
    var commits = await commitsQuery.ToListAsync();
    Console.WriteLine($"Commits by author and date range count : {commits.Count}");
}