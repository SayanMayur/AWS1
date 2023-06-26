namespace WebApi.Controllers;

using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using TeleradiologyDataAccess.DAL;
using WebApi.Helpers;
using WebApi.Models;
using WebApi.Services;

[ApiController]
[Route("[controller]")]
public class PatientsController : ControllerBase
{
    private IPatientService _PatientService;

    
    public PatientsController(IPatientService PatientService)
    {
        _PatientService = PatientService;
    }

    /*[HttpGet]
    public IActionResult GetAll()
    {
        var Patients = _PatientService.GetAll();
        return Ok(Patients);
    }*/

    [Authorize]
    [HttpGet]
    public IActionResult GetPatient()
    {
        Common LobjCom = new Common();

        int user_key =0; int center_key = 0; string report_status = "A";

        // get para,eter from query string
        user_key = LobjCom.MYTNumeric(HttpContext.Request.Query["user_key"].ToString());
        center_key = LobjCom.MYTNumeric(HttpContext.Request.Query["center_key"].ToString());
        report_status = LobjCom.MYTString(HttpContext.Request.Query["report_status"]);
        LobjCom = null;

        var Patients = _PatientService.GetPatient(user_key, center_key, report_status);
        return Ok(Patients);
    }

    
    [HttpPost("patient")]
    public async Task<IActionResult> PatientFileUpload([FromForm] PatientUploadRequest postedFiles)
    {
        try
        {
            //***************Hosted at S3 Bucket*****************//
            // Configure your AWS credentials
            var awsCredentials = new BasicAWSCredentials("AKIA3IDRZH6YSZ63HNOK", "rU6L/gRr/1+PpQ+ydw1iHcn8XX52l3kgFiZnDsOC");

            // Configure the AWS region
            var region = RegionEndpoint.APSouth1; // Change it to your desired region

            // Configure the S3 client
            var s3Client = new AmazonS3Client(awsCredentials, region);

            //2.Upload a file to a bucket:
            var bucketName = "lambdabucketmumbai123";
            var filePath = "E:/MTR Development/BISWAJIT__MALIK-24-Oct-22/b.dcm";
            var key = "b.dcm"; // The key under which to store the file in the bucket

            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                await s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = fileStream
                }).ConfigureAwait(true);
            }


            if (postedFiles.action_tag == null)
            {
                if (postedFiles.UploadFiles is null)
                {
                    return StatusCode(StatusCodes.Status204NoContent);
                }
            }

            var Patients = _PatientService.PatientFileUpload(postedFiles);
            return Ok(Patients);

//            return StatusCode(StatusCodes.Status201Created);

        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex}");
        }

        /*var response = _PatientService.Patient(model);

        if (response == null)
            return BadRequest(new { message = "Failed to save" });

        return Ok(response);*/
    }
    
}
