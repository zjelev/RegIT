using Microsoft.AspNetCore.Mvc.Rendering;

namespace Regit.Models;

public class ContractsViewModel
{
    public const string props = "Id,SignedOn,Title,ValidFrom,RegNum,Subject,Value,Term,ControlledBy,Responsible,Guarantee,WaysOfCollection,InformationList,Status,FilePath";

    // public static string[] propsArr = props.Split(',', StringSplitOptions.RemoveEmptyEntries);
    public string? SearchString { get; set; }
    public string? Responsible { get; set; }
    public SelectList? Responsibles { get; set; }
    public List<Contract>? Contracts { get; set; }
}