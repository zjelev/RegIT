using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Regit.Models;

public class Contract
{
    public int Id { get; set; }

    [Required, Display(Name = "Подписан на"), DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
    public DateTime SignedOn { get; set; }

    [Required, Display(Name = "Валиден от"), DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
    public DateTime ValidFrom { get; set; }

    [Required, Display(Name = "Рег. №")]
    public string RegNum { get; set; }

    [Required, Display(Name = "Предмет")]
    // [RegularExpression(@"[^\s\p{IsCyrillic}A-Za-z0-9,.-]+")]
    [StringLength(100, MinimumLength = 5)]
    public string? Subject { get; set; }

    [Display(Name = "Стойност лв. без ДДС"), DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Value { get; set; }

    [Display(Name = "Срок")]
    public string? Term { get; set; }

    [Display(Name = "Контролиращ отдел")]
    public Department? ControlledBy { get; set; }

    [Display(Name = "Отговорен отдел")]
    public Department? Responsible { get; set; }

    [DataType(DataType.Currency), Column(TypeName = "decimal(18, 2)")]
    [Display(Name = "Гаранция")]
    public decimal? Guarantee { get; set; }
    // public HashSet<string> Annexes { get; set; }
    // public HashSet<decimal> Penalties { get; set; }
    // public HashSet<decimal> GuaranteesCollected { get; set; }

    [Display(Name = "Начин на събиране")]
    public string? WaysOfCollection { get; set; }

    [Display(Name = "Инф. лист")]
    public string? InformationList { get; set; }

    [Display(Name = "Инф. лист")]
    public string? OwnerID { get; set; }

    [Display(Name = "Инф. лист")]
    public ContractStatus Status { get; set; }    
}

public enum ContractStatus
{
    Submitted,
    Approved,
    Rejected
}