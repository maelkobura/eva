using Eva.Commons.Security;

namespace Tests.Commons.Security;

using System.Collections.Generic;
using Eva.Commons.Security.Certificate;
using Xunit;

public class AuthorizationsTests
{
    private static Certificate CreateCert(params string[] auths)
    {
        return new Certificate
        {
            Payload = new CertificatePayload
            {
                Content = new CertificateContent
                {
                    Authorization = { auths }
                }
            }
        };
    }

    [Fact]
    public void ExactMatch_ShouldReturnTrue()
    {
        // Exact authorization should match
        var cert = CreateCert("category.sub1.action");

        Assert.True(Has(cert, "category.sub1.action"));
    }

    [Fact]
    public void NoImplicitInheritance_ShouldReturnFalse()
    {
        // No wildcard -> no inheritance allowed
        var cert = CreateCert("category.sub1");

        Assert.False(Has(cert, "category.sub1.action"));
    }

    [Fact]
    public void Wildcard_ShouldAllowSubtree()
    {
        // Wildcard should allow any deeper authorization
        var cert = CreateCert("category.sub1.*");

        Assert.True(Has(cert, "category.sub1.action"));
        Assert.True(Has(cert, "category.sub1.anything.deep"));
    }

    [Fact]
    public void Wildcard_ShouldNotMatchOtherBranches()
    {
        // Wildcard should not cross branches
        var cert = CreateCert("category.sub1.*");

        Assert.False(Has(cert, "category.sub2.action"));
    }

    [Fact]
    public void GlobalWildcard_ShouldAllowEverything()
    {
        // Global wildcard should allow all authorizations
        var cert = CreateCert("*");

        Assert.True(Has(cert, "anything"));
        Assert.True(Has(cert, "category.sub1.action"));
    }

    [Fact]
    public void DenyExact_ShouldOverrideAllow()
    {
        // Exact deny should override wildcard allow
        var cert = CreateCert(
            "category.sub1.*",
            "-category.sub1.action"
        );

        Assert.False(Has(cert, "category.sub1.action"));
        Assert.True(Has(cert, "category.sub1.other"));
    }

    [Fact]
    public void DenyWildcard_ShouldOverrideSpecificAllow()
    {
        // Deny wildcard should override even specific allow
        var cert = CreateCert(
            "category.sub1.action",
            "-category.sub1.*"
        );

        Assert.False(Has(cert, "category.sub1.action"));
    }

    [Fact]
    public void DenyBranch_ShouldBlockSubtree()
    {
        // Deny subtree inside a broader allow
        var cert = CreateCert(
            "category.*",
            "-category.secret.*"
        );

        Assert.True(Has(cert, "category.public.read"));
        Assert.False(Has(cert, "category.secret.read"));
    }

    [Fact]
    public void DenyExact_ShouldOnlyBlockExact()
    {
        // Exact deny should not block other nodes
        var cert = CreateCert(
            "category.sub1.*",
            "-category.sub1.a"
        );

        Assert.False(Has(cert, "category.sub1.a"));
        Assert.True(Has(cert, "category.sub1.b"));
    }

    [Fact]
    public void MultipleRules_OrderShouldNotMatter()
    {
        // Order independence: deny should always win
        var cert1 = CreateCert(
            "category.sub1.*",
            "-category.sub1.a"
        );

        var cert2 = CreateCert(
            "-category.sub1.a",
            "category.sub1.*"
        );

        Assert.Equal(
            Has(cert1, "category.sub1.a"),
            Has(cert2, "category.sub1.a")
        );

        Assert.False(Has(cert1, "category.sub1.a"));
        Assert.False(Has(cert2, "category.sub1.a"));
    }

    [Fact]
    public void EmptyAuthorization_ShouldReturnFalse()
    {
        // Empty input should be rejected
        var cert = CreateCert("category.sub1.*");

        Assert.False(Has(cert, ""));
        Assert.False(Has(cert, null));
    }

    [Fact]
    public void NoAuthorizationInCert_ShouldReturnFalse()
    {
        // No authorizations -> nothing allowed
        var cert = new Certificate
        {
            Payload = new CertificatePayload
            {
                Content = new CertificateContent()
            }
        };

        Assert.False(Has(cert, "category.sub1.action"));
    }
    
    private static bool Has(Certificate cert, string authorization)
    {
        return Authorizations.Has(cert, authorization);
    }
}