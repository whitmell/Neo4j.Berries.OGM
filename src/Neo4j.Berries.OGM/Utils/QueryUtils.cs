using System.Text;
using Neo4j.Berries.OGM.Contexts;
using Neo4j.Berries.OGM.Interfaces;

namespace Neo4j.Berries.OGM.Utils;

internal static class QueryUtils
{
    internal static StringBuilder BuildLockQuery(this StringBuilder builder, List<IMatch> matches)
    {
        return builder.AppendLines(
            $"SET {matches.First().StartNodeAlias}._LOCK_ = true"
        );
    }
    internal static StringBuilder BuildUnlockQuery(this StringBuilder builder, List<IMatch> matches)
    {
        return builder.AppendLines(
            $"REMOVE {matches.First().StartNodeAlias}._LOCK_"
        );
    }
    internal static StringBuilder BuildAnyQuery(this StringBuilder builder, List<IMatch> matches)
    {
        return builder.AppendLines(
            $"WITH DISTINCT {matches.First().StartNodeAlias}",
            $"RETURN count({matches.First().StartNodeAlias}) > 0 as any"
        );
    }
    internal static StringBuilder BuildCountQuery(this StringBuilder builder, List<IMatch> matches)
    {
        return builder.AppendLines(
            $"WITH DISTINCT {matches.First().StartNodeAlias}",
            $"RETURN count({matches.First().StartNodeAlias}) as count"
        );
    }
    internal static StringBuilder BuildFirstOrDefaultQuery(this StringBuilder builder, List<IMatch> matches)
    {
        var key = matches.First().StartNodeAlias;
        return builder.AppendLines(
            $"WITH DISTINCT {key}",
            $"RETURN {key}"
        );
    }
    internal static StringBuilder BuildListQuery(this StringBuilder builder, List<IMatch> matches)
    {
        var key = matches.First().StartNodeAlias;
        return builder.AppendLines(
            $"WITH DISTINCT {key}",
            $"RETURN {key}"
        );
    }

    internal static void BuildConnectionRelation(this StringBuilder builder, IRelationConfiguration relationConfig, List<IMatch> matches)
    {
        builder.AppendLine($"CREATE ({matches.First().StartNodeAlias}){relationConfig.Format("r0")}({matches.Last().StartNodeAlias})");
        var timestampConfig = Neo4jSingletonContext.TimestampConfiguration;
        if (timestampConfig.Enabled)
        {
            if (timestampConfig.EnforceModifiedTimestampKey)
                builder.AppendLine($"SET r0.{timestampConfig.CreatedTimestampKey} = timestamp(), r0.{timestampConfig.ModifiedTimestampKey} = timestamp()");
            else
                builder.AppendLine($"SET r0.{timestampConfig.CreatedTimestampKey} = timestamp()");

        }
    }
}