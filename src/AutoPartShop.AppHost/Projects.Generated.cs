using Aspire.Hosting;

namespace Projects
{
    // Placeholder project metadata types used by the AppHost builder.
    // These implement IProjectMetadata so they can be used as TProject type parameters.
    public sealed class AutoPartShop_Api : IProjectMetadata
    {
        // Absolute path to the API project file (resolved for this workspace)
        public string ProjectPath => @"D:\\AI\\SujanMotors\\src\\AutoPartShop.Api\\AutoPartShop.Api.csproj";
    }

    public sealed class AutoPartShop_Web : IProjectMetadata
    {
        // Absolute path to the Web project file (resolved for this workspace)
        public string ProjectPath => @"D:\\AI\\SujanMotors\\src\\AutoPartShop.Web\\AutoPartShop.Web.csproj";
    }
}
