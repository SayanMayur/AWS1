using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Xml;

namespace TeleradiologyDataAccess.DAL
{   

    public interface IAppSettings
    {

        string WebRootPath { get; }
        string ContentRootPath { get; }
        string ApplicationBasePath { get; }
        string ConnectionString { get; }
        string ConnectionType { get; }
        string OtpConnectionString { get; }
        string LogSQLOutput { get; }
        string WPMiscPath { get; }
        string WriteLogFull { get; }
        string EnableLog { get; }
        string DevLog { get; }
        string Get(string key);
    }

    public class Common : ICommon
    {
        private string FsConnectionString = "", FsRedirectURL = "";
        
        private readonly IAppSettings _appSettings;
        private readonly IHttpContextAccessor _httpContext;
        private IMemoryCache _cache;
        public string ConnectionString
        {
            get
            {
                return FsConnectionString;
            }
        }
        public string RedirectURL
        {
            get
            {
                return FsRedirectURL;
            }
        }
        public Common()
        {
            ;
        }
        public Common(IServiceProvider provider)
        {
            _httpContext = (IHttpContextAccessor)provider.GetService(typeof(IHttpContextAccessor));
            _cache = (IMemoryCache)provider.GetService(typeof(IMemoryCache));
            _appSettings = (IAppSettings)provider.GetService(typeof(IAppSettings));

            if ((_appSettings.EnableLog ?? "").ToLower() == "yes" ||
                (_appSettings.EnableLog ?? "").ToLower() == "y" ||
                (_appSettings.EnableLog ?? "").ToLower() == "true")
            {
                var LbProcess = true;
                try
                {
                    string referer = _httpContext.HttpContext.Request.Headers["Referer"].ToString();
                    if (_httpContext.HttpContext.Request.Host.Host != new Uri(referer).Host)
                    {
                        if (_cache.Get<string>("Cross Site Request Forgery") == null)
                        {

                            var cacheEntry = _cache.GetOrCreate("Cross Site Request Forgery", entry =>
                            {
                                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                                return "Yes";
                            });
                            WriteLog(this.GetType().Name, "clsCommon", "Cross Site Request Forgery", "URL :" + GetAbsoluteUri() + ", UrlReferrer:" + referer);

                            if (_httpContext.HttpContext.Session != null)
                            {
                                _httpContext.HttpContext.Session.Clear();
                            }

                            //_httpContext.HttpContext.Response.Redirect(_appSettings.ApplicationBasePath + "Common/Redirect");
                            _httpContext.HttpContext.Response.Redirect(_appSettings.Get("App:HostingUrl")+ "/");

                            LbProcess = false;
                        }
                    }
                }
                catch { }

                if (LbProcess)
                {

                }

            }
        }

        public string GetConnectionString(string LsConnectionType=null)
        {
            bool LbStatus = false;

            string LsServerName = null;
            string LsDatabaseName = null;
            string LsUserID = null;
            //long ll_value = 7;
            string LsPassword = null;
            string LsOutletID = "", LsUnitID = "";

            string LsDeplXMLPath = null;
            string LsDBName = null;

            DataSet LdsXML = null;
            DataView Ldv = null;
            DataTable LdtFiletrTable = null;

            bool LbConStrFound = false;
            int LiPropID = 0;
            bool LbPropertyFound = true;

            try
            {
                FsConnectionString = _appSettings.ConnectionString;



                // if having conneciton string return
                if (!String.IsNullOrEmpty(FsConnectionString))
                {
                    return FsConnectionString;
                }

                //set the boolean value whether property passs for connection 
                
               
                FsConnectionString = "server=" + LsServerName + ";database=" + LsDatabaseName + ";uid=" + LsUserID + ";pwd=" + LsPassword + ";";

                LbStatus = true;
            }
            catch (Exception LexcErr)
            {
                throw LexcErr;
            }
            finally
            {
                LsServerName = null; LsDatabaseName = null; LsUserID = null; LsPassword = null; LsOutletID = null;
                LsDeplXMLPath = null; LsDBName = null;
                LdsXML = null; Ldv = null; LdtFiletrTable = null; LsUnitID = null;
            }

            return FsConnectionString;
        }
        public string GetOtpConnectionString(string LsConnectionType = null)
        {
            try
            {
                FsConnectionString = _appSettings.OtpConnectionString;
            }
            catch (Exception LexcErr)
            {
                throw LexcErr;
            }

            return FsConnectionString;
        }
        private Uri GetAbsoluteUri()
        {
            var request = _httpContext.HttpContext.Request;
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = request.Scheme;
            uriBuilder.Host = request.Host.Host;
            uriBuilder.Path = request.Path.ToString();
            uriBuilder.Query = request.QueryString.ToString();
            return uriBuilder.Uri;
        }
        public void WriteLog(string LsForm, string LsMethod)
        {
            WriteLog(LsForm, LsMethod, "", "", null, null, null, null, null, null);
            return;
        }

