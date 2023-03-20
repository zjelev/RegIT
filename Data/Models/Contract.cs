using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Regit.Models;

public class Contract
{
    // public static string[] propsArr = props.Split(',', StringSplitOptions.RemoveEmptyEntries);
    public int Id { get; set; }

    [Required, Display(Name = "Подписан на"), DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = false)]
    public DateOnly SignedOn { get; set; }

    [Required, Display(Name = "Валиден от"), DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
    public DateOnly ValidFrom { get; set; }

    [Required, Display(Name = "Рег. №"), ]
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

    public int? ControlledById { get; set; }

    [Display(Name = "Контролиращ отдел")]
    public virtual Department? ControlledBy { get; set; }

    public int? ResponsibleId { get; set; }

    [Display(Name = "Отговорен отдел")]
    public virtual Department? Responsible { get; set; }

    [Display(Name = "Гаранция")]
    [DataType(DataType.Currency), Column(TypeName = "decimal(18, 2)")]
    public decimal? Guarantee { get; set; }
    // public HashSet<string> Annexes { get; set; }
    // public HashSet<decimal> Penalties { get; set; }
    // public HashSet<decimal> GuaranteesCollected { get; set; }

    [Display(Name = "Начин на събиране")]
    public string? WaysOfCollection { get; set; }

    [Display(Name = "Инф. лист")]
    public string? InformationList { get; set; }

    public string? OwnerId { get; set; }

    [Display(Name = "Добавен от")]
    public virtual IdentityUser? Owner { get; set; }

    [Display(Name = "Статус")]
    public ContractStatus? Status { get; set; }

    [Display(Name = "Файл")]
    public virtual IEnumerable<UploadedFile>? Files { set; get; }
}

public enum ContractStatus
{
    Качен,
    Одобрен,
    Отхвърлен
}