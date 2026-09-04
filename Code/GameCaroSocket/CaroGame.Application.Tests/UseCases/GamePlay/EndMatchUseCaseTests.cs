using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Application.UseCases.GamePlay;
using CaroGame.Domain.Entities;
using CaroGame.Domain.Enum;
using Moq;

namespace CaroGame.Application.Tests.UseCases.GamePlay;

public sealed class EndMatchUseCaseTests
{
    private static readonly DateTime StartTime =
        new(2026, 9, 3, 5, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WhenRoomRepositoryIsNull_Throws()
    {
        var players = new Mock<IPlayerRepository>();

        Assert.Throws<ArgumentNullException>(() =>
            new EndMatchUseCase(null!, players.Object));
    }

    [Fact]
    public void Constructor_WhenPlayerRepositoryIsNull_Throws()
    {
        var rooms = new Mock<IRoomRepository>();

        Assert.Throws<ArgumentNullException>(() =>
            new EndMatchUseCase(rooms.Object, null!));
    }

    [Fact]
    public async Task EndMatchAsync_WhenRoomIsNull_Throws()
    {
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var sut = new EndMatchUseCase(rooms.Object, players.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.EndMatchAsync(null!, MatchResultType.Draw, CancellationToken.None));
    }

    [Fact]
    public async Task EndMatchAsync_WhenCancellationWasRequested_DoesNotChangeAnything()
    {
        var (room, _, _) = CreatePlayingRoom();
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var sut = new EndMatchUseCase(rooms.Object, players.Object);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.EndMatchAsync(room, MatchResultType.Draw, cancellation.Token));

        Assert.Equal(RoomStatus.Playing, room.Status);
        Assert.Equal(MatchResultType.Continue, room.CurrentMatch!.Result);
    }

    [Fact]
    public async Task EndMatchAsync_WithContinueResult_RejectsResultWithoutPersistence()
    {
        var (room, _, _) = CreatePlayingRoom();
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var sut = new EndMatchUseCase(rooms.Object, players.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.EndMatchAsync(room, MatchResultType.Continue, CancellationToken.None));

        Assert.Equal("matchResultType", exception.ParamName);
        Assert.Equal(RoomStatus.Playing, room.Status);
    }

    [Fact]
    public async Task EndMatchAsync_WithUndefinedResult_RejectsResultWithoutPersistence()
    {
        var (room, _, _) = CreatePlayingRoom();
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var sut = new EndMatchUseCase(rooms.Object, players.Object);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.EndMatchAsync(room, (MatchResultType)999, CancellationToken.None));

