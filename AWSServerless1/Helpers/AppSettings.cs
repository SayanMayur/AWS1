namespace WebApi.Helpers;

public class AppSettings
{
    public string Secret { get; set; }
    public int TokenExpiryInMinute { get; set; }
    public string ConnectionString { get; set; }
    public string DICOM_UploadPath { get; set; }
    public string IMAGE_UploadPath { get; set; }
    public string Attach_UploadPath { get; set; }

    public string EnableLog { get; set; }

    public string HostingUrl { get; set; }
    public string Port { get; set; }


}