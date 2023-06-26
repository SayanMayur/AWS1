namespace WebApi.Models;

using WebApi.Entities;

public class PatientResponse
{
    public int center_key { get; set; }
    public int patient_key { get; set; }
    public string patient_name { get; set; }
    public string study_key { get; set; }
  
    public PatientResponse(m_patient patient)
    {
        center_key = patient.center_key;
        patient_key = patient.patient_key;
        patient_name = patient.patient_name;
        study_key = patient.study_key; 
    }
}
