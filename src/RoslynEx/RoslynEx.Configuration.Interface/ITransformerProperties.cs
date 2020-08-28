namespace RoslynEx
{
    public interface ITransformerProperties
    {
        T Get<T>(string name);
    }
}
