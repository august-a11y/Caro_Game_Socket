# MatchEnder (EndMatchUseCase)

File: Code/GameCaroSocket/CaroGame.Application/UseCases/GamePlay/MatchEnder.cs

Purpose:
- Finalizes a match by calling Room.EndMatch and persisting the room via IRoomRepository.UpdateAsync.

Notes:
- Room.EndMatch stores the MatchResultType in Room.Result and sets Status to Finished; it throws if the room was already finished.
- Consider adding match history persistence or domain events here if required by the system.
