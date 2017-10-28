using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Multi
{
    namespace BGA
    {
        public class TableListData
        {
            private JSONObject _json = null;

            Dictionary<string, TableData> _tables = new Dictionary<string, TableData>();

            public void Update(JSONObject json)
            {
                _json = json;
                _tables.Clear();
                if (_json == null) return;
                for (int i = 0; i < _json.Count; ++i)
                {
                    string table_id = _json.keys[i];
                    TableData table = new TableData(_json[i]);
                    if (table.isValid)
                    {
                        _tables.Add(table_id, table);
                    }
                }
            }

            public int Count { get { return _tables.Count; } }

            public List<string> GetSortedKeys()
            {
                List<string> sorted = new List<string>();
                foreach (string key in _tables.Keys)
                {
                    sorted.Add(key);
                }
                return sorted;
            }

            public TableData this[string key]
            {
                get
                {
                    return _tables[key];
                }
            }

            public bool ContainsKey(string key)
            {
                return _tables.ContainsKey(key);
            }
        }
    }
}
