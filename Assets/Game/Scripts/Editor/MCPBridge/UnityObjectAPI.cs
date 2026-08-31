using UnityEngine;
using UnityEditor;

public static class UnityObjectAPI
{
    public static string FindObject(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null)
            return "NOT_FOUND:" + objectName;

        string result = "OBJECT:" + obj.name + "\nCOMPONENTS:";
        foreach (var c in obj.GetComponents<Component>())
        {
            if (c != null)
                result += "\n- " + c.GetType().Name;
        }

        return result;
    }

    public static string AddComponent(string objectName, string componentName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null)
            return "OBJECT_NOT_FOUND";

        System.Type type = System.Type.GetType(componentName);
        if (type == null)
            return "COMPONENT_NOT_FOUND";

        obj.AddComponent(type);
        return "ADDED:" + componentName;
    }
}
