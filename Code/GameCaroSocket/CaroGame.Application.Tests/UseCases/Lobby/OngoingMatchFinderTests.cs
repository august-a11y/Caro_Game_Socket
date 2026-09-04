using System.Reflection;
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.Lobby;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using Moq;

namespace CaroGame.Application.Tests.UseCases.Lobby;

public sealed class OngoingMatchFinderTests
{
    private static readonly DateTime MatchStartedAt =
        new(2026, 9, 3, 8, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithNullRoomRepository_ThrowsArgumentNullException()
    {
        var players = new Mock<IPlayerRepository>().Object;

        Assert.Throws<ArgumentNullException>(() => new OngoingMatchFinder(null!, players));
    }

    [Fact]
    public void Constructor_WithNullPlayerRepository_ThrowsArgumentNullException()
    {
        var rooms = new Mock<IRoomRepository>().Object;

        Assert.Throws<ArgumentNullException>(() => new OngoingMatchFinder(rooms, null!));
    }

    [Fact]
    public async Task FindOngoingMatchesAsync_WhenRepositoryIsEmpty_ReturnsEmptyList()
    {
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync())
            .ReturnsAsync(Array.Empty<Room>());
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var finder = new OngoingMatchFinder(rooms.Object, players.Object);

        var result = await finder.FindOngoingMatchesAsync(CancellationToken.None);

        Assert.Empty(result);
        rooms.Verify(repository => repository.GetOngoingRoomsAsync(), Times.Once);
        players.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task FindOngoingMatchesAsync_MapsEveryPlayingRoom()
    {
        var playerX = new Player("player-x");
        var playerO = new Player("player-o");
        var room = CreateRoom(playerX, playerO);
        StartRoom(room);
        var spectatorA = Guid.NewGuid();
        var spectatorB = Guid.NewGuid();
        room.AddSpectator(spectatorA);
        room.AddSpectator(spectatorB);

        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync())
            .ReturnsAsync([room]);
        var players = CreatePlayerRepository(playerX, playerO);
        var finder = new OngoingMatchFinder(rooms.Object, players.Object);

        var result = await finder.FindOngoingMatchesAsync(CancellationToken.None);

        var summary = Assert.Single(result);
        Assert.Equal(room.RoomId, summary.RoomId);
        Assert.Equal(playerX.Nickname, summary.PlayerAName);
        Assert.Equal(playerO.Nickname, summary.PlayerBName);
        Assert.Equal(2, summary.SpectatorCount);
        Assert.Equal(RoomStatus.Playing, summary.Status);
        Assert.Equal(MatchStartedAt, summary.StartedAt);
        players.Verify(repository => repository.GetByIdAsync(playerX.PlayerId), Times.Once);
        players.Verify(repository => repository.GetByIdAsync(playerO.PlayerId), Times.Once);
    }

    [Fact]
    public async Task FindOngoingMatchesAsync_FiltersWaitingAndFinishedRooms()
    {
        var waitingX = new Player("waiting-x");
        var waitingO = new Player("waiting-o");
        var waitingRoom = CreateRoom(waitingX, waitingO);

        var finishedX = new Player("finished-x");
        var finishedO = new Player("finished-o");
        var finishedRoom = CreateRoom(finishedX, finishedO);
        StartRoom(finishedRoom);
        finishedRoom.EndMatch(MatchResultType.Draw);

        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync())
            .ReturnsAsync([waitingRoom, finishedRoom]);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var finder = new OngoingMatchFinder(rooms.Object, players.Object);

        var result = await finder.FindOngoingMatchesAsync(CancellationToken.None);

        Assert.Empty(result);
        players.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task FindOngoingMatchesAsync_WhenPlayerXIsMissing_ThrowsIntegrityError()
    {
        var playerX = new Player("player-x");
        var playerO = new Player("player-o");
        var room = CreateRoom(playerX, playerO);
        StartRoom(room);

        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync()).ReturnsAsync([room]);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(playerX.PlayerId))
            .ReturnsAsync((Player?)null);
        var finder = new OngoingMatchFinder(rooms.Object, players.Object);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            finder.FindOngoingMatchesAsync(CancellationToken.None));

        Assert.Contains(playerX.PlayerId.ToString(), exception.Message, StringComparison.Ordinal);
        players.Verify(repository => repository.GetByIdAsync(playerO.PlayerId), Times.Never);
    }

    [Fact]
    public async Task FindOngoingMatchesAsync_WhenPlayerOIsMissing_ThrowsIntegrityError()
    {
        var playerX = new Player("player-x");
        var playerO = new Player("player-o");
        var room = CreateRoom(playerX, playerO);
        StartRoom(room);

        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync()).ReturnsAsync([room]);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(playerX.PlayerId)).ReturnsAsync(playerX);
        players.Setup(repository => repository.GetByIdAsync(playerO.PlayerId))
            .ReturnsAsync((Player?)null);
        var finder = new OngoingMatchFinder(rooms.Object, players.Object);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            finder.FindOngoingMatchesAsync(CancellationToken.None));

        Assert.Contains(playerO.PlayerId.ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FindOngoingMatchesAsync_WhenPlayingRoomHasNoMatch_ThrowsIntegrityError()
    {
        var playerX = new Player("player-x");
        var playerO = new Player("player-o");
        var room = CreateRoom(playerX, playerO);
        StartRoom(room);
        typeof(Room)
            .GetField("<CurrentMatch>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(room, null);

        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        rooms.Setup(repository => repository.GetOngoingRoomsAsync()).ReturnsAsync([room]);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var finder = new OngoingMatchFinder(rooms.Object, players.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            finder.FindOngoingMatchesAsync(CancellationToken.None));

        Assert.Contains(room.RoomId.ToString(), exception.Message, StringComparison.Ordinal);
        players.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task FindOngoingMatchesAsync_WithPreCancelledToken_ThrowsWithoutQueryingRepositories()
    {
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var finder = new OngoingMatchFinder(rooms.Object, players.Object);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            finder.FindOngoingMatchesAsync(cancellation.Token));

        rooms.Verify(repository => repository.GetOngoingRoomsAsync(), Times.Never);
        players.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    private static Room CreateRoom(Player playerX, Player playerO) =>
        new(
            new PlayerSlot(playerX.PlayerId, Symbol.X),
            new PlayerSlot(playerO.PlayerId, Symbol.O));

    private static void StartRoom(Room room)
    {
        room.MarkReady(room.PlayerX.PlayerId);
        room.MarkReady(room.PlayerO.PlayerId);
        room.StartNewMatch(MatchStartedAt);
    }

    private static Mock<IPlayerRepository> CreatePlayerRepository(Player playerX, Player playerO)
    {
        var repository = new Mock<IPlayerRepository>(MockBehavior.Strict);
        repository.Setup(candidate => candidate.GetByIdAsync(playerX.PlayerId)).ReturnsAsync(playerX);
        repository.Setup(candidate => candidate.GetByIdAsync(playerO.PlayerId)).ReturnsAsync(playerO);
        return repository;
    }
}
