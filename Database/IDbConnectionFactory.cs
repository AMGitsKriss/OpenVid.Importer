using System.Data;

namespace Database
{
    public interface IDbConnectionFactory
    {
        IDbConnection OpenDefault();
    }
}