namespace RoslynEx
{
    // TODO: should this really differ from MS.CA.ISourceGenerator only by namespace?
    public interface ISourceGenerator
    {
        void Execute(GeneratorContext context);
    }
}
