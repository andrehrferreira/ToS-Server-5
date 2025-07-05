
public static class Namespaces
{
    public static IEnumerable<Type> GetTypesInNamespace(string namespaceName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && type.Namespace == namespaceName);
    }
}