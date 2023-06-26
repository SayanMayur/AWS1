namespace WebApi.Entities;

using System;
using System.Text.Json.Serialization;

public class m_patient_dicom
{
    public m_patient m_pat_info { get; set; }
    public t_patient_files[] patient_files { get; set; }

}

public class m_patient
{
    public int center_key { get; set; }
    public int patient_key { get; set; }
    public int ref_patient_key { get; set; }
    public string patient_id { get; set; }
    public string patient_id_ref { get; set; }
    public string patient_name { get; set; }
    public string patient_sex { get; set; }
    public int patient_age { get; set; }
    public string address { get; set; }
    public string state { get; set; }
    public string zipcode { get; set; }
    public string country { get; set; }
    public string phone_no { get; set; }
    public string email { get; set; }
    public DateTime regn_on { get; set; }
    public string refer_physician { get; set; }
    public string modality { get; set; }
    public string body_part { get; set; }
    public string study_description { get; set; }
    public string study_file { get; set; }
    public string remarks { get; set; }
    public string internal_comments { get; set; }
    public string series_description { get; set; }

    public int image_group_count { get; set; }
    public int image_total_count { get; set; }
    public int image_fail_count { get; set; }

    public int created_by { get; set; }
    public DateTime created_on { get; set; }
    public int updated_by { get; set; }
    public DateTime updated_on { get; set; }
    public string comments { get; set; }
    public string comments_file { get; set; }
    public string notes { get; set; }
    public string report_status { get; set; }
    public DateTime last_report_on { get; set; }
    public string print_flag { get; set; }
    public DateTime last_printed_on { get; set; }
    public string report_id { get; set; }
    public string report_content { get; set; }
    public int doctor_key { get; set; }
    public string priority { get; set; }
    public string study_key { get; set; }
    public string dicom_path { get; set; }
    public string fileupload_status { get; set; }
    public string regn_date { get; set; }
    public string regn_time { get; set; }


}

public class t_patient_files
{
    public int center_key { get; set; }
    public int patient_key { get; set; }
    public int file_seq_no { get; set; }
    public string file_name { get; set; }
    public int test_key { get; set; }
    public string file_scan_text { get; set; }
    public string modality { get; set; }
    public string body_part { get; set; }
    public int created_by { get; set; }
    public DateTime created_on { get; set; }

}
