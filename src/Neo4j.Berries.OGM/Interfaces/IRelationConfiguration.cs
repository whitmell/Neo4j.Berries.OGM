using Neo4j.Berries.OGM.Enums;
using System.Reflection;

namespace Neo4j.Berries.OGM.Interfaces;

public interface IRelationConfiguration
{
    string Label { get; }
    RelationDirection Direction { get; }
    string[] EndNodeLabels { get; }
    IEnumerable<string> EndNodeMergeProperties { get; }
    string Property { get; }
    bool IsCollection { get; }
    bool KeepHistory { get; set; }
}