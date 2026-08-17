# feature/gameplay-impl — All code and detailed explanations

This document aggregates the current code changes on branch `feature/gameplay-impl` and explains each part in detail. It includes the exact source as present in the workspace and notes about behavior, assumptions, and recommendations.

Files included:
- Code/GameCaroSocket/CaroGame.Domain/Entities/Board.cs
- Code/GameCaroSocket/CaroGame.Domain/Entities/Room.cs
- Code/GameCaroSocket/CaroGame.Application/UseCases/GamePlay/MoveSubmitter.cs
- Code/GameCaroSocket/CaroGame.Application/UseCases/GamePlay/MatchEnder.cs
- Code/GameCaroSocket/CaroGame.Application/UseCases/GamePlay/TurnTimeoutHandler.cs
- Code/GameCaroSocket/CaroGame.Infrastructure/InMemory/InMemorySessionRepository.cs

---

## Board.cs

Source

```csharp
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Domain.Entities
{
	public sealed class Board
	{
		private readonly Symbol?[,] _cells;

		public int Size { get; }

		public Board(int size = 15)
		{
			if (size <= 0)
				throw new ArgumentOutOfRangeException(nameof(size));

			Size = size;
			_cells = new Symbol?[size, size];
		}
		public void PlaceSymbol(Position position, Symbol symbol)
		{
			if (!IsInBounds(position))
				throw new ArgumentOutOfRangeException(nameof(position));
			if (_cells[position.Y, position.X] is not null)
				throw new InvalidOperationException("Cell is already occupied.");
			_cells[position.Y, position.X] = symbol;
		}   

		public Symbol? GetSymbol(Position position)
		{
			if (!IsInBounds(position))
				throw new ArgumentOutOfRangeException(nameof(position));

			return _cells[position.Y, position.X];
		}


		private bool IsInBounds(Position position)
		{
			return position.X >= 0 &&
				   position.X < Size &&
				   position.Y >= 0 &&
				   position.Y < Size;
		}

		public bool IsFull()
		{
			for (var y = 0; y < Size; y++)
			{
				for (var x = 0; x < Size; x++)
				{
					if (_cells[y, x] is null)
						return false;
				}
			}

			return true;
		}
	}
}
```

Explanation
- _cells: internal 2D array of nullable Symbol. Null = empty cell.
- PlaceSymbol: validates bounds and occupancy; writes symbol. Throws on invalid.
- GetSymbol: returns symbol or null; used by win detection.
- IsFull: checks for draw by scanning all cells.

Recommendations
- Consider tracking empty cell count to avoid scanning whole board for IsFull on large boards.

---

## Room.cs

Source

