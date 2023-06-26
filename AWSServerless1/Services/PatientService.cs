namespace WebApi.Services;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Models;

using TeleradiologyDataAccess; 
using TeleradiologyCore.Extensions;
using MySqlX.XDevAPI;
using Microsoft.AspNetCore.Mvc;
using TeleradiologyDataAccess.DAL;
using System.IO;
using Org.BouncyCastle.Asn1.Ocsp;
using Amazon;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.S3.Model;

public interface IPatientService: ICommonService
{
    //IReadOnlyList<m_patient> GetAll();

    IReadOnlyList<m_patient> GetPatient(int user_key, int center_key, string report_status);

    m_patient GetById(int Patient_key);

    m_patient PatientFileUpload([FromForm] PatientUploadRequest postedFiles); 
}

public class PatientService : IPatientService
{
    private List<m_patient> _patients;

    private readonly AppSettings _appSettings;

    public PatientService(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    
    /*public IReadOnlyList<m_patient> GetAll()
    {
        var _db = new DatabaseContext(_appSettings.ConnectionString);
        _db.OpenConnection();

        string sql = "call patient_studylist(1)";
        var ds = _db.ExecuteDataSet(sql, CommandType.Text);

        _patients = ds.Tables[0].TableToList<m_patient>();

        return _patients;
    }
    */

    public IReadOnlyList<m_patient> GetPatient(int user_key, int center_key, string report_status)
    {
        var _db = new DatabaseContext(_appSettings.ConnectionString);
        _db.OpenConnection();

        
        string sql = "call patient_studylist("+ user_key.ToString() + ","+ center_key.ToString() + ",'"+ report_status + "','', '', '')";
        var ds = _db.ExecuteDataSet(sql, CommandType.Text);

        _patients = ds.Tables[0].TableToList<m_patient>();

        return _patients;
    }
    
    public m_patient GetById(int patient_key)
    {
        var _db = new DatabaseContext(_appSettings.ConnectionString);
        _db.OpenConnection();

        string sql = "call fetch_user_dtls(" + patient_key + ")";
        var ds = _db.ExecuteDataSet(sql, CommandType.Text);

        _patients = ds.Tables[0].TableToList<m_patient>();

        return _patients.FirstOrDefault(x => x.patient_key == patient_key);
    }

    // helper methods

    public m_patient PatientFileUpload([FromForm] PatientUploadRequest postedFiles)
    {
        Common LobjCom = new Common();
        var _db = new DatabaseContext(_appSettings.ConnectionString);
        _db.OpenConnection();

        string sql = "";

        if (LobjCom.MYTString(postedFiles.action_tag) == "REPORT_SUBMIT"
            || LobjCom.MYTString(postedFiles.action_tag) == "ADD_NOTES")
        {
            // save the patient details
            sql = "call patient_report_update('" + postedFiles.user_key.ToString()
                + "','" + postedFiles.center_key.ToString()
                + "','" + postedFiles.patient_key.ToString()
                + "','" + LobjCom.MYTDBString(postedFiles.action_tag)
                + "','" + LobjCom.MYTDBString(postedFiles.report_content)
                + "','" + LobjCom.MYTDBString(postedFiles.notes)
                + "')";

            var ds = _db.ExecuteDataSet(sql, CommandType.Text);
            _patients = ds.Tables[0].TableToList<m_patient>();

            return _patients.FirstOrDefault(x => x.patient_key == postedFiles.patient_key);
        }
        else
        {
            var patient_key = postedFiles.patient_key;
            var patient_id = "";

            sql = "call patientkey_generate('" + postedFiles.user_key.ToString()
                + "','" + postedFiles.center_key.ToString()
                + "','" + patient_key.ToString()
                + "')";

            var ds = _db.ExecuteDataSet(sql, CommandType.Text);
            patient_key = Convert.ToInt32(ds.Tables[0].Rows[0]["patient_key"]);
            patient_id = Convert.ToString(ds.Tables[0].Rows[0]["patient_id"]);

            List<string> uploadedStudyFiles = new List<string>();

            string path = _appSettings.Attach_UploadPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            //************study file attachment************//
            if (postedFiles.study_file != null)
            {
                // center and patient combination 
                path = Path.Combine(path, postedFiles.center_key.ToString() + "_" + patient_key.ToString());
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName_study_file = Path.GetFileName(postedFiles.study_file.FileName);

                using (FileStream stream = new FileStream(Path.Combine(path, fileName_study_file), FileMode.Create))
                {
                    postedFiles.study_file.CopyTo(stream);
                    uploadedStudyFiles.Add(fileName_study_file);
                }
                fileName_study_file = null;
            }

            //***************Hosted at S3 Bucket*****************//
            // Configure your AWS credentials
            var awsCredentials = new BasicAWSCredentials("AKIA3IDRZH6YSZ63HNOK", "rU6L/gRr/1+PpQ+ydw1iHcn8XX52l3kgFiZnDsOC");

            // Configure the AWS region
            var region = RegionEndpoint.APSouth1; // Change it to your desired region

            // Configure the S3 client
            var s3Client = new AmazonS3Client(awsCredentials, region);

            //2.Upload a file to a bucket:
            var bucketName = "lambdabucketmumbai123";
            var filePath = "D:/RND/ListBucketsExample/files/text.txt";
            var key = "text.txt"; // The key under which to store the file in the bucket

            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = fileStream
                });
            }

