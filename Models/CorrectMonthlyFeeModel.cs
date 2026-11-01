using System.ComponentModel.DataAnnotations;

namespace _2Korriku.Models;

public class CorrectMonthlyFeeModel
{
    public int PeriodId { get; set; }
    public int ExpectedRevision { get; set; }
    public MonthlyInvoiceView? Invoice { get; set; }
    [Required, Range(typeof(decimal), "0", "999999999.99")]
    public decimal? Amount { get; set; }
    [Required(ErrorMessage = "Shkruani arsyen e korrigjimit."), StringLength(200)]
    public string Reason { get; set; } = "";
}