```csharp
using CaroGame.Domain.Enum;
using CaroGame.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Domain.Entities
{
	public sealed class Room
	{
		private readonly List<Move> _moveHistory = new();
		private readonly HashSet<Guid> _spectators = new();
		private readonly Dictionary<Guid, DisconnectInfo> _disconnected = new();

		public Guid RoomId { get; }

		public PlayerSlot PlayerA { get; }
		public PlayerSlot PlayerB { get; }

		public Board Board { get; }

		public Guid CurrentTurn { get; private set; }

		public RoomStatus Status { get; private set; }

		public IReadOnlyCollection<Move> MoveHistory => _moveHistory;

		public IReadOnlyCollection<Guid> Spectators => _spectators;

		public IReadOnlyDictionary<Guid, DisconnectInfo> Disconnected =>
			_disconnected;

		public int TurnDurationSec { get; }

		public DateTime TurnDeadline { get; private set; }

		public DateTime CreatedAt { get; }

		public Room(
			PlayerSlot playerA,
			PlayerSlot playerB,
			int boardSize = 15,
			int turnDurationSec = 30)
		{
			RoomId = Guid.NewGuid();

			PlayerA = playerA;
			PlayerB = playerB;

			Board = new Board(boardSize);

			CurrentTurn = playerA.PlayerId;

			Status = RoomStatus.Waiting;

			TurnDurationSec = turnDurationSec;

			CreatedAt = DateTime.UtcNow;

			// Do not start the turn deadline until the first move is made.
			TurnDeadline = DateTime.MinValue;
		}

		public void ApplyMove(Guid userId, Position position)
		{
			// Check match status before allowing any move
			if (Status == RoomStatus.Finished)
				throw new InvalidOperationException("Match already finished.");

			if (Status != RoomStatus.Waiting && Status != RoomStatus.Playing)
				throw new InvalidOperationException("Invalid room status for playing.");

			if (GetRole(userId) != Role.Player)
				throw new InvalidOperationException(
					"Only players can make moves.");

			if (CurrentTurn != userId)
				throw new InvalidOperationException(
					"It is not your turn.");

			var symbol = GetPlayerSymbol(userId);

			Board.PlaceSymbol(position, symbol);

			_moveHistory.Add(
				new Move(
					_moveHistory.Count + 1,
					userId,
					position,
					symbol,
					DateTime.UtcNow));
			// Transition Waiting -> Playing when first move is applied and start turn deadline.
			if (Status == RoomStatus.Waiting)
			{
				Status = RoomStatus.Playing;
				// After placing the first move, the opponent becomes current turn and the deadline starts now.
				CurrentTurn = userId == PlayerA.PlayerId
					? PlayerB.PlayerId
					: PlayerA.PlayerId;
				TurnDeadline = DateTime.UtcNow.AddSeconds(TurnDurationSec);
			}
			else
			{
				CurrentTurn = userId == PlayerA.PlayerId
					? PlayerB.PlayerId
					: PlayerA.PlayerId;
				TurnDeadline = DateTime.UtcNow.AddSeconds(TurnDurationSec);
			}
		}

		public MatchResultType CheckWinCondition()
		{
			if (_moveHistory.Count == 0)
				return MatchResultType.Continue;

			var last = _moveHistory[^1];
			var pos = last.Position;
			var symbol = last.Symbol;

			// Directions: (dx,dy)
			(int dx, int dy)[] directions = new[]
			{
				(1, 0), // horizontal
				(0, 1), // vertical
				(1, 1), // diag down-right
				(1, -1) // diag up-right
			};

			foreach (var (dx, dy) in directions)
			{
				var count = 1; // include last move

				// scan negative direction
				for (int step = 1; ; step++)
				{
					var nx = pos.X - step * dx;
					var ny = pos.Y - step * dy;
					if (nx < 0 || nx >= Board.Size || ny < 0 || ny >= Board.Size)
						break;
					var s = Board.GetSymbol(new Position(nx, ny));
					if (s == symbol)
						count++;
					else
						break;
				}

				// scan positive direction
				for (int step = 1; ; step++)
				{
					var nx = pos.X + step * dx;
					var ny = pos.Y + step * dy;
					if (nx < 0 || nx >= Board.Size || ny < 0 || ny >= Board.Size)
						break;
					var s = Board.GetSymbol(new Position(nx, ny));
					if (s == symbol)
						count++;
					else
						break;
				}

				if (count >= 5)
				{
					return symbol == PlayerA.Symbol
						? MatchResultType.PlayerAWin
						: MatchResultType.PlayerBWin;
				}
			}

			if (Board.IsFull())
				return MatchResultType.Draw;

			return MatchResultType.Continue;
		}

		public MatchResultType? Result { get; private set; }

		public void EndMatch(MatchResultType result)
		{
			if (Status == RoomStatus.Finished)
				throw new InvalidOperationException("Match already finished.");

			Result = result;
			Status = RoomStatus.Finished;
			// Additional end-match bookkeeping could be added here
		}

		public Role GetRole(Guid userId)
		{
			if (userId == PlayerA.PlayerId ||
				userId == PlayerB.PlayerId)
			{
				return Role.Player;
			}

			if (_spectators.Contains(userId))
				return Role.Spectator;

			return Role.None;
		}

		public void AddSpectator(Guid userId)
		{
			if (GetRole(userId) == Role.None)
				_spectators.Add(userId);
		}

		public void RemoveSpectator(Guid userId)
		{
			_spectators.Remove(userId);
		}

		public void MarkDisconnected(
			Guid userId,
			int gracePeriodSeconds)
		{
			var disconnectedAt = DateTime.UtcNow;

			var gracePeriodEndsAt =
				disconnectedAt.AddSeconds(gracePeriodSeconds);

			_disconnected[userId] = new DisconnectInfo(
				userId,
				disconnectedAt,
				gracePeriodEndsAt);
		}

		public void MarkReconnected(Guid userId)
		{
			_disconnected.Remove(userId);
		}

		private Symbol GetPlayerSymbol(Guid userId)
		{
			if (userId == PlayerA.PlayerId)
				return PlayerA.Symbol;

			if (userId == PlayerB.PlayerId)
				return PlayerB.Symbol;

			throw new InvalidOperationException(
				"User is not a player in this room.");
		}
	}
}
```

