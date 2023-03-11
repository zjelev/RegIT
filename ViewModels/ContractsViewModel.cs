namespace Regit.Models;

public class ContractsViewModel
{
    public const string props = "Contract_Id,Contract_SignedOn,Contract_Title,Contract_ValidFrom,Contract_RegNum,Contract_Subject,Contract_Value," +
        "Contract_Term,Contract_ControlledBy,Contract_Responsible,Contract_Guarantee,Contract_WaysOfCollection,Contract_InformationList,Contract_Status,Contract_FilePath";

    public string? searchSubject { get; set; }
    public string? selectDepartment { get; set; }
    public IEnumerable<Department>? Departments { get; set; }
    public IEnumerable<Contract>? Contracts { get; set; }
    public Department Department { get; set; }
    public Contract Contract { get; set; }
}