using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security.SignInMethods;

namespace VirtoCommerce.XFrontend.Core.Schemas;

public class SignInMethodType : ObjectGraphType<SignInMethod>
{
    public SignInMethodType()
    {
        Field(x => x.Kind);
        Field(x => x.Name);
        Field(x => x.DisplayName, nullable: true);
        Field(x => x.CanCreateKey);
        Field(x => x.CanRenameKey);
        Field(x => x.CanDeleteKey);
        Field(x => x.Keys, type: typeof(ListGraphType<NonNullGraphType<SignInKeyType>>));
    }
}