            //************Patient DICOM file Upload************//
            path = _appSettings.DICOM_UploadPath;//Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // center and patient combination 
            path = Path.Combine(path, postedFiles.center_key.ToString() + "_" + patient_key.ToString());
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // image upload file
            string pathImage = _appSettings.IMAGE_UploadPath;
            if (!Directory.Exists(pathImage))
            {
                Directory.CreateDirectory(pathImage);
            }

            // center and patient combination 
            pathImage = Path.Combine(pathImage, postedFiles.center_key.ToString() + "_" + patient_key.ToString());
            if (!Directory.Exists(pathImage))
            {
                Directory.CreateDirectory(pathImage);
            }

            var m_patient_dicom = new m_patient();
            List<string> uploadedFiles = new List<string>();
            var path_file = path;

            foreach (IFormFile postedFile in postedFiles.UploadFiles)
            {
                string fileName = Path.GetFileName(postedFile.FileName);
                if (fileName.ToUpper().EndsWith(".DCM") == false)
                {
                    path_file = pathImage;
                }
                else
                {
                    path_file = path;
                }

                using (FileStream stream = new FileStream(Path.Combine(path_file, fileName), FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                    uploadedFiles.Add(fileName);
                    //ViewBag.Message += string.Format("<b>{0}</b> uploaded.<br />", fileName);

                }
            }

            // read petient file 
            m_patient_dicom = TagReaderMethod(path, postedFiles.center_key, patient_key, postedFiles.user_key);

            var ref_patient_key = 0;
            m_patient_dicom.priority = "N";

            // save the patient details
            sql = "call patient_save('" + postedFiles.user_key.ToString()
                + "','" + postedFiles.center_key.ToString()
                + "','" + patient_key.ToString()
                + "','" + ref_patient_key.ToString()
                + "','" + LobjCom.MYTDBString(patient_id)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.patient_id_ref)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.patient_name)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.patient_sex)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.patient_age)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.address)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.state)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.zipcode)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.country)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.phone_no)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.email)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.refer_physician)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.modality)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.body_part)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.study_description)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.study_file)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.remarks)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.internal_comments)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.priority)
                + "','" + LobjCom.MYTDBString(m_patient_dicom.series_description)
                + "','" + LobjCom.MYTDBString(postedFiles.uploadfile_count)
                + "')";

            ds = _db.ExecuteDataSet(sql, CommandType.Text);

            _patients = ds.Tables[0].TableToList<m_patient>();

            return _patients.FirstOrDefault(x => x.patient_key == patient_key);
        }
    }

    public m_patient TagReaderMethod(string PathToDicom, int center_key, int patient_key, int user_key)
    {
        var m_pat_info = new m_patient();
        var fileName = "";
        Common LobjCom = new Common();
        int LiCounter = 0, LiFileCounter = 0;

        try
        {

            string[] filePaths = Directory.GetFiles(PathToDicom);

            foreach (string filePath in filePaths)
            {
                LiFileCounter++;
                LiCounter = 0;

                try
                {

                    var file = FellowOakDicom.DicomFile.Open(filePath);
                    fileName = filePath;
                    // string LsStr = "";
                    foreach (var tag in file.Dataset)
                    {
                        //Console.WriteLine($" {tag} '{file.Dataset.GetValueOrDefault(tag.Tag, 0, "")}'");
                        //LsStr = LsStr + " <br/> " + $" {tag} '{file.Dataset.GetValueOrDefault(tag.Tag, 0, "")}'";

                        LiCounter++;
                        /*if(LiFileCounter==15 && LiCounter == 32)
                        {
                            var ss="";
                        }*/

                        string tagValue = $"{file.Dataset.GetValueOrDefault(tag.Tag, 0, "")}";

                        switch (tag.Tag.ToString().ToUpper())
                        {
                            case "(0010,0010)": //(0010,0010) PN Patient's Name
                                m_pat_info.patient_name = tagValue;
                                break;
                            case "(0010,0020)": //(0010,0020) LO Patient ID
                                m_pat_info.patient_id_ref = tagValue;
                                break;
                            case "(0010,0040)": //(0010,0040) CS Patient's Sex
                                m_pat_info.patient_sex = tagValue;
                                break;
                            //case "(0010,2160)": //(0010,2160) SH Ethnic Group
                            case "(0010,1010)": //(0010,1010) Patient's Age
                                                //read year only i.e. 048Y
                                if (tagValue.ToUpper().IndexOf("Y") != -1)
                                {
                                    if (tagValue.ToUpper().IndexOf("Y") == tagValue.Length - 1)
                                    {
                                        tagValue = tagValue.Substring(0, tagValue.ToUpper().IndexOf("Y"));
                                    }
                                }
                                m_pat_info.patient_age = LobjCom.MYTNumeric(tagValue);
                                break;

                            case "(0008,0060)": //(0008,0060) CS Modality
                                m_pat_info.modality = tagValue;
                                break;
                            case "(0018,0015)": //(0018,0015) CS Body Part Examined
                                m_pat_info.body_part = tagValue;
                                break;
                            case "(0008,1030)": //(0008,1030) LO Study Description
                                m_pat_info.study_description = tagValue;
                                break;
                            case "(0008,103E)": //(0008,103E) LO Series Description
                                m_pat_info.series_description = tagValue;
                                break;
                            case "(0010,4000)": //(0010,4000) LT Patient Comments
                                m_pat_info.comments = tagValue;
                                break;


                        }

                    }

                    file = null;

                    // save the patient details
                    var sql = "call patient_files_save('" + user_key.ToString()
                        + "','" + center_key.ToString()
                        + "','" + patient_key.ToString()
                        + "','" + LobjCom.MYTDBString(fileName)
                        + "','" + LobjCom.MYTDBString(m_pat_info.modality)
                        + "','" + LobjCom.MYTDBString(m_pat_info.body_part)
                        + "','" + LobjCom.MYTDBString(m_pat_info.study_description)
                        + "','" + LobjCom.MYTDBString(m_pat_info.series_description)
                        + "')";

                    var _db = new DatabaseContext(_appSettings.ConnectionString);
                    var ds = _db.ExecuteDataSet(sql, CommandType.Text);

                }
                catch (Exception e1)
                {
                    Console.WriteLine($"Error occured during DICOM file dump operation -> {e1.StackTrace}");
                }
            }

            filePaths = null;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error occured during DICOM file dump operation -> {e.StackTrace}");
        }
        finally
        {
            LobjCom = null;
        }
        return m_pat_info;
    }
}