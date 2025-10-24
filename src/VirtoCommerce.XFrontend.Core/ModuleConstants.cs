namespace VirtoCommerce.XFrontend.Core;

public static class ModuleConstants
{
    public static class Security
    {
        public static class Permissions
        {
            public const string Create = "x-frontend:create";
            public const string Read = "x-frontend:read";
            public const string Update = "x-frontend:update";
            public const string Delete = "x-frontend:delete";

            public static string[] AllPermissions { get; } =
            [
                Create,
                Read,
                Update,
                Delete,
            ];
        }
    }
}
