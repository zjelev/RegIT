using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Contracts.Models;

public class ResponsibleViewModel
{
    public string? SearchString { get; set; }
    public string? Responsible { get; set; }
    public SelectList? Responsibles { get; set; }
    public List<Contract>? Contracts { get; set; }
}