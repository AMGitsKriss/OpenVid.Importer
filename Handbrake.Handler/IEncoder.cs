using Common.Entities;

namespace Handbrake.Handler
{
    public interface IEncoder
    {
        Task<bool> Execute(EncodeJobContext queueItem);
    }
}