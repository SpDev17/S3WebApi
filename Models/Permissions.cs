namespace S3WebApi.Models;

public class PermissionsList
{
    public string Access {  get; set; }
    public string Role { get; set; }
    public string SharedBy { get; set; }

    public PermissionsList(string access, string role, string sharedBy)
    {
        Access = access;
        Role = role;
        SharedBy = sharedBy;
    }
}
