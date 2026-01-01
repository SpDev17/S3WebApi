using System.Web;

namespace S3WebApi.Types
{
    public static class Utility
    {
        public static string GetLibraryName(InstitutionLibrary library)
        {
            return library switch
            {
                InstitutionLibrary.AccountManagement => "Account Management",
                InstitutionLibrary.Placements => "Placements",
                InstitutionLibrary.Transactions => "Transactions",
                InstitutionLibrary.Policies => "Policies",
                InstitutionLibrary.Fiduciary => "Fiduciary",
                InstitutionLibrary.Claims => "Claims",
                InstitutionLibrary.Projects => "Projects",
                InstitutionLibrary.Inbox => "Inbox",
                _ => throw new ArgumentOutOfRangeException(nameof(library), library, null)
            };
        }

        public static InstitutionLibrary GetLibrary(string name)
        {
            var library =
                Enum.TryParse<InstitutionLibrary>(
                    HttpUtility.UrlDecode(name).Replace(" ", string.Empty), true, out var result);

            if (!library)
                throw new InvalidOperationException("Invalid INST library");

            return result;
        }

        public static string GetHiddenLibraryName(InstitutionLibrary library)
        {
            return library switch
            {
                InstitutionLibrary.AccountManagement => "HiddenAccountManagement",
                InstitutionLibrary.Placements => "HiddenPlacements",
                InstitutionLibrary.Transactions => "HiddenTransactions",
                InstitutionLibrary.Policies => "HiddenPolicies",
                InstitutionLibrary.Fiduciary => "HiddenFiduciary",
                InstitutionLibrary.Claims => "HiddenClaims",
                InstitutionLibrary.Projects => "HiddenProjects",
                InstitutionLibrary.Inbox => throw new InvalidOperationException(),
                _ => throw new ArgumentOutOfRangeException(nameof(library), library, null)
            };
        }
    }
}
