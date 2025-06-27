using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PROG_POE_Final.Models
{
    public class TaskItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? Reminder { get; set; }
        public bool IsCompleted { get; set; }
    }
}
