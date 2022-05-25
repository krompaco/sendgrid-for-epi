using Krompaco.SendGridForEpi.Extensions;
using Krompaco.SendGridForEpi.Models;
using Krompaco.SendGridForEpi.Services;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;

namespace Krompaco.SendGridForEpi.SqlServer.Services;

public class SqlServerMailService : SqlServerServiceBase, IMailService
{
    private static readonly object ServiceLock = new();

    public SqlServerMailService(string sqlServerConnectionString)
        : base(sqlServerConnectionString)
    {
    }

    public void AddToQueue(MailQueueItem item)
    {
        var i = 1;
        var personalizations = item.Mail?.Personalizations.ToList() ?? new List<Personalization>();

        const string InsertQuery = @"INSERT INTO [SendGridForEpiMailQueue]
                                            ([Date], [TemplateId], [MailJson], [Personalizations], [Batch], [LastAttempt], [Attempts])
                                            VALUES
                                            (@Date, @TemplateId, @MailJson, @Personalizations, @Batch, @LastAttempt, @Attempts)";

        using var connection = this.GetNewConnection();
        connection.Open();

        // SendGrid API accepts 1000 personalizations per send posting
        // so we make copies with batched Personalizations here if needed
        foreach (var batch in personalizations.SplitToBatches(1000))
        {
            if (item.Mail != null)
            {
                item.Mail.Personalizations = batch.ToList();

                var cmd = new SqlCommand(InsertQuery, connection);

                cmd.Parameters.AddWithValue("@Date", item.Date);
                cmd.Parameters.AddWithValue("@TemplateId", item.Mail.TemplateId);
                cmd.Parameters.AddWithValue("@MailJson", item.Mail.Serialize());
                cmd.Parameters.AddWithValue("@Personalizations", item.Mail.Personalizations.Count);
                cmd.Parameters.AddWithValue("@Batch", i);
                cmd.Parameters.AddWithValue("@LastAttempt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@Attempts", 0);

                cmd.ExecuteNonQuery();
            }

            i++;
        }
    }

    public List<MailQueueItem> GetQueue()
    {
        const string SelectQuery = @"SELECT 
                                            [MailQueueId]
                                           ,[Date]
                                           ,[MailJson]
                                           ,[LastAttempt]
                                           ,[Attempts]
                                           ,[AttemptMessage]
                                        FROM [SendGridForEpiMailQueue]
                                        ORDER BY [Attempts]";
        var list = new List<MailQueueItem>();

        lock (ServiceLock)
        {
            using var connection = this.GetNewConnection();
            connection.Open();

            using var command = new SqlCommand(SelectQuery, connection);
            using var reader = command.ExecuteReader();
            var mailQueueItemIdOrdinal = reader.GetOrdinal("MailQueueId");
            var dateOrdinal = reader.GetOrdinal("Date");
            var lastAttemptOrdinal = reader.GetOrdinal("LastAttempt");
            var attemptsOrdinal = reader.GetOrdinal("Attempts");
            var attemptMessageOrdinal = reader.GetOrdinal("AttemptMessage");
            var mailJsonOrdinal = reader.GetOrdinal("MailJson");

            while (reader.Read())
            {
                try
                {
                    list.Add(new MailQueueItem
                    {
                        MailQueueItemId = reader.GetInt64(mailQueueItemIdOrdinal),
                        AttemptMessage = reader.IsDBNull(attemptMessageOrdinal) ? null : reader.GetString(attemptMessageOrdinal),
                        Attempts = reader.GetInt32(attemptsOrdinal),
                        Date = reader.GetDateTime(dateOrdinal),
                        LastAttempt = reader.GetDateTime(lastAttemptOrdinal),
                        Mail = JsonConvert.DeserializeObject<SendGridMessage>(reader.GetString(mailJsonOrdinal)),
                    });
                }
                catch (Exception ex)
                {
                    MarkError(reader.GetInt64(mailQueueItemIdOrdinal), $"{ex.Message}\r\n{ex.StackTrace}", false, connection);
                }
            }
        }

        return list;
    }

    public void MarkComplete(long mailQueueItemId)
    {
        lock (ServiceLock)
        {
            const string MoveQuery = @"BEGIN TRANSACTION [MoveTransaction]
BEGIN TRY
  INSERT INTO [SendGridForEpiMailQueueArchive]
      ([MailQueueId], [Date], [TemplateId], [MailJson], [Personalizations], [Batch], [LastAttempt], [Attempts], [AttemptMessage])
    SELECT [MailQueueId], [Date], [TemplateId], [MailJson], [Personalizations], [Batch], @LastAttempt AS LastAttempt, [Attempts] + 1 AS Attempts, [AttemptMessage]
        FROM [SendGridForEpiMailQueue] WHERE [MailQueueId] = @MailQueueId
  DELETE FROM [SendGridForEpiMailQueue] WHERE [MailQueueId] = @MailQueueId
  COMMIT TRANSACTION [MoveTransaction]
END TRY
BEGIN CATCH
  ROLLBACK TRANSACTION [MoveTransaction]
END CATCH";

            using (var connection = this.GetNewConnection())
            {
                connection.Open();

                var cmd = new SqlCommand(MoveQuery, connection);

                cmd.Parameters.AddWithValue("@MailQueueId", mailQueueItemId);
                cmd.Parameters.AddWithValue("@LastAttempt", DateTime.UtcNow);

                cmd.ExecuteNonQuery();
            }
        }
    }

    public void MarkError(long mailQueueItemId, string message)
    {
        using var connection = this.GetNewConnection();
        connection.Open();
        MarkError(mailQueueItemId, message, true, connection);
    }

    private static void MarkError(long mailQueueItemId, string message, bool needsLock, SqlConnection openConnection)
    {
        if (needsLock)
        {
            lock (ServiceLock)
            {
                SetError(mailQueueItemId, message, openConnection);
            }
        }
        else
        {
            SetError(mailQueueItemId, message, openConnection);
        }
    }

    private static void SetError(long mailQueueItemId, string message, SqlConnection openConnection)
    {
        const string UpdateQuery = "UPDATE [SendGridForEpiMailQueue] SET [LastAttempt] = @LastAttempt, [Attempts] = [Attempts] + 1, [AttemptMessage] = @AttemptMessage WHERE [MailQueueId] = @MailQueueId";

        var cmd = new SqlCommand(UpdateQuery, openConnection);

        cmd.Parameters.AddWithValue("@MailQueueId", mailQueueItemId);
        cmd.Parameters.AddWithValue("@AttemptMessage", message);
        cmd.Parameters.AddWithValue("@LastAttempt", DateTime.UtcNow);

        cmd.ExecuteNonQuery();
    }
}
