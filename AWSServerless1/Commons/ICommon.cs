using System;
using System.Data;

namespace TeleradiologyDataAccess.DAL
{
    public interface ICommon
    {
        string ConnectionString { get; }
        string RedirectURL { get; }

        string ConvertToNumberFormat(string LsReturnValue, bool LbWithDecimalSeperator);
        string GetApprovalTemplate(string LsOutletID, string LsChainID, string LsApplicationPath, string LsTemplate);
        string GetConnectionString(string LsConnectionType=null);
        string GetOtpConnectionString(string LsConnectionType = null);
        string GetIPAddress();
        char MYTChar(object LobjValue);
        string PFDateFormat(object LobjValue, string FsDateFormat);
        string PFDateFormat(object LobjValue, string FsDateFormat, string LsTimeFormat);
        string PFDateFormat(object LobjValue, string FsDateFormat, string LsTimeFormat, bool LbFullMonthName);
        DateTime PFDateTime(object LobjDateTimeValue);
        string PFDBDateFormat(object LobjDateTimeValue);
        string PFDBNumeric(object LobjValue, int LiDecimalDigit);
        string MYTDBString(object LobjValue);
        double MYTDouble(object LobjValue);
        int MYTNumeric(object LobjValue);
        string MYTNumeric(object LobjValue, int LiDecimalDigit);
        string MYTNumericCheckZero(object LobjValue, int LiDecimalDigit);
        string MYTString(object LobjValue);
        string MYTString(object LobjValue, bool LbTrim);
        void RemoveLogInTrack(string LsUserID, char LcUserType, string LsFolderPath, string LsOutletID, string LsChainID, DateTime LdtLoginDateTime);
        bool SendMail(string argMailTO, string argMailCC, string argMailFrom, string argSubject, string argMailBody, string argSMTPServer, string argSMTPUserName, string argSMTPPassword, int argSMTPPort, string argSMTPEnableSsl, string argFileType, string argCreatedDate, object[] LobjFileLists, string LsUserMail);
        string SetXMLAttributes(string LsColumnName, string LsColumnValue);
        string SetXMLAttributes(string LsColumnName, string LsColumnValue, bool LbWithCDATA);
        bool WriteDataFile(DataSet LdsOutput, string LsOutletID, string LsRoomNo, string LsTableNo, string LsCustomer_Id, string LsSP, string LsSQL);
        bool WriteDataFile_AdminLogin(DataSet LdsOutput, string LsUserId, string LsSP, string LsSQL);
        void WriteLog(string LsForm, string LsMethod);
        void WriteLog(string LsForm, string LsMethod, string LsErrCode, string LsErrMsg);
        void WriteLog(string LsForm, string LsMethod, string LsErrCode, string LsErrMsg, string strUserID, string strOutletID, string LsChainID, string strModuleID, string strProductID, string strCaller);
        bool WriteStringFile(string LsSQL, string LsOutletID, string LsRoomNo, string LsTableNo, string LsCustomer_Id, string LsSP);
    }
}