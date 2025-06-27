using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PROG_POE_Final.Models;

namespace PROG_POE_Final
{
    //Manages list of tasks that including adding, deleting, completing and displaying the list of tasks
    public class TaskManager
    {
        //List that stores tasks
        private List<TaskItem> tasks = new List<TaskItem>();

        //------------------------------------------------------------------------------------------------------------------------
        //Adds a new task to the task list
        public void AddTask(string title, string description = "", DateTime? reminder = null)
        {
            tasks.Add(new TaskItem
            {
                Title = title,
                Description = description,
                Reminder = reminder,
                IsCompleted = false
            });
        }
        //------------------------------------------------------------------------------------------------------------------------
        //Marks a task as complete
        public void MarkTaskCompleted(int index)
        {
            if (index >= 0 && index < tasks.Count)
            {
                tasks[index].IsCompleted = true;
            }
        }
        //------------------------------------------------------------------------------------------------------------------------
        //Deletes task from task list based on the given index
        public void DeleteTask(int index)
        {
            if (index >= 0 && index < tasks.Count)
            {
                tasks.RemoveAt(index);
            }
               
        }
        //------------------------------------------------------------------------------------------------------------------------ 
        //Returns the list of tasks, showing the title, status, description and reminder date if it was given
        public string DisplayTasks()
        {
            if (!tasks.Any())
            {
                return "You currently have no tasks.";
            }
               

            var sb = new StringBuilder();
            sb.AppendLine("Here's a summary of recent actions:\n");
            //Loop through all tasks and append their details to the string builder
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                sb.AppendLine($"{i + 1}. {(task.IsCompleted ? "✔" : "❌")} {task.Title}");
                if (!string.IsNullOrWhiteSpace(task.Description))
                {
                    sb.AppendLine($"   Description: {task.Description}");
                }
                   
                if (task.Reminder.HasValue)
                {
                    sb.AppendLine($"   Reminder set for: {task.Reminder.Value:g}");
                }
            }
                    
            return sb.ToString();
        }
    }
}
//---------------------------------------------------------------------END OF FILE----------------------------------------------------------