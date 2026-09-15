using CaroGame.Domain.Entities;

namespace CaroGame.Application.UseCases.SessionUseCase
{
    public interface IPlayerJoiner
    {
        Session Join(string nickname);
    }
}

