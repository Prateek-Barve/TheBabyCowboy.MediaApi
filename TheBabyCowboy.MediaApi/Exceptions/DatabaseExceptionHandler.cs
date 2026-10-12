using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TheBabyCowBoy.MediaApi.Exceptions;

public static class DatabaseExceptionHandler
{
    public static bool IsUniqueViolation(
        DbUpdateException exception,
        out string? constraintName)
    {
        constraintName = null;

        if (exception.InnerException is not PostgresException postgresException)
        {
            return false;
        }

        if (postgresException.SqlState !=
            PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }

        constraintName = postgresException.ConstraintName;

        return true;
    }
}