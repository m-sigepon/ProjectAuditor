using System.Linq;
using Mono.Cecil;
using UnityEngine;

internal static class ScriptAnalyzerHelper
{
    private static readonly int monoBehaviourHashCode = "UnityEngine.MonoBehaviour".GetHashCode();

    private static string[] monoBehaviourMagicMethods = new[]
        {"Awake", "Start", "OnEnable", "OnDisable", "Update", "LateUpdate", "OnEnable", "FixedUpdate"};

    private static string[] monoBehaviourUpdateMethods = new[]
        {"Update", "LateUpdate", "FixedUpdate"};

    public static bool IsMonoBehaviour(TypeReference typeReference)
    {
        TypeDefinition typeDefinition = null;
        try
        {
            typeDefinition = typeReference.Resolve();
        }
        catch (AssemblyResolutionException e)
        {
            Debug.LogWarning(e);
            return false;
        }

        if (typeDefinition.FullName.GetHashCode() == monoBehaviourHashCode && typeDefinition.Module.Name.Equals("UnityEngine.CoreModule.dll"))
            return true;

        if (typeDefinition.BaseType != null)
            return IsMonoBehaviour(typeDefinition.BaseType);

        return false;
    }

    public static bool IsMonoBehaviourMagicMethod(MethodDefinition methodDefinition)
    {
        return monoBehaviourMagicMethods.Contains(methodDefinition.Name);
    }
    
    public static bool IsMonoBehaviourUpdateMethod(MethodDefinition methodDefinition)
    {
        return monoBehaviourUpdateMethods.Contains(methodDefinition.Name);
    }

    public static bool IsPerformanceCriticalContext(TypeReference typeReference, MethodDefinition methodDefinition)
    {
        return IsMonoBehaviour(typeReference) && monoBehaviourUpdateMethods.Contains(methodDefinition.Name);
    }
}