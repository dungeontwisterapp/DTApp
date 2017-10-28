using UnityEngine;
using System.Collections;

namespace Multi
{
    namespace BGA
    {
        public class JSON
        {
            protected JSONObject _json;

            public string StringFieldAccess(string field)
            {
                try
                {
                    switch (_json.GetField(field).type)
                    {
                        case JSONObject.Type.STRING:
                            return _json.GetField(field).str;
                        default:
                            return _json.GetField(field).ToString();
                    }

                }
                catch
                {
                    return "????";
                }
            }

            public JSON()
            {
                _json = null;
            }

            public JSON(JSONObject json)
            {
                _json = json;
            }
        }
    }
}
