using UnityEngine;
using System.Collections;
//using UnityEditor;

public class GlossarySpecificInfo : MonoBehaviour {

    public GlossaryInfoCategory category;
    public int index;

    public void requestAnotherInfo ()
    {
        GameObject[] glossaries = GameObject.FindGameObjectsWithTag("GlossaryContent");
        glossaries[glossaries.GetLength(0) - 1].GetComponent<DisplayGlossaryInfo>().displayAnotherInfo(index, name, category);
        //transform.parent.parent.parent.GetComponent<DisplayGlossaryInfo>().displayAnotherInfo(index, name, category);
    }

}

/*
[CustomEditor(typeof(GlossarySpecificInfo))]
public class GlossarySpecificInfoEditor : Editor
{
    void OnInspectorGUI()
    {
        var myScript = target as GlossarySpecificInfo;

        switch (myScript.category)
        {
            case GlossaryInfoCategory.character: myScript.character = (GlossaryInfoCharacter)EditorGUILayout.EnumPopup(myScript.character);
                break;
            case GlossaryInfoCategory.item: myScript.item = (GlossaryInfoItem)EditorGUILayout.EnumPopup(myScript.item);
                break;
            case GlossaryInfoCategory.room: myScript.room = (GlossaryInfoRoom)EditorGUILayout.EnumPopup(myScript.room);
                break;
            default: Debug.LogError("GlossaryInfoEditor, OnInspectorGUI: L'énumération possède une valeur inconnue");
                break;
        }

    }
}
*/