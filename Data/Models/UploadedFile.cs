using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Regit.Models;

public class UploadedFile
{
    public int Id { get; set; }

    [Display(Name = "Файл")]
    public string? Path { get; set; }

    public byte[]? Bytes { get; set; }  // BLOB

    [Display(Name = "Файл")]
    [NotMapped]
    public IFormFile? FormFile { set; get; }

    public int? ContractId { get; set; }
    public virtual Contract? Contract { get; set; }
}
