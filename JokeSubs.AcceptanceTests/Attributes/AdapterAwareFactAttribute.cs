using Xunit;
using JokeSubs.AcceptanceTests.Infrastructure;

namespace JokeSubs.AcceptanceTests.Attributes;

/// <summary>
/// Data sources for adapter-aware theories.
/// These provide the adapter kind(s) that a test should execute against.
/// </summary>
public class AllAdaptersData : TheoryData<AdapterKind>
{
    public AllAdaptersData()
    {
        Add(AdapterKind.Api);
        Add(AdapterKind.Ui);
    }
}

public class ApiOnlyAdapterData : TheoryData<AdapterKind>
{
    public ApiOnlyAdapterData()
    {
        Add(AdapterKind.Api);
    }
}

public class UiOnlyAdapterData : TheoryData<AdapterKind>
{
    public UiOnlyAdapterData()
    {
        Add(AdapterKind.Ui);
    }
}

