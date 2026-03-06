using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace WilPick.Common
{
    public class Logger
    {
        private static string _strLastError;
        private static string _userName;

        public static string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }

        public static string LastError
        {
            get { return _strLastError; }
            set { _strLastError = value; }
        }

        private static string _SuccessPath;
        public static string SuccessPath
        {
            get { return _SuccessPath; }
            set { _SuccessPath = value; }
        }

        private static string _ErrorPath;
        public static string ErrorPath
        {
            get { return _ErrorPath; }
            set { _ErrorPath = value; }
        }

        private static bool _IsMonthly = true;
        public static bool IsMonthly
        {
            get { return _IsMonthly; }
            set { _IsMonthly = value; }
        }

        // Constructor
        public Logger(string username)
        {           
            IsMonthly = true;
            UserName = username;
        }

        // Initialize logger paths from IConfiguration (reads Logging:LogPath)
        public static void Initialize(IConfiguration configuration)
        {
            if (configuration == null)
                return;

            var logPath = configuration["Logging:LogPath"];
            if (string.IsNullOrWhiteSpace(logPath))
                return;

            // Set separate folders for success and error logs under the configured path
            try
            {
                var basePath = logPath.TrimEnd('\\', '/');
                SuccessPath = Path.Combine(basePath, "Success");
                ErrorPath = Path.Combine(basePath, "Error");
            }
            catch
            {
                // Fallback to raw path if combine fails
                SuccessPath = logPath;
                ErrorPath = logPath;
            }
        }

        public static bool Logs(string Path, string FileNamePrefix, string Module, string Function, string Message)
        {
            FileStream fStream = null;
            StreamWriter Writer = null;
            try
            {
                LastError = string.Empty;
                if (Path.Equals(string.Empty))
                    throw new Exception("Path is empty or null.");

                if (IsMonthly)
                    Path = Path.TrimEnd('\\') + "\\" + DateTime.Today.ToString("yyyyMM");

                if (!Directory.Exists(Path))
                {
                    try
                    {
                        Directory.CreateDirectory(Path);
                    }
                    catch
                    {
                        throw new Exception(Path + " Directory doesn't exist.");
                    }
                }

                Path = Path.TrimEnd('\\');
                string FileName = Path + "\\" + FileNamePrefix + DateTime.Today.ToString("yyyyMMdd") + ".log";

                if (!File.Exists(FileName))
                    File.Create(FileName).Close();

                fStream = new FileStream(FileName, FileMode.Append, FileAccess.Write);
                Writer = new StreamWriter(fStream);
                var loggedUserName = "";// TODO string.IsNullOrEmpty(HttpContext.Current.User.Identity.Name) ? UserName : HttpContext.Current.User.Identity.Name;

                Writer.WriteLine("[" + DateTime.Now.ToString() + "] - " +
                             Module + " : " + "fn(" + Function + ") " +
                             Message + " BY USER: " + loggedUserName);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return false;
            }
            finally
            {
                Writer.Close();
                fStream.Close();
                Writer.Dispose();
                fStream.Dispose();
            }
            return true;
        }

        public static bool Error(string Module, string Function, string Message)
        {
            try
            {
                return Logs(ErrorPath, "ERR", Module, Function, Message);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return false;
            }
        }

        public static bool Status(string Module, string Function, string Message)
        {
            try
            {
                return Logs(SuccessPath, "LOG", Module, Function, Message);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return false;
            }
        }
    }
}
