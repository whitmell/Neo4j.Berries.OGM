using Neo4j.Berries.OGM.Tests.Common;
using Neo4j.Berries.OGM.Tests.Mocks.Models;
using FluentAssertions;
using Neo4j.Berries.OGM.Tests.Mocks.Enums;
using Neo4j.Berries.OGM.Enums;

namespace Neo4j.Berries.OGM.Tests.Models.Sets;
public class UpdateCamelCasePropertyTests : TestBase
{
    private readonly Movie TestMovieNode;
    private readonly Equipment TestEquipmentNode;
    private readonly Person TestPersonNode;
    public UpdateCamelCasePropertyTests() : base(true, (x) => $"{char.ToLower(x[0])}{x[1..]}")
    {
        TestMovieNode = TestGraphContext.Movies.Match().FirstOrDefault();
        TestEquipmentNode = TestGraphContext.Equipments.Match().FirstOrDefault();
        TestPersonNode = TestGraphContext.People.Match()
            .WithRelation(x => x.MoviesAsActor, x => x.Where(y => y.Id, ComparisonOperator.NotEquals, TestMovieNode.Id))
            .FirstOrDefault();
    }

    [Fact]
    public async void Should_Only_Update_Name_Of_The_Movie()
    {
        TestGraphContext
            .Movies
            .Match(x => x.Where(y => y.Id, TestMovieNode.Id))
            .Update(x => x.Set(y => y.Name, "The Test"));

        var updatedNode = await TestGraphContext.Movies.Match(x => x.Where(y => y.Id, TestMovieNode.Id)).FirstOrDefaultAsync();
        updatedNode.Should().NotBeNull();
        updatedNode.Id.Should().Be(TestMovieNode.Id);
        updatedNode.Name.Should().Be("The Test");
    }
    [Fact]
    public async void Should_Update_And_Return_Value()
    {
        TestMovieNode.ReleaseDate = new DateTime(1980, 01, 01);
        var result = await TestGraphContext
            .Movies
            .Match(x => x.Where(y => y.Id, TestMovieNode.Id))
            .UpdateAndReturnAsync(x => x.Set(TestMovieNode));
        result.ElementAt(0).ReleaseDate.Should().Be(TestMovieNode.ReleaseDate);
        result.ElementAt(0).Id.Should().Be(TestMovieNode.Id);
    }

    [Fact]
    public async void Should_Create_New_Node_And_Connect_With_Existing()
    {
        var newPerson = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = "Fariborz",
            LastName = "Nowzari",
            Age = 31
        };
        TestGraphContext.People.Add(newPerson);
        await TestGraphContext.SaveChangesAsync();

        await TestGraphContext.Movies
            .Match(x => x.Where(y => y.Id, TestMovieNode.Id))
            .ConnectAsync(x => x.Actors, x => x.Where(y => y.Id, newPerson.Id));

        var count = await TestGraphContext
            .Movies
            .Match(x => x.Where(y => y.Id, TestMovieNode.Id))
            .WithRelation(x => x.Actors, x => x.Where(y => y.Id, newPerson.Id))
            .CountAsync();