        public void WriteLog(string LsForm, string LsMethod, string LsErrCode, string LsErrMsg)
        {
            WriteLog(LsForm, LsMethod, LsErrCode, LsErrMsg, null, null, null, null, null, null);
            return;
        }
        public void WriteLog(string LsForm, string LsMethod, string LsErrCode, string LsErrMsg,
                            string strUserID, string strOutletID, string LsChainID,
                            string strModuleID, string strProductID, string strCaller)
        {
            string LsFileName = null;
            string LsEx = null;

            FileStream oFileStream = null;
            StreamWriter oStreamWriter = null;

            DataSet oLogFileDS = null;
            DataTable oLogFileDt = null;
            DirectoryInfo dinfo = null;

            try
            {
                if (LsErrMsg == null) { LsErrMsg = ""; }

                // write the redirect log
                if (FsRedirectURL != null)
                {
                    if (FsRedirectURL != "")
                    {
                        LsErrMsg += " - User Session does not found. Redirect to login screen";
                    }
                }

                string LsWriteLogFull = "";

                try
                {
                    LsWriteLogFull = _appSettings.WriteLogFull;
                }
                catch {; }

                if (LsWriteLogFull != null)
                {
                    // if error, then force to write log
                    bool LbErrorFound = false;
                    if (LsErrCode != "")
                    {
                        if (LsErrCode.IndexOf("error") == 0)
                        {
                            if (LsErrCode == "error127")
                            {
                                LbErrorFound = true;
                            }
                        }
                        else
                        {
                            if (LsErrCode != "SQL")
                            {
                                LbErrorFound = true;
                            }
                        }
                    }
                    else if (LsErrMsg != "")
                    {
                        LbErrorFound = true;
                    }

                    if (LbErrorFound == false)
                    {
                        if (LsWriteLogFull.ToUpper() == "X")
                        {
                            return;
                        }

                        if (LsWriteLogFull.ToUpper() != "Y")
                        {
                            if (strModuleID == "") { return; }

                            if (LsForm.ToUpper().IndexOf(".ASPX") != -1) {; }
                            //else if (LsForm.ToUpper().IndexOf(".JS") != -1) { ; }
                            else
                            {
                                return;
                            }

                            if (LsMethod == "PopulateBrowserData()") {; }
                            else if (LsMethod.ToUpper().IndexOf("SAVE") != -1) {; }
                            else if (LsMethod.ToUpper().IndexOf("SHOW") != -1) {; }
                            else if (LsMethod.ToUpper().IndexOf("LOAD") != -1) {; }
                            else
                            {
                                return;
                            }
                        }
                    }
                }

                
            }
            catch (System.Exception ex) { LsEx = ex.ToString(); }
            finally
            {
                oLogFileDt = null; oLogFileDS = null;
                oStreamWriter = null;
                dinfo = null; LsFileName = null; oFileStream = null;
            }
        }


        public bool WriteDataFile(DataSet LdsOutput, string LsOutletID, string LsRoomNo, string LsTableNo, string LsCustomer_Id, string LsSP, string LsSQL)
        {
            try
            {
                //Log Result
                if (_appSettings.LogSQLOutput == "Y")
                {

                    string LsXMLPath = Path.Combine( this.MYTString(_appSettings.WPMiscPath).ToString(),"LOG");

                    //Creating the log folder
                    DirectoryInfo dinfo = new DirectoryInfo(LsXMLPath);
                    if (!dinfo.Exists) { dinfo.Create(); }
                    dinfo = null;

                    //Creating the outlet
                    if (this.MYTString(LsOutletID) != "")
                    {
                        LsXMLPath = Path.Combine(LsXMLPath, this.MYTString(LsOutletID));//+= @"\" + this.MYTString(LsOutletID);
                        dinfo = new DirectoryInfo(LsXMLPath);
                        if (!dinfo.Exists) { dinfo.Create(); }
                    }

                    LsXMLPath = Path.Combine(LsXMLPath, this.MYTString(LsOutletID)
                        + "_" + this.MYTString(LsRoomNo)
                        + "_" + this.MYTString(LsTableNo)
                        + "_" + this.MYTString(LsCustomer_Id)
                        + "_" + DateTime.Now.ToLongTimeString().Replace(":", "-") + ".xml");

                    LdsOutput.WriteXml(LsXMLPath);

                    this.WriteLog(this.GetType().Name, LsXMLPath, LsSP, LsSQL);//log entry
                }
            }
            catch { }
            return true;
        }

        public bool WriteDataFile_AdminLogin(DataSet LdsOutput, string LsUserId, string LsSP, string LsSQL)
        {
            try
            {
                //Log Result
                if (_appSettings.LogSQLOutput == "Y")
                {

                    string LsXMLPath = Path.Combine(this.MYTString(_appSettings.WPMiscPath).ToString(), "LOG");

                    //Creating the log folder
                    DirectoryInfo dinfo = new DirectoryInfo(LsXMLPath);
                    if (!dinfo.Exists) { dinfo.Create(); }
                    dinfo = null;

                    //Creating the outlet
                    if (this.MYTString(LsUserId) != "")
                    {
                        LsXMLPath = Path.Combine(LsXMLPath, this.MYTString(LsUserId));
                        dinfo = new DirectoryInfo(LsXMLPath);
                        if (!dinfo.Exists) { dinfo.Create(); }
                    }

                    LsXMLPath = Path.Combine(LsXMLPath, this.MYTString(LsUserId)
                        + "_" + DateTime.Now.ToLongTimeString().Replace(":", "-") + ".xml");

                    LdsOutput.WriteXml(LsXMLPath);

                    this.WriteLog(this.GetType().Name, LsXMLPath, LsSP, LsSQL);//log entry
                }
            }
            catch { }
            return true;
        }

