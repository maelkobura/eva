using System.Reflection;
using Eva.AuthorityServer.Security;
using Eva.AuthorityServer.System;
using Eva.Commons.Util;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Tests.EAS;

public class KeysTest
{
    public KeysTest()
    {
        EvaLogger.Init("EAS TEST");
    }
    
    private void ResetState()
    {
        // Reset IsInitialized
        var field = typeof(KeysManager)
            .GetField("<IsInitialized>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);

        field!.SetValue(null, false);
    }

    private void SetConfiguration(Dictionary<string, string?> values)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        #if TEST
            Configuration.SetContentForTest(config);
        #endif
    }

    [Fact]
    public void Init_Should_Set_IsInitialized_To_True()
    {
        ResetState();

        SetConfiguration(new Dictionary<string, string?>
        {
            ["security:keys:showPrivateKey"] = "false"
        });

        KeysManager.Init();

        KeysManager.IsInitialized.Should().BeTrue();
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void Init_Should_Not_Throw_With_Valid_Config(string value)
    {
        ResetState();

        SetConfiguration(new Dictionary<string, string?>
        {
            ["security:keys:showPrivateKey"] = value
        });

        var act = () => KeysManager.Init();

        act.Should().NotThrow();
    }

    [Fact]
    public void Init_Should_Default_To_False_When_Config_Missing()
    {
        ResetState();

        SetConfiguration(new Dictionary<string, string?>());

        var act = () => KeysManager.Init();

        act.Should().NotThrow();
        KeysManager.IsInitialized.Should().BeTrue();
    }
}