internal interface IEntry
{
    string Name { get; }
    Stream Open();
}

internal interface IEntry<T> : IEntry
{
    T Entry { get; }
}
