using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pyanulis.PingPong.DbWatch
{
    public class DbService : IDisposable
    {
        #region SQL scripts

        private const string c_parameterTableName = "@table_name";

        private const string c_createHistoryTableScript = @"
CREATE SCHEMA logging;
CREATE TABLE logging.t_pingpong_history (
        id             serial,
        tstamp         timestamp DEFAULT now(),
        schemaname     text,
        tabname        text,
        operation      text,
        who            text DEFAULT current_user,
        new_val        json,
        old_val        json
);";

        private const string c_createTrackingTriggerScript = @"
CREATE FUNCTION pingpong_tracking_trigger() RETURNS trigger AS $$
        BEGIN
                IF      TG_OP = 'INSERT'
                THEN
                        INSERT INTO logging.t_history (tabname, schemaname, operation, new_val)
                                VALUES (TG_RELNAME, TG_TABLE_SCHEMA, TG_OP, row_to_json(NEW));
                        RETURN NEW;
                ELSIF   TG_OP = 'UPDATE'
                THEN
                        INSERT INTO logging.t_history (tabname, schemaname, operation, new_val, old_val)
                                VALUES (TG_RELNAME, TG_TABLE_SCHEMA, TG_OP,
                                        row_to_json(NEW), row_to_json(OLD));
                        RETURN NEW;
                ELSIF   TG_OP = 'DELETE'
                THEN
                        INSERT INTO logging.t_history (tabname, schemaname, operation, old_val)
                                VALUES (TG_RELNAME, TG_TABLE_SCHEMA, TG_OP, row_to_json(OLD));
                        RETURN OLD;
                END IF;
        END;
$$ LANGUAGE 'plpgsql' SECURITY DEFINER;
";

        private const string c_applyTriggerScript = @$"
CREATE TRIGGER pingpong_trigger BEFORE INSERT OR UPDATE OR DELETE ON {c_parameterTableName}
        FOR EACH ROW EXECUTE PROCEDURE pingpong_tracking_trigger();
";

        private const string c_selectTrackingRecordsScript = @"
SELECT * FROM logging.t_pingpong_history WHERE tstamp > @start_time
";

        #endregion

        private readonly string m_host;
        private readonly string m_user;
        private readonly string m_password;

        private List<WatchItem> m_watchItems = new List<WatchItem>();

        private static DbService m_instance;
        public static DbService Instance = m_instance;

        private DbService(string host, string user, string password)
        {
            m_host = host;
            m_user = user;
            m_password = password;
        }

        public static void Create(string host, string user, string password)
        {
            m_instance?.Dispose();
            m_instance = new DbService(host, user, password);
        }

        public void AddWatchItem(WatchItem item)
        {
            m_watchItems.Add(item);
        }

        public void AddWatchItem(string database, string table)
        {
            WatchItem? item = m_watchItems.FirstOrDefault(i => i.DbName == database);
            if (item == null)
            {
                m_watchItems.Add(new WatchItem(database, new List<string> { table }));
                return;
            }
            item.TableNames.Add(table);
        }

        public void DeleteWatchItem(WatchItem item)
        {
            m_watchItems.Remove(item);
        }

        public async Task ApplyWatch()
        {
            foreach (WatchItem item in m_watchItems)
            {
                NpgsqlDataSource dataSource = NpgsqlDataSource.Create($"Host={m_host};Username={m_user};Password={m_password};Database={item.DbName}");

                await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
                
                await using NpgsqlCommand cmdCreateHistoryTable = new NpgsqlCommand(c_createHistoryTableScript, connection);
                await cmdCreateHistoryTable.ExecuteNonQueryAsync();

                await using NpgsqlCommand cmdCreateTrigger = new NpgsqlCommand(c_createTrackingTriggerScript, connection);
                await cmdCreateHistoryTable.ExecuteNonQueryAsync();

                foreach (string table in item.TableNames)
                {
                    await using NpgsqlCommand cmdApplyTrigger = new NpgsqlCommand(c_applyTriggerScript, connection);
                    cmdApplyTrigger.Parameters.AddWithValue(c_parameterTableName, NpgsqlTypes.NpgsqlDbType.Name, table);
                    await cmdCreateHistoryTable.ExecuteNonQueryAsync();
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