        public bool WriteStringFile(string LsSQL, string LsOutletID, string LsRoomNo, string LsTableNo, string LsCustomer_Id, string LsSP)
        {
            try
            {
                //Log Result
                if (_appSettings.LogSQLOutput == "Y")
                {

                    string LsFilePath = Path.Combine( this.MYTString(_appSettings.WPMiscPath).ToString() , "LOG");

                    //Creating the log folder
                    DirectoryInfo dinfo = new DirectoryInfo(LsFilePath);
                    if (!dinfo.Exists) { dinfo.Create(); }
                    dinfo = null;

                    //Creating the outlet
                    if (this.MYTString(LsOutletID) != "")
                    {
                        LsFilePath = Path.Combine(LsFilePath, this.MYTString(LsOutletID));
                        dinfo = new DirectoryInfo(LsFilePath);
                        if (!dinfo.Exists) { dinfo.Create(); }
                    }

                    LsFilePath  = Path.Combine(LsFilePath, this.MYTString(LsOutletID)
                        + "_" + this.MYTString(LsRoomNo)
                        + "_" + this.MYTString(LsTableNo)
                        + "_" + this.MYTString(LsCustomer_Id)
                        + "_" + DateTime.Now.ToLongTimeString().Replace(":", "-") + ".txt");



                    if (System.IO.File.Exists(LsFilePath))
                    {
                        if (new FileInfo(LsFilePath).IsReadOnly)
                        {
                            System.IO.File.SetAttributes(LsFilePath, FileAttributes.Normal);
                        }
                    }

                    System.IO.File.WriteAllText(LsFilePath, LsSQL);

                    //using (System.IO.StreamWriter file = new System.IO.StreamWriter(LsFilePath, true))
                    //{
                    //    file.WriteLine(LsSQL);
                    //}
                    //if (System.IO.File.Exists(LsFilePath))
                    //    System.IO.File.Delete(LsFilePath);

                    //FileStream fs = new FileStream(LsFilePath, FileMode.Create, FileAccess.Write);
                    //StreamWriter sw = new StreamWriter(fs);
                    //sw.BaseStream.Seek(0, SeekOrigin.End);
                    //sw.Write(LsSQL);
                    //sw.Flush();
                    //sw.Close();

                    this.WriteLog(this.GetType().Name, LsFilePath, LsSP, LsSQL);//log entry

                }
            }
            catch (Exception exe)
            {
                throw exe;
            }
            return true;
        }


