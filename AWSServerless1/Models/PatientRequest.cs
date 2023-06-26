namespace WebApi.Models;

using System.ComponentModel.DataAnnotations;

public class PatientRequest
{
    [Required]
    public string? Patientname { get; set; }

    [Required]
    public string? Password { get; set; }
}
public class PatientUploadRequest
{
    public int user_key { get; set; }
    public int center_key { get; set; }
    public int patient_key { get; set; }
    public string? priority { get; set; }

    public int uploadfile_count { get; set; }

    public List<IFormFile>? UploadFiles { get; set; }

    public IFormFile? study_file { get; set; }

    public string? action_tag { get; set; }
    public string? report_content { get; set; }
    public string? notes { get; set; }

}