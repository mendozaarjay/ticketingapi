public class UserAccessMatrix
{
    public string Code { get; set; }
    public string Description { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanSave { get; set; }
    public bool CanDelete { get; set; }
    public bool CanSearch { get; set; }
    public bool CanPrint { get; set; }
    public bool CanExport { get; set; }
    public bool CanAccess { get; set; }
}