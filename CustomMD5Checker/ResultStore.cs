#pragma warning disable IDE0044

using System.Collections.Generic;

namespace CustomMD5Checker
{
    class ResultStore
    {
        // Invalid file path, file is missing or inaccessible
        private List<string> NA = new List<string>();
        // Current MD5 hash of file has not been calculated
        private List<string> Unknown = new List<string>();
        // Current MD5 hash of file is different than saved MD5 hash
        private List<string> Failed = new List<string>();
        // Current MD5 hash of file is equal to saved MD5 hash
        private List<string> Passed = new List<string>();
        // Misc Notes
        private List<string> Notes = new List<string>();

        public void AddNA(string entry) { NA.Add(entry); }
        public void AddUnknown(string entry) { Unknown.Add(entry); }
        public void AddFailed(string entry) { Failed.Add(entry); }
        public void AddPassed(string entry) { Passed.Add(entry); }
        public void AddNote(string entry) { Notes.Add(entry); }

        public string NAToString()
        {
            string ret = "";
            for(int i = 0; i < NA.Count; i++)
                ret += NA[i] + "\n";
            return ret;
        }
        public string UnknownToString()
        {
            string ret = "";
            for (int i = 0; i < Unknown.Count; i++)
                ret += Unknown[i] + "\n";
            return ret;
        }
        public string FailedToString()
        {
            string ret = "";
            for (int i = 0; i < Failed.Count; i++)
                ret += Failed[i] + "\n";
            return ret;
        }
        public string PassedToString()
        {
            string ret = "";
            for (int i = 0; i < Passed.Count; i++)
                ret += Passed[i] + "\n";
            return ret;
        }
        public string NotesToString()
        {
            string ret = "";
            for (int i = 0; i < Notes.Count; i++)
                ret += Notes[i] + "\n";
            return ret;
        }

        public string[] ToStringAll()
        {
            string[] ret = new string[5];
            ret[0] = NAToString();
            ret[1] = UnknownToString();
            ret[2] = FailedToString();
            ret[3] = PassedToString();
            ret[4] = NotesToString();
            return ret;
        }
        public List<string>[] GetAll()
        {
            List<string>[] ret = new List<string>[5];
            ret[0] = NA;
            ret[1] = Unknown;
            ret[2] = Failed;
            ret[3] = Passed;
            ret[4] = Notes;
            return ret;
        }
    }
}
