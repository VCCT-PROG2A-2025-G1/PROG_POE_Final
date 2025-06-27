using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PROG_POE_Final
{
    public class ActivityLog
    {
        private List<string> logs = new List<string>();

        //------------------------------------------------------------------------------------------------------------------------
        //Adds a time stamp to when the user completed a log 
        public void AddLog(string entry)
        {
            logs.Add($"[{DateTime.Now:HH:mm}] {entry}");
            if (logs.Count > 10)
            {
                logs.RemoveAt(0);
            }
        }
        //------------------------------------------------------------------------------------------------------------------------
       // Displays all user logs if there are any 
        public string GetLogSummary()
        {
            if (!logs.Any())
            {
                return "Activity log is empty.";
            }
               
            return string.Join("\n", logs);
        }
        //------------------------------------------------------------------------------------------------------------------------
        //Checks if the last log entry in the activity log contains a specified keyword.
        //useful for determining the last action the user performed. 
        public bool LastLogContains(string keyword)
        {
            if (!logs.Any())
            {
                return false;
            }
            return logs.Last().ToLower().Contains(keyword.ToLower());
        }
        //------------------------------------------------------------------------------------------------------------------------
        //Clears all the logs in the activity log of the user
        public void ClearLogs()
        {
            logs.Clear();
        }
    }
}
//-------------------------------------------------------------------------------------------END OF FILE--------------------------------------------------------------------------