using System.Reflection;
using Neo4j.Berries.OGM.Interfaces;
using Neo4j.Berries.OGM.Models.Config;

namespace Neo4j.Berries.OGM.Contexts;

internal class Neo4jSingletonContext
{
    private readonly Assembly[] _assemblies;

    public static bool EnforceIdentifiers { get; internal set; }
    internal static TimestampConfiguration TimestampConfiguration { get; set; } = new TimestampConfiguration();
    internal static Dictionary<string, NodeConfiguration> Configs { get; private set; } = [];
    internal static Func<string, string> PropertyCaseConverter { get; set; } = (x) => x;
    public Neo4jSingletonContext(params Assembly[] assemblies)
    {
        _assemblies = assemblies;
        ParseAssemblyForConfigurations();
    }
    public Neo4jSingletonContext(OGMConfigurationBuilder builder)
    {
        _assemblies = builder.Assemblies;
        EnforceIdentifiers = builder.EnforceIdentifiers;
        PropertyCaseConverter = builder.PropertyCaseConverter ?? PropertyCaseConverter;
        TimestampConfiguration = builder.TimestampConfiguration ?? TimestampConfiguration;
        ParseAssemblyForConfigurations();
        foreach (var config in builder.NodeSetConfigurations)
        {
            Configs[config.Key] = config.Value;
        }
    }
    private void ParseAssemblyForConfigurations()
    {
        var interfaceType = typeof(INodeConfiguration<>);
        var configTypes = _assemblies.SelectMany(x => x.GetTypes())
            .Where(x => x.GetInterfaces().Any(y => y.IsGenericType && y.GetGenericTypeDefinition() == interfaceType))
            .Select(x => new
            {
                Interface = x.GetInterfaces().First(y => y.IsGenericType && y.GetGenericTypeDefinition() == interfaceType),
                Instance = x,
                x.Name
            });
        foreach (var configType in configTypes)
        {
            var genericArguments = configType.Interface.GetGenericArguments();
            if (genericArguments.Length > 1 || genericArguments.Length == 0)
            {
                throw new InvalidOperationException($"Invalid number of generic arguments on {configType.Name}");
            }

            var builder = Activator.CreateInstance(typeof(NodeTypeBuilder<>).MakeGenericType(genericArguments[0]));
            var config = Activator.CreateInstance(configType.Instance);
            var configureMethod = configType.Instance.GetMethod(nameof(INodeConfiguration<object>.Configure));
            configureMethod.Invoke(config, [builder]);
        }

    }
}