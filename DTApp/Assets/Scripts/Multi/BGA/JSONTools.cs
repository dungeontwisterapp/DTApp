using UnityEngine;
using System.Collections;

public class JSONTools {

    static public bool HasFieldOfTypeString(JSONObject json, string field)
    {
        return json.HasField(field) && json.GetField(field).IsString;
    }

    static public bool HasFieldOfTypeContainer(JSONObject json, string field) // Object or Array
    {
        return json.HasField(field) && json.GetField(field).isContainer;
    }

    static public bool HasFieldOfTypeArray(JSONObject json, string field)
    {
        return json.HasField(field) && json.GetField(field).IsArray;
    }

    static public bool HasFieldOfTypeObject(JSONObject json, string field)
    {
        return json.HasField(field) && json.GetField(field).IsObject;
    }

    static public bool HasFieldOfTypeNumber(JSONObject json, string field)
    {
        return json.HasField(field) && json.GetField(field).IsNumber;
    }

    // Return string, or number converted to string, or null
    static public string GetStrValue(JSONObject obj, string key)
    {
        if (HasFieldOfTypeString(obj, key))
            return obj.GetField(key).str;
        else if (HasFieldOfTypeNumber(obj, key))
            return Mathf.RoundToInt(obj.GetField(key).n).ToString();
        else
            return null;
    }

    // Return number or string succesfully converted to int, or defaultValue
    static public int GetIntValue(JSONObject obj, string key, int defaultValue = -1)
    {
        if (HasFieldOfTypeString(obj, key))
        {
            int result;
            if (int.TryParse(obj.GetField(key).str, out result))
                return result;
            else
                return defaultValue;
        }
        else if (HasFieldOfTypeNumber(obj, key))
            return Mathf.RoundToInt(obj.GetField(key).n);
        else
            return defaultValue;
    }

    // Return true if a number or string succesfully converted to expected int value
    static public bool GetBoolValue(JSONObject obj, string key, int expectedValue = 1)
    {
        int defaultValue = (expectedValue == 0) ? -1 : 0; 
        return (GetIntValue(obj, key, defaultValue) == expectedValue);
    }

    static public string FormatJsonDisplay(JSONObject json) { return FormatJsonDisplay( json.ToString() ); }
    static public string FormatJsonDisplay(string input)
    {
        string output = "";
        string indent = "";
        string step = "  ";
        bool insideString = false;
        string openbracket = "[{";
        string closebracket = "]}";
        string endofline = ",";
        foreach (char c in input)
        {
            if (c == '"') insideString = !insideString;
            string C = c.ToString();
            if (!insideString && openbracket.Contains(C))
            {
                indent += step;
                output += C + '\n' + indent;
            }
            else if (!insideString && closebracket.Contains(C))
            {
                indent = indent.Substring(0, indent.Length - step.Length);
                output += '\n' + indent + C;
            }
            else if (!insideString && endofline.Contains(C))
            {
                output += C + '\n' + indent;
            }
            else
            {
                output += C;
            }
        }
        return output;
    }
}