        count.Should().Be(1);
    }

    [Fact]
    public async void Should_Throw_Exception_On_Disconnecting_Without_Relation()
    {
        var act = () => TestGraphContext
            .Movies
            .Match(x => x.Where(y => y.Id, TestMovieNode.Id))
            .DisconnectAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async void Should_Disconnect_Two_Nodes()
    {
        var person = await TestGraphContext
            .People
            .Match()
            .WithRelation(x => x.MoviesAsActor, x => x.Where(y => y.Id, TestMovieNode.Id))
            .FirstOrDefaultAsync();

        await TestGraphContext
            .Movies
            .Match(x => x.Where(y => y.Id, TestMovieNode.Id))
            .WithRelation(x => x.Actors, x => x.Where(y => y.Id, person.Id))
            .DisconnectAsync();

        var count = await TestGraphContext
            .Movies
            .Match(x => x.Where(y => y.Id, TestMovieNode.Id))
            .WithRelation(x => x.Actors, x => x.Where(y => y.Id, person.Id))
            .CountAsync();

        count.Should().Be(0);
    }

    [Fact]
    public async void Should_Remove_And_Return_Archived_Nodes()
    {
        var archivedNodes = await TestGraphContext
            .Movies
            .Match(x => x.Where(y => y.Id, TestMovieNode.Id))
            .ArchiveAsync();

        archivedNodes.Should().HaveCount(1);
        archivedNodes.Should().Contain(x => x.Id == TestMovieNode.Id);

        var records = OpenSession(session =>
        {
            return session.Run(
                "MATCH(m:Movie WHERE m.id=$p0) return m.archivedOn as archivedOn",
                new Dictionary<string, object> { { "p0", TestMovieNode.Id.ToString() } })
                .Select(x =>
                {
                    var offset = DateTimeOffset.FromUnixTimeMilliseconds(x.Get<long>("archivedOn"));
                    return offset.UtcDateTime;
                })
                .ToList();
        });
        records.First().Should().BeBefore(DateTime.UtcNow);
        records.First().Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async void Test_Sequence_Of_Database_Actions_Against_Database_In_One_Transaction(bool withFailure)
    {
        var movieId = (await TestGraphContext
            .Movies
            .Match()
            .WithRelation(x => x.Actors)
            .FirstOrDefaultAsync()).Id;
        var sut = () => TestGraphContext.Database.BeginTransaction(async () =>
        {
            var movie = await TestGraphContext
                .Movies
                .Match(x => x.Where(y => y.Id, movieId))
                .WithRelation(x => x.Actors)
                .FirstOrDefaultAsync();

            await TestGraphContext
                .Movies
                .Match(x => x.Where(y => y.Id, movie.Id))
                .WithRelation(x => x.Actors)
                .DisconnectAsync();
            if (withFailure)
            {
                throw new Exception();
            }
            return movie;
        });
        if (!withFailure)
        {
            var movie = sut();
            var count = await TestGraphContext
                .People
                .Match()
                .WithRelation(x => x.MoviesAsActor, x => x.Where(y => y.Id, movie.Id))
                .CountAsync();
            count.Should().Be(0, "There should be no connection from any person to this movie anymore");
        }
        else
        {
            sut.Should().Throw<Exception>();
            var count = await TestGraphContext
                .People
                .Match()
                .WithRelation(x => x.MoviesAsActor, x => x.Where(y => y.Id, movieId))
                .CountAsync();
            count.Should().BeGreaterThan(0, "The transaction is failed, so the movie should not have it's relations disconnected");
        }
    }

    [Fact]
    public async void Should_Execute_Cyphers_Independently_From_Match_Phrase_And_Executed_Actions()
    {
        var query = TestGraphContext
            .People
            .Match()
            .WithRelation(x => x.MoviesAsActor, x => x.Where(y => y.Id, TestMovieNode.Id));

        (await query.CountAsync()).Should().BeGreaterThan(0);

        var updateAct = () => query.UpdateAsync(x => x.Set(y => y.Age, 20));
        await updateAct.Should().NotThrowAsync();

        var connectId = Guid.NewGuid();
        var connectAct = () => query.ConnectAsync(x => x.Friends, x => x.Where(y => y.Id, connectId));
        await connectAct.Should().NotThrowAsync();

        var disconnectAct = () => query.DisconnectAsync();
        await disconnectAct.Should().NotThrowAsync();

        var archivedAct = () => query.ArchiveAsync();
        await archivedAct.Should().NotThrowAsync();

        (await query.CountAsync()).Should().Be(0, "Because it is disconnected");
    }

    [Fact]
    public async void Should_Update_Multiple_Properties()
    {
        var query = TestGraphContext
            .Movies
            .Match(x => x.Where(y => y.Id, TestMovieNode.Id));

        var releaseDate = new DateTime(2004, 06, 10);
        await query.UpdateAsync(x => x
                .Set(y => y.Name, "The Punisher")
                .Set(y => y.ReleaseDate, releaseDate));

        var updatedNode = await TestGraphContext.Movies.Match(x => x.Where(y => y.Id, TestMovieNode.Id)).FirstOrDefaultAsync();
        updatedNode.Should().NotBeNull();
        updatedNode.Id.Should().Be(TestMovieNode.Id);
        updatedNode.Name.Should().Be("The Punisher");
        updatedNode.ReleaseDate.Should().Be(releaseDate);
    }
    [Fact]
    public void Should_Update_Nodes_Without_Config()
    {
        var result = TestGraphContext.Equipments.Match(x => x.Where(y => y.Id, TestEquipmentNode.Id)).UpdateAndReturn(x => x.Set(y => y.Type, EquipmentType.Light));

        result.ElementAt(0).Type.Should().Be(EquipmentType.Light);
    }

    [Fact]
    public void Should_Update_Movie_By_Property_Anonymously()
    {
        var anonymous = TestGraphContext.Anonymous("Movie");
        anonymous.Match(x => x.Where("id", TestMovieNode.Id)).Update(x => x.Set("name", "Anonymous"));
        var anonymousMovie = anonymous.Match(x => x.Where("id", TestMovieNode.Id)).FirstOrDefault<Dictionary<string, object>>();
        anonymousMovie["name"].ToString().Should().Be("Anonymous");
    }

    [Fact]
    public void Should_Update_Movie_By_Object_Anonymously()
    {
        var anonymous = TestGraphContext.Anonymous("Movie");
        anonymous
            .Match(
                x => x.Where("id", TestMovieNode.Id)
            )
            .Update(x =>
                x.Set(new Dictionary<string, object> {
                    { "name", "Anonymous" },
                    { "releaseDate", new DateTime(2050, 01, 01, 0, 0, 0) }
                })
            );
        var anonymousMovie = anonymous.Match(x => x.Where("id", TestMovieNode.Id)).FirstOrDefault<Dictionary<string, object>>();
        anonymousMovie["name"].ToString().Should().Be("Anonymous");
        anonymousMovie["releaseDate"].ToString().Should().Be("2050-01-01T00:00:00");
    }
    [Fact]
    public void Should_Connect_Node_Anonymously()
    {
        var anonymous = TestGraphContext.Anonymous("Movie");
        anonymous
            .Match(
                x => x.Where("id", TestMovieNode.Id)
            )
            .Connect("Actors", x => x.Where("id", TestPersonNode.Id));

        var testAnonymousMovie = anonymous.Match(x => x.Where("id", TestMovieNode.Id))
            .WithRelation("Actors", x => x.Where("id", TestPersonNode.Id))
            .FirstOrDefault<Dictionary<string, object>>();
        testAnonymousMovie.Should().NotBeNull();
    }
}