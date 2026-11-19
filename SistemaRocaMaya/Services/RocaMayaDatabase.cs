using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Dapper;
using Microsoft.Data.Sqlite;

namespace SistemaRocaMaya.Services;

internal sealed class RocaMayaDatabase
{
    private static readonly Lazy<RocaMayaDatabase> Database = new(() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RocaMaya", "rocamaya.sqlite")));

    internal static RocaMayaDatabase Instance => Database.Value;
    internal int? CurrentUserId { get; set; }
    internal int UserId => CurrentUserId ?? throw new InvalidOperationException("Inicia sesión antes de registrar operaciones.");
    private readonly string _connectionString;

    internal RocaMayaDatabase(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path, ForeignKeys = true }.ToString();
        Write(db =>
        {
            if (db.Scalar<int>("PRAGMA user_version") == 0)
            {
                db.Execute(ReadScript("schema.sql"));
                db.Execute(ReadScript("seed.sql"));
            }
            return 0;
        });
    }

    internal SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    internal T Write<T>(Func<DatabaseWrite, T> operation)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        try
        {
            var result = operation(new DatabaseWrite(connection, transaction));
            transaction.Commit();
            return result;
        }
        catch (SqliteException exception)
        {
            throw new InvalidOperationException("No se pudo guardar la operación en SQLite.", exception);
        }
    }

    internal static long Cents(decimal value) => checked((long)decimal.Round(value * 100m, 0, MidpointRounding.AwayFromZero));
    internal static decimal Money(long value) => value / 100m;
    internal static string Date(DateTime date) => date.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
    internal static DateTime ReadDate(string date) => DateTime.ParseExact(date, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

    private static string ReadScript(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"SistemaRocaMaya.Database.{name}")
            ?? throw new InvalidOperationException($"No se encontró el script {name}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    internal static bool VerifySecret(string secret, string hash)
    {
        var parts = hash.Split(':');
        if (parts.Length != 2) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var stored = Convert.FromBase64String(parts[1]);
            if (salt.Length < 8 || stored.Length != 32) return false;
            var derived = Rfc2898DeriveBytes.Pbkdf2(secret, salt, 100_000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(stored, derived);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    internal static string HashSecret(string secret)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(secret, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
}

internal sealed class DatabaseWrite(SqliteConnection connection, SqliteTransaction transaction)
{
    internal int Execute(string sql, object? args = null) => connection.Execute(sql, args, transaction);
    internal T Scalar<T>(string sql, object? args = null) => connection.ExecuteScalar<T>(sql, args, transaction)!;
    internal List<T> Query<T>(string sql, object? args = null) => connection.Query<T>(sql, args, transaction).AsList();
    internal int Insert(string sql, object args) => Scalar<int>(sql + " RETURNING id", args);

    internal string Folio(string prefix, DateTime? date = null)
    {
        var day = date?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "";
        var number = Scalar<int>("""
            INSERT INTO secuencias_folio(tipo,fecha,ultimo) VALUES(@prefix,@day,1)
            ON CONFLICT(tipo,fecha) DO UPDATE SET ultimo=ultimo+1 RETURNING ultimo
            """, new { prefix, day });
        return date.HasValue ? $"{prefix}-{day}-{number:0000}" : $"{prefix}-{number:000000}";
    }

    internal void Post(DateTime date, int user, string concept, string debit, string credit, long amount,
        int? purchase = null, int? sale = null, int? movement = null, string? creditConcept = null)
    {
        if (amount == 0) return;
        var id = Insert("""
            INSERT INTO asientos_contables(numero,fecha_hora,concepto,usuario_id,compra_id,venta_id,movimiento_credito_id)
            VALUES(@number,@stamp,@concept,@user,@purchase,@sale,@movement)
            """, new { number = Folio("AS", date), stamp = RocaMayaDatabase.Date(date), concept, user, purchase, sale, movement });
        Execute("""
            INSERT INTO asiento_detalles(asiento_id,cuenta_id,orden,concepto,debito_centavos,credito_centavos)
            VALUES(@id,(SELECT id FROM cuentas_contables WHERE nombre=@debit),1,@concept,@amount,0),
                  (@id,(SELECT id FROM cuentas_contables WHERE nombre=@credit),2,@creditConcept,0,@amount)
            """, new { id, debit, credit, amount, concept, creditConcept = creditConcept ?? concept });
    }
}
