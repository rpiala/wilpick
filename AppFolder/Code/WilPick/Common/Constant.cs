using Microsoft.AspNetCore.Identity;
using WilPick.Models;

namespace WilPick.Common
{
    public class Constant
    {
        public const string LOGINTRANSTYPE = "LOGIN";
        public const string LOGOUTTRANSTYPE = "LOGOUT";
        public const decimal BETTICKETPRICE = 5;
        public const decimal WINNINGPRICE = 25000;
        public const string LOADTYPE = "LOAD";
        public const string REMITTYPE = "REMIT";
        public const string UPLOADPATH = "wwwroot/uploads";
        public const string OPENPATH = "wwwroot\\uploads";        
    }

    public static class Roles
    {
        public const string Agent = "Agent";
        public const string Client = "Client";
        public const string Owner = "Owner";
    }

}
