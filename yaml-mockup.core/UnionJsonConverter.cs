using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace YamlMockup.Core;

public class UnionJsonConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return typeof(Union<short, string>) == type;
    }

    public object? ReadYaml(IParser parser, Type type)
    {
        if (parser.Current is not Scalar scalar)
        {
            return new Union<short, string>(0);
        }

        parser.MoveNext();

        if (scalar.Value.Contains(':'))
        {
            return new Union<short, string>(scalar.Value);
        }
        else
        {
            _ = short.TryParse(scalar.Value, out short res);

            return new Union<short, string>(res);
        }
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type) { throw new NotImplementedException(); }
}
