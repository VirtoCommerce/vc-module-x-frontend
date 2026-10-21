using GraphQL.Types;
using VirtoCommerce.XFrontend.Core.Commands;

namespace VirtoCommerce.XFrontend.Core.Schemas;

public class InputDeleteSignInKeyType : InputObjectGraphType<DeleteSignInKeyCommand>
{
    public InputDeleteSignInKeyType()
    {
        Field(x => x.SignInMethodName);
        Field(x => x.KeyId);
    }
}
