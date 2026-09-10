namespace TaladPOS.Api.IntegrationTests;

/// <summary>Shares one <see cref="TaladPosApiFactory"/> (and one database reset) across every
/// test class in this project — resetting per-class would be needlessly slow.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<TaladPosApiFactory>
{
    public const string Name = "Api";
}
