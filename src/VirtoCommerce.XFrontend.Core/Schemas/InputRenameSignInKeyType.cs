using GraphQL.Types;
using VirtoCommerce.XFrontend.Core.Commands;

namespace VirtoCommerce.XFrontend.Core.Schemas;

public class InputRenameSignInKeyType : InputObjectGraphType<RenameSignInKeyCommand>
{
    public InputRenameSignInKeyType()
    {
        Field(x => x.SignInMethodName);
        Field(x => x.KeyId);
        Field(x => x.Name, nullable: true);
    }
}
