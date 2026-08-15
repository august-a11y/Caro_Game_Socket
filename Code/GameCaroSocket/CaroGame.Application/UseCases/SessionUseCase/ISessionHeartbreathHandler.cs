using System;
using System.Collections.Generic;
using System.Text;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public interface ISessionHeartbreathHandler
    {
        Task HandleAsync(Guid playerId, CancellationToken cancellationToken);
    }
}
