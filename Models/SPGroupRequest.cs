namespace S3WebApi.Models;

public class SPGroupRequest
{
    public string SiteUrl { get; set; }
    public string Email { get; set; }

}


    public class PrincipalInfo
    {
        public int? Id { get; set; }                   // SharePoint principal id (int) when present
        public string Title { get; set; }              // Display name
        public string LoginName { get; set; }          // Claim string (may contain AAD GUID)
        public int PrincipalType { get; set; }         // SPPrincipalType bitmask (1=user,4=security group,8=sp group)
        public bool IsSharePointGroup { get; set; }    // true when principal is a SharePoint group (Member.PrincipalType == 8)
    }

public class SharePointGroupInfo
{
    public int GroupId { get; set; }               // SharePoint group id (integer)
    public string Title { get; set; }
    public string SiteUrl { get; set; }
    public List<PrincipalInfo> Members { get; set; } = new List<PrincipalInfo>();
}
    
     public class SPGroupResponse
    {
        public int GroupId { get; set; }               // SharePoint group id (integer)
        public string Title { get; set; }
    }



