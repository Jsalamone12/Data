#pragma warning disable CS8618 

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Data.Models;


public class Zip
{
    [Key]
    public int ZipId { get; set; }


    [Required(ErrorMessage = "is required.")]

    public string zipUrl { get; set; }


    [ForeignKey("User")]
    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
