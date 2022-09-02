namespace YamlMockup.Core;

public class Union<A, B, C> 
{
    private readonly A? _firstObject;
    private readonly B? _secondObject;
    private readonly C? _thirdObject;

    public Union(A @object)
    {
        _firstObject = @object;
    }
    public Union(B @object)
    {
        _secondObject = @object;
    }
    public Union(C @object)
    {
        _thirdObject = @object;
    }

    public bool IsAContent() => _firstObject is not null;
    public bool IsBContent() => _secondObject is not null;
    public bool IsCContent() => _thirdObject is not null;

    public A FirstObject { get => _firstObject ?? throw new NullReferenceException(); }
    public B SecondObject { get => _secondObject ?? throw new NullReferenceException(); }
    public C ThirdObject { get => _thirdObject ?? throw new NullReferenceException(); }
}

public class Union<A, B>
{
    private readonly A? _firstObject;
    private readonly B? _secondObject;

    public Union(A @object)
    {
        _firstObject = @object;
    }
    public Union(B @object)
    {
        _secondObject = @object;
    }

    public bool IsAContent() => _firstObject is not null;
    public bool IsBContent() => _secondObject is not null;

    public A FirstObject { get => _firstObject ?? throw new NullReferenceException(); }
    public B SecondObject { get => _secondObject ?? throw new NullReferenceException(); }
}