Explanation
- Room stores players, board, move history, spectators, disconnected info, and manages turn flow.
- TurnDeadline is only started after the first move; that prevents timeouts before play begins.
- ApplyMove enforces state and role validations and rotates CurrentTurn and TurnDeadline appropriately.
- CheckWinCondition inspects only around the last move (efficient) and recognizes wins/draws.
- EndMatch stores the result and prevents double-ending.

Recommendations
- Persist TurnDeadline and Result in the repository to allow external timeout processing and recovery.
- Consider adding EndedAt and WinnerId to Room for easier history queries.

---

## MoveSubmitter.cs

Source

```csharp
namespace CaroGame.Application.UseCases.GamePlay
{
	using CaroGame.Application.Interfaces.Repositories;
	using CaroGame.Domain.Entities;
	using CaroGame.Domain.ValueObjects;
	using System.Threading;
	using System.Threading.Tasks;
	using System;

	public sealed class MoveSubmitter : IMoveSubmitter
	{
		private readonly IRoomRepository _roomRepository;
		private readonly IMatchEnder _matchEnder;

		public MoveSubmitter(IRoomRepository roomRepository, IMatchEnder matchEnder)
		{
			_roomRepository = roomRepository;
			_matchEnder = matchEnder;
		}

		public async Task<Room> SubmitMoveAsync(Guid roomId, Guid playerId, Position position, CancellationToken cancellationToken)
		{
			var room = await _roomRepository.GetByIdAsync(roomId);
			if (room is null)
				throw new InvalidOperationException("Room not found.");

			room.ApplyMove(playerId, position);

			var result = room.CheckWinCondition();

			if (result != Domain.Enum.MatchResultType.Continue)
			{
				// delegate to match ender to finalize
				return await _matchEnder.EndMatchAsync(roomId, result, cancellationToken);
			}

			await _roomRepository.UpdateAsync(room);
			return room;
		}
	}
}
```

Explanation
- MoveSubmitter is the application layer orchestrator for applying moves. It delegates validation to the Room domain object and persists changes.

Notes
- Ensure repository implementations handle concurrency (atomic update or optimistic concurrency) to avoid lost updates.

---

## MatchEnder.cs

Source

```csharp
namespace CaroGame.Application.UseCases.GamePlay
{
	using CaroGame.Application.Interfaces.Repositories;
	using CaroGame.Domain.Entities;
	using CaroGame.Domain.Enum;
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	public sealed class EndMatchUseCase : IMatchEnder
	{
		private readonly IRoomRepository _roomRepository;

		public EndMatchUseCase(IRoomRepository roomRepository)
		{
			_roomRepository = roomRepository;
		}

		public async Task<Room> EndMatchAsync(Guid roomId, MatchResultType matchResultType, CancellationToken cancellationToken)
		{
			var room = await _roomRepository.GetByIdAsync(roomId);
			if (room is null)
				throw new InvalidOperationException("Room not found.");

			room.EndMatch(matchResultType);

			await _roomRepository.UpdateAsync(room);

			// history persistence and notifications handled elsewhere

			return room;
		}
	}
}
```

Explanation
- Calls Room.EndMatch to record result and set Status Finished, then persists the room. Good place to also persist match history or emit events.

---

## TurnTimeoutHandler.cs

Source

