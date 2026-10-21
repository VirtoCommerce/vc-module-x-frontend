using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security.SignInMethods;

namespace VirtoCommerce.XFrontend.Core.Schemas;

public class SignInKeyType : ObjectGraphType<SignInKey>
{
    public SignInKeyType()
    {
        Field(x => x.Id);
        Field(x => x.Name, nullable: true);
        Field(x => x.CreatedDate, nullable: true);
        Field(x => x.ModifiedDate, nullable: true);
        Field(x => x.State, nullable: true);
    }
}