        Assert.Equal("matchResultType", exception.ParamName);
        Assert.Equal(RoomStatus.Playing, room.Status);
    }

    [Fact]
    public async Task EndMatchAsync_WhenRoomIsWaiting_ThrowsWithoutLoadingPlayers()
    {
        var (_, playerX, playerO) = CreateRoom();
        var room = new Room(
            new PlayerSlot(playerX.PlayerId, Symbol.X),
            new PlayerSlot(playerO.PlayerId, Symbol.O));
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        var sut = new EndMatchUseCase(rooms.Object, players.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.EndMatchAsync(room, MatchResultType.Draw, CancellationToken.None));

        Assert.Equal(RoomStatus.Waiting, room.Status);
    }

    [Theory]
    [InlineData(MatchResultType.PlayerXWin, 1, 0, 0, 0, 1, 0)]
    [InlineData(MatchResultType.PlayerOWin, 0, 1, 0, 1, 0, 0)]
    [InlineData(MatchResultType.Draw, 0, 0, 1, 0, 0, 1)]
    public async Task EndMatchAsync_WithFinalResult_FinalizesAndUpdatesBothPlayers(
        MatchResultType result,
        int xWins,
        int xLosses,
        int xDraws,
        int oWins,
        int oLosses,
        int oDraws)
    {
        var (room, playerX, playerO) = CreatePlayingRoom();
        playerX.Status = PlayerStatus.InMatch;
        playerO.Status = PlayerStatus.InMatch;
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = CreatePlayerRepository(playerX, playerO);
        rooms.Setup(repository => repository.UpdateAsync(room))
            .Returns(Task.CompletedTask);
        var sut = new EndMatchUseCase(rooms.Object, players.Object);

        var returned = await sut.EndMatchAsync(room, result, CancellationToken.None);

        Assert.Same(room, returned);
        Assert.Equal(RoomStatus.Finished, room.Status);
        Assert.Equal(result, room.CurrentMatch!.Result);
        Assert.Equal((xWins, xLosses, xDraws),
            (playerX.Stats.Wins, playerX.Stats.Losses, playerX.Stats.Draws));
        Assert.Equal((oWins, oLosses, oDraws),
            (playerO.Stats.Wins, playerO.Stats.Losses, playerO.Stats.Draws));
        Assert.Equal(PlayerStatus.Free, playerX.Status);
        Assert.Equal(PlayerStatus.Free, playerO.Status);
        players.Verify(repository => repository.UpdateAsync(playerX), Times.Once);
        players.Verify(repository => repository.UpdateAsync(playerO), Times.Once);
        rooms.Verify(repository => repository.UpdateAsync(room), Times.Once);
        rooms.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task EndMatchAsync_WhenPlayerIsOffline_PreservesOfflineStatus()
    {
        var (room, playerX, playerO) = CreatePlayingRoom();
        playerX.Status = PlayerStatus.Offline;
        playerO.Status = PlayerStatus.InMatch;
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = CreatePlayerRepository(playerX, playerO);
        rooms.Setup(repository => repository.UpdateAsync(room))
            .Returns(Task.CompletedTask);
        var sut = new EndMatchUseCase(rooms.Object, players.Object);

        await sut.EndMatchAsync(room, MatchResultType.PlayerXWin, CancellationToken.None);

        Assert.Equal(PlayerStatus.Offline, playerX.Status);
        Assert.Equal(PlayerStatus.Free, playerO.Status);
        Assert.Equal(1, playerX.Stats.Wins);
        Assert.Equal(1, playerO.Stats.Losses);
    }

    [Fact]
    public async Task EndMatchAsync_WhenOnePlayerCannotBeLoaded_UpdatesAvailablePlayer()
    {
        var (room, playerX, playerO) = CreatePlayingRoom();
        playerO.Status = PlayerStatus.InMatch;
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = new Mock<IPlayerRepository>(MockBehavior.Strict);
        players.Setup(repository => repository.GetByIdAsync(playerX.PlayerId))
            .ReturnsAsync((Player?)null);
        players.Setup(repository => repository.GetByIdAsync(playerO.PlayerId))
            .ReturnsAsync(playerO);
        players.Setup(repository => repository.UpdateAsync(playerO))
            .Returns(Task.CompletedTask);
        rooms.Setup(repository => repository.UpdateAsync(room))
            .Returns(Task.CompletedTask);
        var sut = new EndMatchUseCase(rooms.Object, players.Object);

        await sut.EndMatchAsync(room, MatchResultType.PlayerXWin, CancellationToken.None);

        Assert.Equal(1, playerO.Stats.Losses);
        Assert.Equal(PlayerStatus.Free, playerO.Status);
        players.Verify(repository => repository.UpdateAsync(It.IsAny<Player>()), Times.Once);
    }

    [Fact]
    public async Task EndMatchAsync_WhenRepeated_IsIdempotentAndKeepsFirstResult()
    {
        var (room, playerX, playerO) = CreatePlayingRoom();
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = CreatePlayerRepository(playerX, playerO);
        rooms.Setup(repository => repository.UpdateAsync(room))
            .Returns(Task.CompletedTask);
        var sut = new EndMatchUseCase(rooms.Object, players.Object);

        await sut.EndMatchAsync(room, MatchResultType.PlayerXWin, CancellationToken.None);
        var returned = await sut.EndMatchAsync(
            room,
            MatchResultType.PlayerOWin,
            CancellationToken.None);

        Assert.Same(room, returned);
        Assert.Equal(MatchResultType.PlayerXWin, room.CurrentMatch!.Result);
        Assert.Equal(1, playerX.Stats.Wins);
        Assert.Equal(0, playerX.Stats.Losses);
        Assert.Equal(1, playerO.Stats.Losses);
        Assert.Equal(0, playerO.Stats.Wins);
        players.Verify(repository => repository.GetByIdAsync(playerX.PlayerId), Times.Once);
        players.Verify(repository => repository.GetByIdAsync(playerO.PlayerId), Times.Once);
        players.Verify(repository => repository.UpdateAsync(playerX), Times.Once);
        players.Verify(repository => repository.UpdateAsync(playerO), Times.Once);
        rooms.Verify(repository => repository.UpdateAsync(room), Times.Once);
    }

    [Fact]
    public async Task EndMatchAsync_WhenRoomPersistenceFails_PropagatesFailure()
    {
        var (room, playerX, playerO) = CreatePlayingRoom();
        var rooms = new Mock<IRoomRepository>(MockBehavior.Strict);
        var players = CreatePlayerRepository(playerX, playerO);
        var expected = new InvalidOperationException("write failed");
        rooms.Setup(repository => repository.UpdateAsync(room))
            .ThrowsAsync(expected);
        var sut = new EndMatchUseCase(rooms.Object, players.Object);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.EndMatchAsync(room, MatchResultType.Draw, CancellationToken.None));

        Assert.Same(expected, actual);
    }

    private static Mock<IPlayerRepository> CreatePlayerRepository(
        Player playerX,
        Player playerO)
    {
        var repository = new Mock<IPlayerRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(playerX.PlayerId))
            .ReturnsAsync(playerX);
        repository.Setup(value => value.GetByIdAsync(playerO.PlayerId))
            .ReturnsAsync(playerO);
        repository.Setup(value => value.UpdateAsync(playerX))
            .Returns(Task.CompletedTask);
        repository.Setup(value => value.UpdateAsync(playerO))
            .Returns(Task.CompletedTask);
        return repository;
    }

    private static (Room Room, Player PlayerX, Player PlayerO) CreateRoom()
    {
        var playerX = new Player("player-x");
        var playerO = new Player("player-o");
        var room = new Room(
            new PlayerSlot(playerX.PlayerId, Symbol.X),
            new PlayerSlot(playerO.PlayerId, Symbol.O));
        return (room, playerX, playerO);
    }

    private static (Room Room, Player PlayerX, Player PlayerO) CreatePlayingRoom()
    {
        var (room, playerX, playerO) = CreateRoom();
        room.MarkReady(playerX.PlayerId);
        room.MarkReady(playerO.PlayerId);
        room.StartNewMatch(StartTime);
        return (room, playerX, playerO);
    }
}
