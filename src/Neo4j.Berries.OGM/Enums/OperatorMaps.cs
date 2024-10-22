namespace Neo4j.Berries.OGM.Enums;

public class OperatorMaps {
#pragma warning disable CA2211 // Non-constant fields should not be visible
    public static Dictionary<ComparisonOperator, string> ComparisonOperatorMap = new() {
#pragma warning restore CA2211 // Non-constant fields should not be visible
        { ComparisonOperator.Equals, "=" },
        { ComparisonOperator.NotEquals, "<>" },
        { ComparisonOperator.GreaterThan, ">" },
        { ComparisonOperator.GreaterThanOrEquals, ">=" },
        { ComparisonOperator.LessThan, "<" },
        { ComparisonOperator.LessThanOrEquals, "<=" }
    };

    public static Dictionary<StringComparisonOperator, string> StringComparisonOperatorMap = new()
    {
        { StringComparisonOperator.Contains, "CONTAINS" },
        { StringComparisonOperator.StartsWith, "STARTS WITH" },
        { StringComparisonOperator.EndsWith, "ENDS WITH" },
        { StringComparisonOperator.IsNormalized, "IS NORMALIZED" },
    };
}