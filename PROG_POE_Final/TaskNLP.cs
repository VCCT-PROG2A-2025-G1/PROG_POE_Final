using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace PROG_POE_Final.Utilities
{
    // Utility class that handles the Natural Processing Language (NLP) for parsing task commands from user input
    // It extracts the task titles and reminders if given
    public static class TaskNLP
    {
        //---------------------------------------------------------------------------------------------------------------------------------------------
        //Parses user input to extract the task title and a reminder date,if given
        public static (string title, DateTime? reminder) ParseTask(string input)
        {
            input = input.ToLower(); //Convert the input to lowercase for processing
            DateTime? reminder =  null;// Initialize the reminder as null

            //Detects the date
            if (input.Contains("tomorrow"))
            {
                reminder = DateTime.Now.AddDays(1);
                input = input.Replace("tomorrow", "") ;
            }
            else if (input.Contains("today"))
            {
                reminder = DateTime.Now;
                input = input.Replace("today", "");
            }
            else
            {
                var inDaysMatch = Regex.Match(input, @"in (\d+) days");
                if (inDaysMatch.Success)
                {
                    int days = int.Parse(inDaysMatch.Groups[1].Value);
                    reminder = DateTime.Now.AddDays(days);
                    input = input.Replace(inDaysMatch.Value, "");
                }
            }

            //add specific phrases that will be used to trigger task creation
            string[] specifiedPhrases = {
                 "add task to", "add task", "create task",
                "remind me to", "set a reminder to"
            };

            //---------------------------------------------------------------------------------------------------------------------------------------------
            // Loop iterates till the phrase in the specifiedPhrases array matches the user's input then extracts the task and assigns it to string title
            foreach (var phrase in specifiedPhrases)
            {
                if (input.StartsWith(phrase))
                {
                    input = input.Replace(phrase, "");
                    break;
                }
            }

            string title = input.Trim();
            return (title, reminder);
        }
    }
}
//---------------------------------------------------------------------END OF FILE----------------------------------------------------------