using System.Collections.Concurrent;
using System.Linq.Expressions;
using Neo4j.Berries.OGM.Contexts;
using Neo4j.Berries.OGM.Enums;

namespace Neo4j.Berries.OGM.Models.Config;

public class NodeTypeBuilder<TNode>
where TNode : class
{
    private static string TypeName => typeof(TNode).Name;
    private static NodeConfiguration Config
    {
        get
        {
            if (!Neo4jSingletonContext.Configs.TryGetValue(TypeName, out NodeConfiguration value))
            {
                value = new NodeConfiguration();
                Neo4jSingletonContext.Configs[TypeName] = value;
            }
            return value;
        }
    }

    /// <summary>
    /// The property will be used as an identifier for the node.
    /// </summary>
    /// <remarks>
    /// The identifier is used to find the node in the database and the value for the identifier must not be null.
    /// </remarks>
    public NodeTypeBuilder<TNode> HasIdentifier<TProperty>(Expression<Func<TNode, TProperty>> expression)
    {
        var propertyName = ((MemberExpression)expression.Body).Member.Name;
        propertyName = Neo4jSingletonContext.PropertyCaseConverter(propertyName);
        Config.Identifiers.Add(propertyName);
        return this;
    }

    /// <summary>
    /// The property will be used to create a relation with another nodes
    /// </summary>
    public RelationConfiguration<TNode, TProperty> HasRelationWithMultiple<TProperty>(Expression<Func<TNode, IEnumerable<TProperty>>> expression, string label, RelationDirection direction)
    where TProperty : class
    {
        var propertyName = ((MemberExpression)expression.Body).Member.Name;
        var relationConfig = new RelationConfiguration<TNode, TProperty>(label, direction) { Property = propertyName, IsCollection = true };
        Config.Relations[propertyName] = relationConfig;
        Exclude(expression);
        return relationConfig;
    }
    /// <summary>
    /// The property will be used to create a relation with another node
    /// </summary>
    public RelationConfiguration<TNode, TProperty> HasRelationWithSingle<TProperty>(Expression<Func<TNode, TProperty>> expression, string label, RelationDirection direction)
    where TProperty : class
    {
        var propertyName = ((MemberExpression)expression.Body).Member.Name;
        var relationConfig = new RelationConfiguration<TNode, TProperty>(label, direction) { Property = propertyName };
        Config.Relations[propertyName] = relationConfig;
        Exclude(expression);
        return relationConfig;
    }

    /// <summary>
    /// The property will be used to create a relation with another node
    /// </summary>
    public RelationConfiguration<TNode, TProperty> HasRelationWithSingle<TProperty>(Expression<Func<TNode, TProperty>> expression, RelationConfiguration<TNode, TProperty> configuration)
    where TProperty : class
    {
        var propertyName = ((MemberExpression)expression.Body).Member.Name;
        configuration.Property = propertyName;
        Config.Relations[propertyName] = configuration;
        Exclude(expression);
        return configuration;
    }
    /// <summary>
    /// The property will be used to create a relation with another nodes
    /// </summary>
    public RelationConfiguration<TNode, TProperty> HasRelationWithMultiple<TProperty>(Expression<Func<TNode, IEnumerable<TProperty>>> expression, RelationConfiguration<TNode, TProperty> configuration)
    where TProperty : class
    {
        var propertyName = ((MemberExpression)expression.Body).Member.Name;
        configuration.Property = propertyName;
        configuration.IsCollection = true;
        Config.Relations[propertyName] = configuration;
        Exclude(expression);
        return configuration;
    }

    /// <summary>
    /// The property will be included in the node. If exclude is used, include will be ignored
    /// </summary>
    public void Include<TProperty>(params Expression<Func<TNode, TProperty>>[] expressions)
    {
        var removeFromExclude = new List<string>();
        expressions
                .Select(x => Neo4jSingletonContext.PropertyCaseConverter(((MemberExpression)x.Body).Member.Name)).ToList()
                .ForEach(x =>
                {
                    Config.IncludedProperties.Add(x);
                    if (Config.ExcludedProperties.Any(e => e == x))
                    {
                        removeFromExclude.Add(x);
                    }
                });

        if (removeFromExclude.Any())
        {
            ConcurrentBag<string> newExclude = new();
            foreach(var x in Config.ExcludedProperties)
            {
                if (!removeFromExclude.Contains(x))
                {
                    newExclude.Add(x);
                }
            }
            Config.ExcludedProperties = newExclude;
        }
    }

    /// <summary>
    /// The property will be excluded from the node. If this is used the include cannot be used
    /// </summary>
    public void Exclude<TProperty>(params Expression<Func<TNode, TProperty>>[] expressions)
    {
        expressions
                .Select(x => Neo4jSingletonContext.PropertyCaseConverter(((MemberExpression)x.Body).Member.Name)).ToList()
                .ForEach(x =>
                {
                    if (!Config.ExcludedProperties.Contains(x))
                    {
                        Config.ExcludedProperties.Add(x);
                    }
                });
    }
}