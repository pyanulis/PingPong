using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pyanulis.PingPong.DbWatch
{
    public class WatchItem
    {
        private List<string> m_tableNames = new();
        private string m_dbName;

        public string DbName
        {
            get { return m_dbName; }
            set { m_dbName = value; }
        }

        public List<string> TableNames
        {
            get { return m_tableNames; }
        }


        public WatchItem(string dbName, List<string> tableNames)
        {
            m_tableNames = tableNames;
            m_dbName = dbName;
        }
    }
}
