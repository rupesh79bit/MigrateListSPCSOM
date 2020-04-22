using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Security;
using Microsoft.SharePoint.Client;

namespace SharePointAuthentication
{
    public enum SharepointEnvironment
    {
        Sharepointonline,
        Sharepointonpremise
    }
    public class SharepointAuth
    {
        public static ICredentials  CreateCredentials(string userName,string password,SharepointEnvironment spEnvironmentType)
        {
            ICredentials credentials = null;
            switch (spEnvironmentType)
            {
                case SharepointEnvironment.Sharepointonline:
                    SecureString securePassword = new SecureString();
                    foreach (var item in password.ToCharArray())
                    {
                        securePassword.AppendChar(item);
                    }
                    credentials = new SharePointOnlineCredentials(userName, securePassword);
                    break;
                case SharepointEnvironment.Sharepointonpremise:
                    credentials = new NetworkCredential(userName, password);
                    break;

            }
            return credentials;
        }
    }
}
