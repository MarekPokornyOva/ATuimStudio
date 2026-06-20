using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace ATuimStudio.Extensions.Core
{
	sealed class SqliteUserOptionsRepository : IUserOptionsRepository, IUserSolutionOptionsRepository, IAsyncDisposable
	{
		readonly string _databasePath;
		SqliteConnection? _connection;

		public SqliteUserOptionsRepository(string databaseFilePath)
		{
			_databasePath = databaseFilePath;
		}

		public async ValueTask DisposeAsync()
		{
			if (_connection != null)
				await _connection.DisposeAsync();
		}

		public async IAsyncEnumerable<KeyValuePair<string, object>> LoadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
		{
			SqliteConnection connection = await GetConnection(cancellationToken);

			using (SqliteCommand command = connection.CreateCommand())
			{
				command.CommandText = "SELECT Key,Value,Type FROM UserOptions ORDER BY Key;";
				using (SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
				{
					while (await reader.ReadAsync(cancellationToken))
						if (TryDecodeValue((byte[])reader.GetValue(1), reader.GetInt32(2), out object? deserializedValue))
							yield return new KeyValuePair<string, object>(reader.GetString(0), deserializedValue);
				}
			}
		}

		public async Task SaveAsync(IEnumerable<KeyValuePair<string, object?>> values, CancellationToken cancellationToken)
		{
			SqliteConnection connection = await GetConnection(cancellationToken);

			using (DbTransaction trans = await connection.BeginTransactionAsync(cancellationToken))
			{
				using (SqliteCommand upsertCommand = connection.CreateCommand())
				using (SqliteCommand deleteCommand = connection.CreateCommand())
				{
//					upsertCommand.CommandText = @"INSERT INTO UserOptions (Key,Value,Type) VALUES(@p0,@p1,@p2)
//ON CONFLICT(name) DO
//UPDATE SET
//		Value=excluded.Value,
//		Type=excluded.Type
//	WHERE Key=excluded.Key
//					";
					upsertCommand.CommandText = @"INSERT OR REPLACE INTO UserOptions (Key, Value, Type) VALUES (@p0,@p1,@p2)";
					SqliteParameter upsertCommandKeyParm = upsertCommand.Parameters.Add("@p0", SqliteType.Text);
					SqliteParameter upsertCommandValueParm = upsertCommand.Parameters.Add("@p1", SqliteType.Blob);
					SqliteParameter upsertCommandTypeParm = upsertCommand.Parameters.Add("@p2", SqliteType.Integer);
					deleteCommand.CommandText = "DELETE FROM UserOptions WHERE Key=@p0";
					SqliteParameter deleteCommandKeyParm = deleteCommand.Parameters.Add("@p0", SqliteType.Text);

					foreach (KeyValuePair<string, object?> item in values)
					{
						object? value = item.Value;
						if (value == null)
						{
							deleteCommandKeyParm.Value = item.Key;
							await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
						}
						else
						{
							upsertCommandKeyParm.Value = item.Key;
							if (!TryEncodeValue(value, out byte[]? bytes, out int type))
								continue;
							upsertCommandValueParm.Value = bytes;
							upsertCommandTypeParm.Value = type;
							await upsertCommand.ExecuteNonQueryAsync(cancellationToken);
						}
					}
				}

				await trans.CommitAsync(cancellationToken);
			}
		}

		public async Task ClearAllAsync(CancellationToken cancellationToken)
		{
			SqliteConnection connection = await GetConnection(cancellationToken);

			using (SqliteCommand command = connection.CreateCommand())
			{
				command.CommandText = "DELETE FROM UserOptions";
				await command.ExecuteNonQueryAsync(cancellationToken);
			}
		}

		#region Decode/Encode value
		static bool TryDecodeValue(byte[] value, int type, [NotNullWhen(true)] out object? result)
		{
			result = type switch
			{
				1/*"string"*/ => Encoding.UTF8.GetString(value),
				2/*"int"*/ => BitConverter.ToInt32(value, 0),
				3/*"long"*/ => BitConverter.ToInt64(value, 0),
				4/*"double"*/ => BitConverter.ToDouble(value, 0),
				5/*"bool"*/ => BitConverter.ToBoolean(value, 0),
				6/*"bytearray"*/ => value,
				_ => null
			};
			return result != null;
		}

		static bool TryEncodeValue(object value, [NotNullWhen(true)] out byte[]? bytes, out int type)
		{
			if (value is string strVal)
			{
				bytes = Encoding.UTF8.GetBytes(strVal);
				type = 1;
				return true;
			}
			if (value is int intVal)
			{
				bytes = BitConverter.GetBytes(intVal);
				type = 2;
				return true;
			}
			if (value is long longVal)
			{
				bytes = BitConverter.GetBytes(longVal);
				type = 3;
				return true;
			}
			if (value is double dblVal)
			{
				bytes = BitConverter.GetBytes(dblVal);
				type = 4;
				return true;
			}
			if (value is bool boolVal)
			{
				bytes = BitConverter.GetBytes(boolVal);
				type = 5;
				return true;
			}
			if (value is byte[] bytesVal)
			{
				bytes = bytesVal;
				type = 6;
				return true;
			}

			bytes = default!;
			type = default;
			return false;
		}
		#endregion Decode/Encode value

		#region GetConnection
		async Task<SqliteConnection> GetConnection(CancellationToken cancellationToken)
		{
			if (_connection == null)
			{
				SqliteConnectionStringBuilder connectionStringBuilder = new SqliteConnectionStringBuilder
				{
					DataSource = _databasePath,
					Mode = SqliteOpenMode.ReadWriteCreate,
				};
				SqliteConnection connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
				await EnsureDatabase(connection, _databasePath, cancellationToken);

				_connection = connection;
			}
			return _connection;
		}

		static async Task EnsureDatabase(SqliteConnection connection, string databasePath, CancellationToken cancellationToken)
		{
			bool exists = File.Exists(databasePath);
			if (!exists)
				Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
			await connection.OpenAsync(cancellationToken);
			if (!exists)
				using (SqliteCommand command = connection.CreateCommand())
				{
					command.CommandText = @"
							CREATE TABLE IF NOT EXISTS UserOptions (
								Key TEXT NOT NULL,
								Value BLOB NOT NULL,
								Type INTEGER NOT NULL,
								PRIMARY KEY (Key)
							);
						";
					await command.ExecuteNonQueryAsync(cancellationToken);
				}
		}
		#endregion GetConnection
	}
}
