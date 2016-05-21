using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ScreenTaker.Models
{
    public enum Languages
    {
        ENG,
        UKR,
        RUS,
        FRN
    }
    public class LocalizationInside
    {
        private static ENGLocalization instance = null;
        public static Languages selectedLanguage = Languages.ENG;
        private LocalizationInside()
        { }
        public static ENGLocalization getInstance()
        {
            if (instance == null)
                instance = new getLocalization();
            return instance;
        }
        private ILocalization getLocalization()
        {
            switch(selectedLanguage)
            {
                case Languages.ENG: return new ENGLocalization();
                case Languages.UKR: return new UKRLocalization();
                case Languages.RUS: return new RUSLocalization();
                case Languages.FRN: return new FRNLocalization();
                default: return new ENGLocalization();
            }
        }
    }
   
    public class ENGLocalization 
    {
        public  string APP_TITLE = "Screen Taker";
        public  string LOG_IN = "Log in";
        public  string LOG_OUT = "Log out";
        public  string LOGIN = "Login";
        public  string PASSWORD = "Password";
        public  string PASSWORD_CONFIRM = "Password confirm";
        public  string SIGN_UP = "Sign up";
        public  string FEATURES = "Features";
        public  string DEVELOPED_BY = "Developed by";
        public  string TEAM = "Dream team";
        public  string UPLOAD = "Upload";
        public  string FORGOT_PASS = "Forgot your password?";
        public  string ALREADY_HAVE = "Already have an account?";
        public  string E_MAIL = "E-mail";
        public  string MANAGE_GROUPS = "Manage groups";
        public  string YOUR_OLD_PASS = "Your old password";
        public  string ENTER_NEW_PASS = "Enter a new password";
        public  string CONFIRM_PASS = "Confirm a new password";
        public  string CHANGE_PASS = "Change password";
        public  string CHANGE_PROFILE_IMG = "Change your profile image";
        public  string CLASSIC = "Classic";
        public  string ALPHABET = "Alphabet";
        public  string DATE = "Date";
        //Context menu
        public  string DELETE = "Delete";
        public  string RENAME = "Rename";
        public  string PRIVATE = "Private";
        public  string MANAGE_ACCESS = "Manage access";
        public  string ACCESS_BY_LINK = "Date";
        public  string PEOPLE = "People";
        public  string GROUPS = "Groups";
        public  string SUBMIT = "Submit";
        public  string SAVED_AS = "Saved as";
        public  string EDIT_GROUPS = "Edit groups";
        public  string SAVE = "Save";
        public  string CANCEL = "Cancel";
        public  string ADD = "Add";
        public  string REMOVE = "Remove";
        public  string EDIT = "Edit";
    }

    public class UKRLocalization: ENGLocalization
    {
    }

    public class RUSLocalization : ENGLocalization
    {
    }

    public class FRNLocalization : ENGLocalization
    {
    }

}