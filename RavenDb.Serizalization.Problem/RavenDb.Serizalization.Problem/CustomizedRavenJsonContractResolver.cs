using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Raven.Client.Json.Serialization;
using Raven.Client.Json.Serialization.NewtonsoftJson;

public class CustomizedRavenJsonContractResolver : DefaultRavenContractResolver
{
    public CustomizedRavenJsonContractResolver(ISerializationConventions conventions) : base(conventions)
    {
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var jsonProperty = base.CreateProperty(member, memberSerialization);
        jsonProperty.PropertyName = jsonProperty.UnderlyingName;
        return jsonProperty;
    }
}