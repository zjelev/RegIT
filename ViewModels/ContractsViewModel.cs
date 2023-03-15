namespace Regit.Models;

public class ContractsViewModel
{
    public const string props = "Id,SignedOn,ValidFrom,RegNum,Subject,Value,Term,ControlledById," +
        "ResponsibleId,Guarantee,WaysOfCollection,InformationList,OwnerId,Status,File,FilePath,FileBytes";

    public string? searchSubject { get; set; }
    public string? selectDepartment { get; set; }
    public IEnumerable<Department>? Departments { get; set; }
    public IEnumerable<Contract>? Contracts { get; set; }
}