```csharp
namespace CaroGame.Application.UseCases.GamePlay
{
	using CaroGame.Application.Interfaces.Repositories;
	using CaroGame.Domain.Enum;
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	public sealed class TurnTimeoutHandler : ITurnTimeoutHandler
	{
		private readonly IRoomRepository _roomRepository;
		private readonly IMatchEnder _matchEnder;

		public TurnTimeoutHandler(IRoomRepository roomRepository, IMatchEnder matchEnder)
		{
			_roomRepository = roomRepository;
			_matchEnder = matchEnder;
		}

		public async Task HandleTurnTimeoutAsync(Guid roomId, Guid playerId, CancellationToken cancellationToken)
		{
			var room = await _roomRepository.GetByIdAsync(roomId);
			if (room is null)
				return;

			// Only handle timeouts for playing rooms
			if (room.Status != RoomStatus.Playing)
				return;

			// Only proceed if it is indeed the player's turn
			if (room.CurrentTurn != playerId)
				return;

			// Ensure the deadline has been reached
			if (room.TurnDeadline == DateTime.MinValue || DateTime.UtcNow < room.TurnDeadline)
				return;

			// Opponent wins by timeout
			var winner = room.PlayerA.PlayerId == playerId ? MatchResultType.PlayerBWin : MatchResultType.PlayerAWin;

			await _matchEnder.EndMatchAsync(roomId, winner, cancellationToken);
		}
	}
}
```

Explanation
- Defensive checks for room existence, status, current turn, and deadline before awarding opponent the win.

---

## InMemorySessionRepository.cs

Source

```csharp
using CaroGame.Application.Interfaces.Repositories;
using CaroGame.Domain.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CaroGame.Infrastructure.InMemory
{
	public class InMemorySessionRepository : ISessionRepository
	{
		private readonly ConcurrentDictionary<Guid, Session> _sessions = new ConcurrentDictionary<Guid, Session>();

		public Task AddAsync(Session session)
		{
			if (session == null)
				throw new ArgumentNullException(nameof(session));

			// Use PlayerId as the dictionary key. Do not allow duplicate sessions for the same player.
			var added = _sessions.TryAdd(session.PlayerId, session);
			if (!added)
				throw new InvalidOperationException("A session for this player already exists.");

			return Task.CompletedTask;
		}

		public Task<bool> ExistsAsync(Guid playerId)
		{
			var exists = _sessions.ContainsKey(playerId);
			return Task.FromResult(exists);
		}

		public Task<Session?> GetByTokenAsync(Guid sessionId)
		{
			// The repository stores sessions keyed by PlayerId. To find by session token/id,
			// search the values for a matching SessionId. This assumes Session.SessionId is the token.
			foreach (var kv in _sessions)
			{
				var s = kv.Value;
				if (s.SessionId == sessionId)
					return Task.FromResult<Session?>(s);
			}

			return Task.FromResult<Session?>(null);
		}

		public Task<Session?> GetByUserIdAsync(Guid playerId)
		{
			if (_sessions.TryGetValue(playerId, out var session))
				return Task.FromResult<Session?>(session);

			return Task.FromResult<Session?>(null);
		}

		public Task RemoveAsync(Guid userId)
		{
			_sessions.TryRemove(userId, out _);
			return Task.CompletedTask;
		}

		public Task UpdateAsync(Session session)
		{
			// Replace or add the session for the given player id
			_sessions.AddOrUpdate(session.PlayerId, session, (k, old) => session);
			return Task.CompletedTask;
		}
	}
}
```

Explanation
- Uses ConcurrentDictionary keyed by PlayerId to store Session objects.
- AddAsync: create-only semantic (TryAdd) — throws if session exists. This prevents accidental overwrites.
- GetByUserIdAsync: efficient lookup by PlayerId via TryGetValue.
- GetByTokenAsync: scans values for a matching SessionId — this assumes Session.SessionId is the session token. If your design uses a separate token field, adjust domain/interface accordingly.
- UpdateAsync: upsert behavior via AddOrUpdate.

---

## Final notes and next steps

- Build: run a local build in Visual Studio to verify compile and missing references.
- DI: register the implemented use-cases and repositories in your server startup (CaroGame.Server Program/DI composition).
- Tests: add unit tests for board, room, and use-cases; add integration tests for timeout behavior.
- Persisted state: ensure your IRoomRepository and ISessionRepository implementations persist TurnDeadline and Result so timeouts and reconnections behave correctly across restarts.

If you want, I can also generate a PR description and checklist based on this aggregated documentation.
