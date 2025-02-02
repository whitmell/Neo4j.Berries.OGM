using System.Text;
using Neo4j.Berries.OGM.Models.Sets;
using Neo4j.Berries.OGM.Tests.Common;
using Neo4j.Berries.OGM.Tests.Mocks.Models;
using FluentAssertions;
using Neo4j.Berries.OGM.Contexts;

namespace Neo4j.Berries.OGM.Tests.Models.Sets;

public class UpdateSetTests : TestBase
{
    [Fact]
    public void Should_Set_Only_One_Property()
    {
        var cypherBuilder = new StringBuilder();
        var sut = new UpdateSet<Movie>(cypherBuilder, 0, "l0")
            .Set(x => x.Name, "The Matrix");
        cypherBuilder.ToString().Should().Be("SET l0.Name = $up_0_0");
        sut.Parameters.Should().HaveCount(1);
        sut.Parameters["up_0_0"].Should().Be("The Matrix");
    }

    [Fact]
    public void Should_Set_Multiple_Properties()
    {
        var cypherBuilder = new StringBuilder();
        var releaseDate = new DateTime(1999, 1, 1);
        var sut = new UpdateSet<Movie>(cypherBuilder, 0, "l0")
            .Set(x => x.Name, "The Matrix")
            .Set(x => x.ReleaseDate, releaseDate);
        cypherBuilder.ToString().Should().Be("SET l0.Name = $up_0_0, l0.ReleaseDate = $up_0_1");
        sut.Parameters.Should().HaveCount(2);
        sut.Parameters["up_0_0"].Should().Be("The Matrix");
        sut.Parameters["up_0_1"].Should().Be(releaseDate);
    }

    [Fact]
    public void Should_Set_Custom_Given_Property()
    {
        var cypherBuilder = new StringBuilder();
        var sut = new UpdateSet<Movie>(cypherBuilder, 0, "l0")
            .Set("CreatedBy", "Farhad");
        cypherBuilder.ToString().Should().Be("SET l0.CreatedBy = $up_0_0");
        sut.Parameters.Should().HaveCount(1);
        sut.Parameters["up_0_0"].Should().Be("Farhad");
    }

    [Fact]
    public void Should_Set_Complete_Node()
    {
        var id = Guid.NewGuid();
        var cypherBuilder = new StringBuilder();
        var sut = new UpdateSet<Movie>(cypherBuilder, 0, "l0")
            .Set(new Movie { Name = "The Matrix", ReleaseDate = new DateTime(1999, 1, 1), Id = id });
        cypherBuilder.ToString().Should().Be("SET l0.Id = $up_0_0, l0.Name = $up_0_1, l0.ReleaseDate = $up_0_2");
        sut.Parameters.Should().HaveCount(3);
        sut.Parameters["up_0_0"].Should().Be(id.ToString());
        sut.Parameters["up_0_1"].Should().Be("The Matrix");
        sut.Parameters["up_0_2"].Should().Be(new DateTime(1999, 1, 1));
    }
}