        /* ********************************************************************
       Method Name        :MYTString
       Written by	      :Saroj Pradhan
       Written on         :22/04/2008	
       Modified by	      :
       Modified on	      :
       Input parameters   :object variable
       Output parameters  : 	
       Returns            :string
       Description        :convert to string and trim
   ***********************************************************************  */
        public string MYTString(object LobjValue)
        {
            return MYTString(LobjValue, true);
        }
        public string MYTString(object LobjValue, bool LbTrim)
        {
            string LsReturnValue = null;
            try
            {
                if (!(LobjValue == DBNull.Value || LobjValue == null))
                {
                    LsReturnValue = Convert.ToString(LobjValue);
                    if (LbTrim)
                    {
                        LsReturnValue = LsReturnValue.Trim();
                    }
                }
                else
                {
                    LsReturnValue = "";
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            return LsReturnValue;
        }

        /* ********************************************************************
           Method Name        :MYTDBString
           Written by	      :Saroj Pradhan
           Written on         :16/01/2008	
           Modified by	      :
           Modified on	      :
           Input parameters   :object variable
           Output parameters  : 	
           Returns            :string
           Description        :convert to string and replace single quote to double single quote
       ***********************************************************************  */
        public string MYTDBString(object LobjValue)
        {
            string LsReturnValue = null;
            LsReturnValue = MYTString(LobjValue);
            LsReturnValue = LsReturnValue.Replace("'", "''");

            return LsReturnValue;
        }
        public char MYTChar(object LobjValue)
        {
            char LcReturnValue = ' ';
            try
            {
                if (!(LobjValue == DBNull.Value || LobjValue == null))
                {
                    if (LobjValue.ToString() != "")
                    {
                        LcReturnValue = Convert.ToChar(LobjValue);
                    }
                }
                else
                {
                    LcReturnValue = ' ';
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            return LcReturnValue;
        }
        #region PFDBNumeric
        /* ********************************************************************
    Method Name        :PFDBNumeric
    Written by	       :Saroj Pradhan
    Written on         :15/10/2011
    Modified by	       :
    Modified on	       :
    Input parameters   :value object
    Output parameters  : 	
    Returns            :double
    Description        :Purpose of this function is change the amount from cultural format to database format.
                Example: France Cultural Format Amount “1 000,50” (One thousand and fifty paisa) convert to “1000.50”
    ***********************************************************************  */
        public string PFDBNumeric(object LobjValue, int LiDecimalDigit)
        {
            string LsReturnValue = "0";
            LsReturnValue = MYTNumeric(LobjValue, LiDecimalDigit);
            LsReturnValue = ConvertToNumberFormat(LsReturnValue, true);
            return LsReturnValue;
        }
        #endregion

        public double MYTDouble(object LobjValue)
        {
            double LdblValue = 0;

            if (!(LobjValue == DBNull.Value || LobjValue == null))
            {
                try
                {
                    LdblValue = Convert.ToDouble(LobjValue);
                }
                catch {; }
            }

            return LdblValue;
        }
        public int MYTNumeric(object LobjValue)
        {
            int LintReturnValue = 0;
            try
            {
                if (!(LobjValue == DBNull.Value || LobjValue == null))
                {
                    if (LobjValue.ToString().Length == 0)
                    {
                        LobjValue = "0";
                    }
                    LintReturnValue = Convert.ToInt32(LobjValue);
                }
            }
            catch
            {
                ;
            }
            finally
            {
                ;
            }
            return LintReturnValue;
        }
        /* ********************************************************************
       Method Name        :MYTNumeric
       Written by	      :Saroj Pradhan
       Written on         :22/04/2008	
       Modified by	      :<Last modified person>
       Modified on	      :<date the method was last modified>
       Input parameters   :
       Output parameters  : 	
       Returns            :
       Description        :convert to string and trim
   ***********************************************************************  */
        public string MYTNumeric(object LobjValue, int LiDecimalDigit)
        {
            string LsReturnValue = "";
            try
            {
                if (!(LobjValue == DBNull.Value || LobjValue == null))
                {
                    if (LobjValue.ToString().Length == 0)
                    {
                        LobjValue = "0";
                    }
                    LsReturnValue = Convert.ToDecimal(LobjValue).ToString("N" + LiDecimalDigit);

                    ////for france culture remove 
                    //if (FobjLogParam.Language.ToString() == "fr-FR")
                    //{
                    //    ;
                    //}
                    //else
                    //{
                    //    //added by saroj, more than 2 digit, remove the comma seperator
                    //    if (LiDecimalDigit > 2)
                    //    {
                    //        LsReturnValue = LsReturnValue.Replace(",", "");
                    //    }
                    //}

                    //added by saroj, more than 2 digit, remove the comma seperator
                    if (LiDecimalDigit > 2)
                    {
                        LsReturnValue = LsReturnValue.Replace(",", "");
                    }
                }
                else
                {
                    LsReturnValue = "";
                }
            }
            catch (Exception ex)
            {
                LsReturnValue = "";
            }
            finally
            {
            }
            return LsReturnValue;
        }
        public string MYTNumericCheckZero(object LobjValue, int LiDecimalDigit)
        {
            string LsReturnValue = "";
            int LintReturnValue = 0;
            try
            {
                if (!(LobjValue == DBNull.Value || LobjValue == null))
                {
                    if (LobjValue.ToString().Length == 0)
                    {
                        LobjValue = "0";
                    }
                    LintReturnValue = Convert.ToInt32(LobjValue);
                    LsReturnValue = Convert.ToDecimal(LobjValue).ToString("N" + LiDecimalDigit);
                    if (Convert.ToDecimal(LobjValue) == Convert.ToDecimal(LintReturnValue))
                    {
                        LsReturnValue = Convert.ToDecimal(LintReturnValue).ToString();
                    }

                    
                }
                else
                {
                    LsReturnValue = "";
                }
            }
            catch
            {
                LsReturnValue = "";
            }
            finally
            {
                ;
            }
            return LsReturnValue;
        }

        #region ConvertToNumberFormat
        /* ********************************************************************
    Method Name        :ConvertToNumberFormat
    Written by	       :Saroj Pradhan
    Written on         :21/10/2011
    Modified by	       :
    Modified on	       :
    Input parameters   :value string, replease also decimal seperator
    Output parameters  : 	
    Returns            :string
    Description        :
    ***********************************************************************  */
        public string ConvertToNumberFormat(string LsReturnValue, bool LbWithDecimalSeperator)
        {
            
                LsReturnValue = LsReturnValue.Replace(",", ""); //replace amount seperator
            

            return LsReturnValue;
        }
        #endregion

        /* ********************************************************************
         * Method Name        :PFDateFormat
           Written by	      :Saroj Pradhan
           Written on         :22/04/2008	
           Modified by	      :<Last modified person>
           Modified on	      :<date the method was last modified>
           Input parameters   :
           Output parameters  : 	
           Returns            :
           Description        :convert datetime as per dateformat
           ***********************************************************************  */
        public string PFDateFormat(object LobjValue, string FsDateFormat)
        {
            return PFDateFormat(LobjValue, FsDateFormat, null, false);
        }
        public string PFDateFormat(object LobjValue, string FsDateFormat, string LsTimeFormat)
        {
            return PFDateFormat(LobjValue, FsDateFormat, LsTimeFormat, false);
        }
        public string PFDateFormat(object LobjValue, string FsDateFormat, string LsTimeFormat, bool LbFullMonthName)
        {
            string LsTemp;
            int LiDate, LiMonth, LiYear;
            DateTime LdtValue;

            try
            {
                LsTemp = "";
                FsDateFormat = FsDateFormat.ToLower().ToString();

                if ((LobjValue != DBNull.Value || LobjValue != null) && Convert.ToString(LobjValue).Trim() != "")
                {
                    string[] LarrayMonthName = null;
                    if (LbFullMonthName)
                    {
                        LarrayMonthName = "January,February,March,April,May,June,July,August,September,October,November,December".Split(',');
                    }
                    else
                    {
                        LarrayMonthName = "Jan,Feb,Mar,Apr,May,Jun,Jul,Aug,Sep,Oct,Nov,Dec".Split(',');
                    }
                    //{ "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                    LdtValue = Convert.ToDateTime(LobjValue);
                    LiDate = LdtValue.Day;
                    LiMonth = LdtValue.Month;
                    LiYear = LdtValue.Year;

                    LsTemp = FsDateFormat;
                    LsTemp = LsTemp.Replace("yyyy", Convert.ToString(LiYear));
                    LsTemp = LsTemp.Replace("yy", Convert.ToString(LiYear).Substring(2, 2)); // added by saroj dtd:23/10/2013 for "yy" format

                    LsTemp = LsTemp.Replace("dd", "<e>"); ;
                    LsTemp = LsTemp.Replace("d", "<d>");
                    LsTemp = LsTemp.Replace("<e>", padZero(LiDate));
                    LsTemp = LsTemp.Replace("<d>", Convert.ToString(LiDate));
                    LsTemp = LsTemp.Replace("mmm", "<o>");
                    LsTemp = LsTemp.Replace("mm", "<n>");
                    LsTemp = LsTemp.Replace("m", "<m>");
                    LsTemp = LsTemp.Replace("<m>", Convert.ToString(LiMonth));
                    LsTemp = LsTemp.Replace("<n>", padZero(LiMonth));
                    LsTemp = LsTemp.Replace("<o>", LarrayMonthName[LiMonth - 1]);
                    LarrayMonthName = null;

                    if (LsTimeFormat != null)
                    {
                        if (LsTimeFormat == "HH:MM")
                        {
                            string[] LsTime = Convert.ToString(LdtValue).Trim().Split(' ');
                            if (LsTime.Length > 1)
                            {
                                LsTime = LsTime[1].Split(':');
                                if (LsTime.Length > 1)
                                {
                                    LsTemp = LsTemp + " " + LsTime[0] + ":" + LsTime[1];
                                }
                            }
                        }
                    }
                }
                return LsTemp;
            }
            catch
            {
                return "";
            }
        }

        private string padZero(int LiNum)
        {
            return (LiNum < 10) ? '0' + Convert.ToString(LiNum) : Convert.ToString(LiNum);
        }
        /* ********************************************************************
           Method Name        :PFDBDateFormat
           Written by	      :Saroj Pradhan
           Written on         :01/05/2008	
           Modified by	      :<Last modified person>
           Modified on	      :<date the method was last modified>
           Input parameters   :
           Output parameters  : 	
           Returns            :
           Description        :convert datetime as per dateformat
           ***********************************************************************  */
        // convert Dateformat 
        public string PFDBDateFormat(object LobjDateTimeValue)
        {
            string LsReturnValue = "";
            try
            {
                DateTime dtDate = Convert.ToDateTime(LobjDateTimeValue);
                //string[] LarrayMonthName = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                //LsReturnValue = dtDate.Day.ToString() + LarrayMonthName[dtDate.Month - 1] + dtDate.Year.ToString();
                //LarrayMonthName = null;

                // for MySQL date format
                LsReturnValue = dtDate.ToString("yyyy-MM-dd");
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            return LsReturnValue;
        }
        public DateTime PFDateTime(object LobjDateTimeValue)
        {
            try
            {
                DateTime dtDate = Convert.ToDateTime(LobjDateTimeValue);

                return dtDate;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        private void AddXmlNode(string LsUserID, char LcUserType, string LsOutletID, string LsChainID, string LsFilePath, DateTime LdtLoginDateTime, DateTime LdtBusinessDate)
        {
            XmlDocument xmlDoc = new XmlDocument();

            try
            {
                if (!File.Exists(LsFilePath))
                {
                    //if file is not found, create a new xml file
                    XmlTextWriter xmlWriter = new XmlTextWriter(LsFilePath, System.Text.Encoding.UTF8);
                    xmlWriter.Formatting = Formatting.Indented;
                    xmlWriter.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
                    xmlWriter.WriteStartElement("LOGINTRACK");
                    xmlWriter.Close();
                    xmlDoc.Load(LsFilePath);
                }

                string LsIPAddress = GetIPAddress();

                XmlNode root = xmlDoc.DocumentElement;
                XmlElement childNode = xmlDoc.CreateElement("LOGIN");
                childNode.SetAttribute("LOGINID", LsUserID);

                XmlElement childNode_userid = xmlDoc.CreateElement("userid");
                XmlElement childNode_usertype = xmlDoc.CreateElement("usertype");
                XmlElement childNode_OutletID = xmlDoc.CreateElement("OutletID");
                XmlElement childNode_chainid = xmlDoc.CreateElement("chainid");
                XmlElement childNode_Flag = xmlDoc.CreateElement("Flag");
                XmlElement childNode_LoginDateTime = xmlDoc.CreateElement("LoginDateTime");
                XmlElement childNode_LastAccessIdleDateTime = xmlDoc.CreateElement("LastAccessIdleDateTime");
                XmlElement childNode_BusinessDate = xmlDoc.CreateElement("BusinessDate"); // added by saroj dtd:01/02/2014                
                XmlElement childNode_ComputerName = xmlDoc.CreateElement("ComputerName");
                XmlElement childNode_IPAddress = xmlDoc.CreateElement("IPAddress");

                XmlText textNode_userid = xmlDoc.CreateTextNode("userid");
                textNode_userid.Value = LsUserID;

                XmlText textNode_usertype = xmlDoc.CreateTextNode("usertype");
                textNode_usertype.Value = LcUserType.ToString();

                XmlText textNode_OutletID = xmlDoc.CreateTextNode("OutletID");
                textNode_OutletID.Value = LsOutletID;

                XmlText textNode_chainid = xmlDoc.CreateTextNode("chainid");
                textNode_chainid.Value = LsChainID;

                XmlText textNode_Flag = xmlDoc.CreateTextNode("Flag");
                textNode_Flag.Value = "Y";

                XmlText textNode_LoginDateTime = xmlDoc.CreateTextNode("LoginDateTime");
                textNode_LoginDateTime.Value = LdtLoginDateTime.ToLongDateString() + " " + LdtLoginDateTime.ToLongTimeString();

                // added by saroj dtd: 01/02/2017
                XmlText textNode_LastAccessIdleDateTime = xmlDoc.CreateTextNode("LastAccessIdleDateTime");
                textNode_LastAccessIdleDateTime.Value = LdtLoginDateTime.ToLongDateString() + " " + LdtLoginDateTime.ToLongTimeString();

                XmlText textNode_BusinessDate = xmlDoc.CreateTextNode("BusinessDate");
                textNode_BusinessDate.Value = LdtBusinessDate.ToLongDateString() + " " + LdtBusinessDate.ToLongTimeString();


                XmlText textNode_ComputerName = xmlDoc.CreateTextNode("ComputerName");
                textNode_ComputerName.Value = System.Net.Dns.GetHostName() + "- Mobile";

                XmlText textNode_IPAddress = xmlDoc.CreateTextNode("IPAddress");
                textNode_IPAddress.Value = LsIPAddress;

                root.AppendChild(childNode);

                childNode.AppendChild(childNode_userid);
                childNode.AppendChild(childNode_usertype);
                childNode.AppendChild(childNode_OutletID);
                childNode.AppendChild(childNode_chainid);
                childNode.AppendChild(childNode_Flag);
                childNode.AppendChild(childNode_LoginDateTime);
                childNode.AppendChild(childNode_LastAccessIdleDateTime);// added by saroj dtd: 01/02/2017
                childNode.AppendChild(childNode_BusinessDate);
                childNode.AppendChild(childNode_ComputerName);
                childNode.AppendChild(childNode_IPAddress);

                childNode_userid.AppendChild(textNode_userid);
                childNode_usertype.AppendChild(textNode_usertype);
                childNode_OutletID.AppendChild(textNode_OutletID);
                childNode_chainid.AppendChild(textNode_chainid);
                childNode_Flag.AppendChild(textNode_Flag);
                childNode_LoginDateTime.AppendChild(textNode_LoginDateTime);
                childNode_LastAccessIdleDateTime.AppendChild(textNode_LastAccessIdleDateTime); // added by saroj dtd: 01/02/2017
                childNode_BusinessDate.AppendChild(textNode_BusinessDate);
                childNode_ComputerName.AppendChild(textNode_ComputerName);
                childNode_IPAddress.AppendChild(textNode_IPAddress);

                xmlDoc.Save(LsFilePath);

                root = null;
                childNode = null;
                childNode_userid = null;
                childNode_usertype = null;
                childNode_OutletID = null;
                childNode_chainid = null;
                childNode_Flag = null;
                childNode_LoginDateTime = null;
                childNode_LastAccessIdleDateTime = null;
                childNode_BusinessDate = null;
                childNode_ComputerName = null;
                childNode_IPAddress = null;

                textNode_userid = null;
                textNode_usertype = null;
                textNode_OutletID = null;
                textNode_chainid = null;
                textNode_Flag = null;
                textNode_LoginDateTime = null;
                textNode_LastAccessIdleDateTime = null;
                textNode_BusinessDate = null;
                textNode_ComputerName = null;
                textNode_IPAddress = null;
            }
            catch (System.Exception ex)
            {
                string LsErr = ex.ToString();
            }
            finally
            {
                xmlDoc = null;
            }
        }

        public void RemoveLogInTrack(string LsUserID, char LcUserType, string LsFolderPath, string LsOutletID, string LsChainID, DateTime LdtLoginDateTime)
        {
            try
            {
                //log entry
                WriteLog(this.GetType().Name, "RemoveLogInTrack()", "", "", LsUserID, LsOutletID, LsChainID, "", "", "MOBILE");

                
                System.IO.DirectoryInfo Ldinfo = new DirectoryInfo(LsFolderPath);
                if (!Ldinfo.Exists)
                {
                    Ldinfo.Create();
                }

                string LsFile = LsUserID + "_" + LcUserType + "_" + LsOutletID + "_" + LsChainID + "_" + LdtLoginDateTime.ToString("yyyyMMddHHmmss") + ".XML";
                if (File.Exists(Path.Combine( LsFolderPath , LsFile)))
                {
                    System.IO.File.Delete(Path.Combine(LsFolderPath, LsFile));
                }
                else
                {
                    FileInfo[] LarrayfileInfo = LarrayfileInfo = Ldinfo.GetFiles(LsUserID + "_" + LcUserType + "_" + LsOutletID + "_" + LsChainID + "_*.XML", SearchOption.TopDirectoryOnly);
                    if (LarrayfileInfo.Length > 0)
                    {
                        LarrayfileInfo[0].Delete();
                    }
                    LarrayfileInfo = null;
                }
                LsFolderPath = null; Ldinfo = null;
            }
            catch {; }
        }

        /********************************************************************
        Method Name        :GetApprovalTemplate
        Written by	       :Saroj
        Written on         :20/03/2013
        Modified by	       :
        Modified on	       :
        Input parameters   :OutletID, chainid, applicationpath, templateid(ApprovalRequest, Rejection, OnHold, "")
        Output parameters  : 	
        Returns            :LsTemplate = "" then all templete will be return
        Description        :if LsTemplate = ApprovalRequest then "Acknowledge" tempalte will be return
        ************************************************************************/
        public string GetApprovalTemplate(string LsOutletID, string LsChainID, string LsApplicationPath, string LsTemplate)
        {
            string Lsemail_body = null;
            string Lsemail_body_temp = null;
            string LsTableName = null;
            string LsFilePath = null;
            System.Text.StringBuilder LsbEmailXML = null;
            DataSet LdsReturn = null;

            try
            {
                if (LsTemplate == null)
                {
                    LsTemplate = "";
                }

                LsApplicationPath = Path.Combine( LsApplicationPath.ToString(),"Common");

                int LiLength = 2000;
                int LiTemplate = 0;

                LsbEmailXML = new System.Text.StringBuilder();
                LsbEmailXML.Append("<Table>");

                while (LiTemplate < 5)
                {
                    switch (LiTemplate)
                    {
                        case 0: LsTableName = "ApprovalRequest"; break;
                        case 1: LsTableName = "Rejection"; break;
                        case 2: LsTableName = "OnHold"; break;
                        case 3: LsTableName = "UnApproval"; break;
                        case 4: LsTableName = "Acknowledge"; break;
                    }

                    LiTemplate++;

                    // mobile apps rejeciton and unapprove is not  required now, later can allow
                    if (LsTableName == "UnApproval")
                    {
                        continue;
                    }

                    // required tempalet will be generate
                    if (LsTemplate != "")
                    {
                        if (LsTemplate != LsTableName && LsTableName != "Acknowledge")
                        {
                            continue;
                        }
                    }
                    LdsReturn = new DataSet();

                    // email details
                    //property wise format setting
                    LsFilePath = Path.Combine( LsApplicationPath, LsOutletID + "_" + LsTableName + "EmailFormat.xml");
                    if (File.Exists(LsFilePath))
                    {
                        LdsReturn.ReadXml(LsFilePath);
                    }
                    else
                    {
                        // chian wise format
                        LsFilePath = Path.Combine(LsApplicationPath, LsChainID + "_" + LsTableName + "EmailFormat.xml");
                        if (File.Exists(LsFilePath))
                        {
                            LdsReturn.ReadXml(LsFilePath);
                        }
                        else
                        {
                            //default format
                            LsFilePath = Path.Combine(LsApplicationPath, LsTableName + "EmailFormat.xml");
                            if (File.Exists(LsFilePath))
                            {
                                LdsReturn.ReadXml(LsFilePath);
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }

                    if (!LdsReturn.Tables.Contains(LsTableName)) { continue; }
                    if (LdsReturn.Tables[LsTableName].Rows.Count == 0) { continue; }

                    Lsemail_body = this.MYTString(LdsReturn.Tables[LsTableName].Rows[0]["MAILBODY"]);

                    for (int LiCnt = 1; LiCnt < 50000; LiCnt++)
                    {
                        LsbEmailXML.Append("<"); LsbEmailXML.Append(LsTableName); LsbEmailXML.Append(">");// start approval tempalte tag
                        if (LiCnt == 1)
                        {
                            LsbEmailXML.Append(this.SetXMLAttributes("email_subject", this.MYTString(LdsReturn.Tables[LsTableName].Rows[0]["MAILSUBJECT"]), true)); //email_subject
                        }

                        if (Lsemail_body.Length <= LiLength)
                        {
                            Lsemail_body_temp = Lsemail_body;
                            Lsemail_body = "";
                        }
                        else
                        {
                            Lsemail_body_temp = Lsemail_body.Substring(0, LiLength);
                            Lsemail_body = Lsemail_body.Substring(LiLength, Lsemail_body.Length - LiLength);

                            if (Lsemail_body.IndexOf('[') > Lsemail_body.IndexOf(']'))
                            {
                                Lsemail_body = Lsemail_body_temp.Substring(Lsemail_body_temp.LastIndexOf('['), Lsemail_body_temp.Length - Lsemail_body_temp.LastIndexOf('['))
                                                + Lsemail_body;
                                Lsemail_body_temp = Lsemail_body_temp.Substring(0, Lsemail_body_temp.LastIndexOf('['));
                            }
                        }

                        LsbEmailXML.Append(this.SetXMLAttributes("email_body", Lsemail_body_temp, true));//email_body
                        LsbEmailXML.Append(this.SetXMLAttributes("rowid", LiCnt.ToString()));
                        LsbEmailXML.Append("</"); LsbEmailXML.Append(LsTableName); LsbEmailXML.Append(">");// end approval tempalte tag

                        if (Lsemail_body == "")
                        {
                            break;
                        }
                    }
                }// end of email template loop

                LsbEmailXML.Append("</Table>");
            }
            catch (Exception LexcErr)
            {
                throw LexcErr;
            }
            finally
            {
                LsFilePath = null;
                Lsemail_body = null;
                Lsemail_body_temp = null;
                LsTableName = null; LdsReturn = null;
            }
            return LsbEmailXML.ToString();
        }
        public string SetXMLAttributes(string LsColumnName, string LsColumnValue)
        {
            return SetXMLAttributes(LsColumnName, LsColumnValue, false);
        }
        public string SetXMLAttributes(string LsColumnName, string LsColumnValue, bool LbWithCDATA)
        {
            string LsNewLine = LsNewLine = Environment.NewLine;
            if (LbWithCDATA && LsColumnValue.Trim() != "")
                return "<" + LsColumnName + "><![CDATA[" + LsColumnValue.Trim() + "]]></" + LsColumnName + ">" + LsNewLine;
            else
                return "<" + LsColumnName + ">" + LsColumnValue.Trim() + "</" + LsColumnName + ">" + LsNewLine;
        }

        public bool SendMail(string argMailTO, string argMailCC, string argMailFrom, string argSubject, string argMailBody,
            string argSMTPServer, string argSMTPUserName, string argSMTPPassword, int argSMTPPort, string argSMTPEnableSsl,
            string argFileType, string argCreatedDate, object[] LobjFileLists, string LsUserMail)//, bool blnWithAttachment)
        {
            MailMessage objMailMsg = null; MailAddress objMailAddressFrom = null;
            MailAddress objMailAddressTO = null; SmtpClient objSMTPClient = null;

            string[] streMailTo = null;
            string[] streMailCC = null;
            bool blnReturn = false;

            try
            {
                this.WriteLog(this.GetType().Name, "SendMail()"); //log entry

                // added by saroj dtd: 20/12/2018 for get the proeprty/chain specific SMTP details
                string LsSMTPServer = "", LsSMTPEnableSsl = "", LsSMTPUserName = "", LsSMTPPassword = "", LsMAILFROM = "", LsMAILSUBJECT = "", LsMAILURL = "", LsMAILBODY = "";
                int LiSMTPPort = 0;
                GetSMTPSetting(ref LsSMTPServer, ref LiSMTPPort, ref LsSMTPEnableSsl, ref LsSMTPUserName, ref LsSMTPPassword, ref LsMAILFROM, ref LsMAILSUBJECT, ref LsMAILURL, ref LsMAILBODY);

                if (LsSMTPServer == "" && argSMTPServer != "") { LsSMTPServer = argSMTPServer; }
                if (LsSMTPEnableSsl == "" && argSMTPEnableSsl != "") { LsSMTPEnableSsl = argSMTPEnableSsl; }
                if (LsSMTPUserName == "" && argSMTPUserName != "") { LsSMTPUserName = argSMTPUserName; }
                if (LsSMTPPassword == "" && argSMTPPassword != "") { LsSMTPPassword = argSMTPPassword; }
                if (LsMAILFROM == "" && argMailFrom != "") { LsMAILFROM = argMailFrom; }

                if (LiSMTPPort == 0 && argSMTPPort != 0) { LiSMTPPort = argSMTPPort; }

                if (string.IsNullOrEmpty(LsUserMail) == true)
                {
                    LsUserMail = argMailFrom;
                }

                if (LsMAILFROM == LsUserMail)
                {
                    objMailAddressFrom = new MailAddress(LsMAILFROM);
                }
                else
                {
                    objMailAddressFrom = new MailAddress(LsMAILFROM, LsUserMail);
                }


                streMailTo = argMailTO.Split(new char[] { ';' }); // split for multiple mail
                for (int i = 0; i < streMailTo.Length; i++)
                {
                    if (streMailTo[i].ToString().Trim() == "")
                    {
                        continue;
                    }

                    objMailAddressTO = new MailAddress(streMailTo[i].ToString().Trim());
                    objMailMsg = new MailMessage(objMailAddressFrom, objMailAddressTO);

                    if (argMailCC == null)
                    {
                        argMailCC = "";
                    }

                    // set carbon copy
                    if (argMailCC != "")
                    {
                        streMailCC = argMailCC.Split(new char[] { ';' }); // split for multiple mail
                        for (int j = 0; j < streMailCC.Length; j++)
                        {
                            objMailMsg.CC.Add(new MailAddress(streMailCC[j].ToString().Trim()));
                        }
                    }

                    objMailMsg.IsBodyHtml = true;
                    objMailMsg.Subject = argSubject;
                    objMailMsg.Body = argMailBody;

                    //file attachment
                    if (LobjFileLists != null)
                    {
                        for (int LiCnt = 0; LiCnt < LobjFileLists.Length; LiCnt++)
                        {
                            if (LobjFileLists[LiCnt] != null)
                            {
                                if (File.Exists(LobjFileLists[LiCnt].ToString()))
                                {
                                    objMailMsg.Attachments.Add(new Attachment(LobjFileLists[LiCnt].ToString()));
                                }
                            }
                        }
                    }

                    objMailMsg.IsBodyHtml = true;

                    objMailMsg.Priority = MailPriority.Normal;
                    objSMTPClient = new SmtpClient(LsSMTPServer);

                    if (LsSMTPUserName == "")
                    {
                        objSMTPClient.Credentials = CredentialCache.DefaultNetworkCredentials;
                    }
                    else
                    {
                        objSMTPClient.Credentials = new NetworkCredential(LsSMTPUserName, LsSMTPPassword);
                    }

                    objSMTPClient.EnableSsl = false;
                    if (LsSMTPEnableSsl == "Y")
                    {
                        objSMTPClient.EnableSsl = true;
                    }

                    objSMTPClient.Port = LiSMTPPort;
                    objSMTPClient.Send(objMailMsg);
                }

                blnReturn = true;
            }
            catch (Exception LexcErr)
            {
                blnReturn = false;
                this.WriteLog(this.GetType().Name, "SendMail()", LexcErr.Source.ToString(), LexcErr.Message.ToString()); //log entry
                //throw LexcErr;
            }
            finally
            {
                objMailMsg.Dispose(); objMailMsg = null; objMailAddressFrom = null;
                objMailAddressTO = null; objSMTPClient = null; streMailTo = null;
            }
            return blnReturn;
        }

        private bool GetSMTPSetting(ref string LsSMTPServer, ref int LiSMTPPort, ref string LsSMTPEnableSsl,
                ref string LsSMTPUserName, ref string LsSMTPPassword, ref string LsMAILFROM,
                ref string LsMAILSUBJECT, ref string LsMAILURL, ref string LsMAILBODY)
        {
            // get the property specific
            string LsOutletID = "";
            string LsChainID = "";
            string LsProductID = "";
            string LsModuleID = "";

            if (LsProductID == null) { LsProductID = ""; }
            if (LsModuleID == null) { LsModuleID = ""; }

            string LsDeploymentXMLPath = "";
            string LsFileName = "SMTP.xml";

            // property file
            if (File.Exists(Path.Combine( LsDeploymentXMLPath, LsOutletID + "_" + LsFileName)))
            {
                LsFileName = LsOutletID + "_" + LsFileName;
            }
            // chain file
            else if (File.Exists(Path.Combine(LsDeploymentXMLPath, LsChainID + "_" + LsFileName)))
            {
                LsFileName = LsChainID + "_" + LsFileName;
            }

            if (File.Exists(Path.Combine(LsDeploymentXMLPath, LsFileName)))
            {
                DataSet LdsSMTP = new DataSet();
                LdsSMTP.ReadXml(Path.Combine(LsDeploymentXMLPath, LsFileName));

                DataRow[] LdrFilter = LdsSMTP.Tables["SMTP"].Select("ChainID='" + LsChainID
                        + "' and OutletID='" + LsOutletID
                        + "' and ProductID='" + LsProductID
                        + "' and ModuleID='" + LsModuleID + "'");

                // except module
                if (LdrFilter.Length == 0)
                {
                    LdrFilter = LdsSMTP.Tables["SMTP"].Select("ChainID='" + LsChainID
                        + "' and OutletID='" + LsOutletID
                        + "' and ProductID='" + LsProductID + "'");
                }

                // except module and product
                if (LdrFilter.Length == 0)
                {
                    LdrFilter = LdsSMTP.Tables["SMTP"].Select("ChainID='" + LsChainID
                        + "' and OutletID='" + LsOutletID + "'");
                }

                // except module , product and property
                if (LdrFilter.Length == 0)
                {
                    LdrFilter = LdsSMTP.Tables["SMTP"].Select("ChainID='" + LsChainID + "'");
                }

                if (LdrFilter.Length > 0)
                {
                    LsSMTPServer = LdrFilter[0]["SMTPServer"].ToString().Trim();
                    LiSMTPPort = Convert.ToInt32(LdrFilter[0]["SMTPPort"].ToString());
                    LsSMTPEnableSsl = LdrFilter[0]["SMTPEnableSsl"].ToString().Trim();
                    LsSMTPUserName = LdrFilter[0]["SMTPUserName"].ToString().Trim();
                    LsSMTPPassword = LdrFilter[0]["SMTPPassword"].ToString().Trim();
                    LsMAILFROM = LdrFilter[0]["MAILFROM"].ToString().Trim();

                    if (LdsSMTP.Tables["SMTP"].Columns.Contains("MAILSUBJECT"))
                    {
                        LsMAILSUBJECT = LdrFilter[0]["MAILSUBJECT"].ToString().Trim();
                    }
                    if (LdsSMTP.Tables["SMTP"].Columns.Contains("MAILURL"))
                    {
                        LsMAILURL = LdrFilter[0]["MAILURL"].ToString().Trim();
                    }
                    if (LdsSMTP.Tables["SMTP"].Columns.Contains("MAILBODY"))
                    {
                        LsMAILBODY = LdrFilter[0]["MAILBODY"].ToString().Trim();
                    }
                }
                LdsSMTP = null; LdrFilter = null;
            }

            return true;
        }

        public string GetIPAddress()
        {
            string LsIPAddress = "";

            try
            {

                LsIPAddress = _httpContext.HttpContext.Response.HttpContext.Connection.RemoteIpAddress.ToString();

            }
            catch { }

            return LsIPAddress;
        }

    }
}
