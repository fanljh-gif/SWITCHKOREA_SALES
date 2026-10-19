using SalesManagement.Setup;

namespace SalesManagement.Tests;

public sealed class InitialAccountCatalogTests
{
    [Fact]
    public void Default_setup_accounts_use_approved_login_ids()
    {
        Assert.Equal(
            ["jhlee", "ptw0517", "cmc", "sylee", "cslee"],
            InitialAccountCatalog.All.Select(x => x.LoginId));
    }
}
