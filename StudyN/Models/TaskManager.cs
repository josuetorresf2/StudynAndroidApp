﻿using Newtonsoft.Json;
﻿using Android.Gms.Tasks;
using Android.Service.Autofill;
using StudyN.Utilities;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using DevExpress.CodeParser;
using DevExpress.XtraRichEdit.Model;
using System;

namespace StudyN.Models
{
    //This class manages all of our tasks and preforms actions related to them
    public class TaskDataManager
    {
        //This function will add a new task to our list of tasks
        public TaskItem AddTask(string name,
                               string description,
                               DateTime dueTime,
                               int priority,
                               int category,
                               double timeWorked,
                               double timeEstimated,
                               Guid recurId = new Guid())
        {
            if(recurId == Guid.Empty)
            {
                recurId = Guid.NewGuid();
            }

            //Creating new task with sent parameters
            TaskItem newTask = new TaskItem(name,
                                            description,
                                            dueTime,
                                            category,
                                            priority,
                                            timeWorked,
                                            timeEstimated,
                                            DateTime.Now,
                                            recurId);

            //This will add the tasks to the list
            TaskList.Add(newTask);

            // Publish task add event
            EventBus.PublishEvent(
                        new StudynEvent(newTask.TaskId, StudynEvent.StudynEventType.AddTask));

            return newTask;
        }

        //This will return a requested task using its id
        public TaskItem GetTask(Guid taskId)
        {
            //Checking each item in the current task list
            foreach (TaskItem task in TaskList)
            {
                //If the task is found, return the task
                if (task.TaskId == taskId)
                {
                    return task;
                }
            }

            //Checking the completed tasks list
            foreach (TaskItem task in CompletedTasks)
            {
                if(task.TaskId == taskId)
                {
                    return task;
                }
            }

            //If not found in either list, return null
            return null;
        }

        //This function will recieve data to update a task with
        public bool EditTask(Guid taskId,
                                string name,
                                string description,
                                DateTime dueTime,
                                int priority,
                                int category,
                                double timeWorked,
                                double timeEstimated,
                                List<TaskItemTime> TimeList = null,
                                bool updateFile = true)
        {
            //Retrieving the task
            TaskItem task = GetTask(taskId);

            //If the task is not found, return false to end early
            if(task == null)
            {
                return false;
            }

            task.Name = name;
            task.Description = description;
            task.DueTime = dueTime;
            task.Priority = priority;
            task.Category = category;
            task.TimeWorked = timeWorked;
            task.TimeEstimated = timeEstimated;
            task.TimeList = TimeList;

            // Publish task edit event
            EventBus.PublishEvent(
                        new StudynEvent(taskId, StudynEvent.StudynEventType.EditTask));

            return true;
        }

        //This function will move a given task to the completed task list
        public void CompleteTask(Guid taskId, bool updateFile = true)
        {
            //Searching for the task in the normal TaskList
            foreach (TaskItem task in TaskList)
            {
                //If it is found in the list
                if (task.TaskId == taskId)
                {
                    //Set the tasks to completed, add it to the completed list, and remove
                    //it from the normal one
                    task.Completed = true;
                    CompletedTasks.Add(task);
                    TaskList.Remove(task);

                    // Publish task complete event
                    EventBus.PublishEvent(
                        new StudynEvent(taskId, StudynEvent.StudynEventType.CompleteTask));

                    return;
                }
            }
        }

        //This function will delete a given task from the normal TaskList
        public void DeleteTask(Guid taskId, bool updateFile = true)
        {
            //Looking for the task based on Id
            foreach (TaskItem task in TaskList)
            {
                //If found
                if (task.TaskId == taskId)
                {
                    //Remove the task from list
                    TaskList.Remove(task);

                    // Publish task delete event
                    EventBus.PublishEvent(
                        new StudynEvent(taskId, StudynEvent.StudynEventType.DeleteTask));

                    return;
                }
            }
        }

        public void CreateDailyReccuringTask(TaskItem ParentTask, DateTime EndDate, int numDays = 1)
        {
            ParentTask.IsRecur = true;

            // Create a deepcopy
            DateTime dueTime = new DateTime(ParentTask.DueTime.Ticks).AddDays(numDays);

            while (dueTime.Date <= EndDate.Date)
            {
                TaskItem task = GlobalTaskData.TaskManager.AddTask(
                                        ParentTask.Name,
                                        ParentTask.Description,
                                        dueTime,
                                        ParentTask.Priority,
                                        ParentTask.Category,
                                        ParentTask.TimeWorked,
                                        ParentTask.TimeEstimated,
                                        ParentTask.RecurId);
                task.IsRecur = true;
                // Create a deepcopy
                dueTime = new DateTime(dueTime.Ticks).AddDays(numDays);
            }
        }


        public void CreateWeeklyReccuringTask(TaskItem ParentTask, DateTime EndDate)
        {
            CreateDailyReccuringTask(ParentTask, EndDate, 7);
        }

        public void CreateMonthlyReccuringTask(TaskItem ParentTask, DateTime EndDate)
        {
            ParentTask.IsRecur = true;

            // Create a deepcopy
            DateTime dueTime = new DateTime(ParentTask.DueTime.Ticks).AddMonths(1);

            while (dueTime.Date <= EndDate.Date)
            {
                TaskItem task = GlobalTaskData.TaskManager.AddTask(
                                        ParentTask.Name,
                                        ParentTask.Description,
                                        dueTime,
                                        ParentTask.Priority,
                                        ParentTask.Category,
                                        ParentTask.TimeWorked,
                                        ParentTask.TimeEstimated,
                                        ParentTask.RecurId);
                task.IsRecur = true;
                // Create a deepcopy
                dueTime = new DateTime(dueTime.Ticks).AddMonths(1);
            }
        }

        public void EditRecurring(TaskItem editedTask)
        {
            if(!editedTask.IsRecur) { return; }

            foreach(TaskItem task in TaskList)
            {
                if(task.RecurId == editedTask.RecurId)
                {
                    task.Name = editedTask.Name;
                    task.Description = editedTask.Description;
                    // Change the time of date without changing the date
                    task.DueTime = task.DueTime.Date + editedTask.DueTime.TimeOfDay;
                    task.Priority = editedTask.Priority;

                    // Publish task edit event
                    EventBus.PublishEvent(
                                new StudynEvent(task.TaskId, StudynEvent.StudynEventType.EditTask));
                }
            }
        }

        //This function will delete every task for the ids avalaible
        public void DeleteListOfTasks(List<Guid> taskIds)
        {
            foreach (Guid id in taskIds)
            {
                DeleteTask(id);
            }
        }

        
        public void LoadFilesIntoLists()
        {
            string jsonfiletext;

            // gets completed tasks
            string[] taskfilelist = FileManager.LoadTaskFileNames();
            foreach (string file in taskfilelist)
            {
                try { 
                    jsonfiletext = File.ReadAllText(file);
                    //Console.WriteLine(jsonfiletext);
                    TaskItem task = JsonConvert.DeserializeObject<TaskItem>(jsonfiletext); 
                    //TaskItem task = JsonSerializer.Deserialize<TaskItem>(jsonfiletext)!;
                    TaskList.Add(task);

                    if (task.TimeList != null)
                    {
                        Console.WriteLine("--------------------------------");
                        Console.WriteLine("--------------------------------");
                        Console.WriteLine("Writing out task times");
                        foreach (TaskItemTime tasktime in task.TimeList)
                        {
                            Console.WriteLine("Time Start" + tasktime.start);
                            Console.WriteLine("TimeStop" + tasktime.stop);
                            Console.WriteLine("Timespanned" + tasktime.span);
                        }
                    }
                }catch (Exception ex)
                {
                    // when files get loaded that don't have all information needed
                    Console.WriteLine(ex.Message);
                }
            }



            // gets completed tasks
            string[] completedfiles = FileManager.LoadCompletedFileNames();
            foreach (string file in completedfiles)
            {
                try
                {
                    jsonfiletext = File.ReadAllText(file);
                    TaskItem task = JsonConvert.DeserializeObject<TaskItem>(jsonfiletext);

                    //TaskItem task = JsonSerializer.Deserialize<TaskItem>(jsonfiletext)!;
                    CompletedTasks.Add(task);
                }
                catch (Exception ex)
                {
                    // when files get loaded that don't have all information needed
                    Console.WriteLine(ex.Message);
                }
            }

            //gets test tasks
            string[] testFile = FileManager.LoadTaskFileNames();
            foreach (string file in testFile)
            {
                try
                {
                    jsonfiletext = File.ReadAllText(file);
                    //Console.WriteLine(jsonfiletext);
                    TaskItem task = JsonConvert.DeserializeObject<TaskItem>(jsonfiletext);
                    TaskListTest.Add(task);

                    if (task.TimeList != null)
                    {
                        Console.WriteLine("--------------------------------");
                        Console.WriteLine("--------------------------------");
                        Console.WriteLine("Writing out task times");
                        foreach (TaskItemTime tasktime in task.TimeList)
                        {
                        Console.WriteLine("Time Start" + tasktime.start);
                        Console.WriteLine("TimeStop" + tasktime.stop);
                        Console.WriteLine("Timespanned" + tasktime.span);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // when files get loaded that don't have all information needed
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void LoadFilesIntoListsTest(string dirName)
        {
            string jsonfiletext;

            // gets completed tasks
            string[] taskfilelist = FileManager.LoadTaskFileTest(dirName);
            foreach (string file in taskfilelist)
            {
                try {
                    jsonfiletext = File.ReadAllText(file);
                    //Console.WriteLine(jsonfiletext);
                    TaskItem task = JsonConvert.DeserializeObject<TaskItem>(jsonfiletext);
                    //TaskItem task = JsonSerializer.Deserialize<TaskItem>(jsonfiletext)!;
                    TaskList.Add(task);

                    if (task.TimeList != null)
                    {
                        Console.WriteLine("--------------------------------");
                        Console.WriteLine("--------------------------------");
                        Console.WriteLine("Writing out task times");
                        foreach (TaskItemTime tasktime in task.TimeList)
                        {
                            Console.WriteLine("Time Start" + tasktime.start);
                            Console.WriteLine("TimeStop" + tasktime.stop);
                            Console.WriteLine("Timespanned" + tasktime.span);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // when files get loaded that don't have all information needed
                    Console.WriteLine(ex.Message);
                }
            }

            // gets completed tasks
            string[] completedfiles = FileManager.LoadCompletedFileNames();
            foreach (string file in completedfiles)
            {
                try { 
                    jsonfiletext = File.ReadAllText(file);
                    TaskItem task = JsonConvert.DeserializeObject<TaskItem>(jsonfiletext);

                    //TaskItem task = JsonSerializer.Deserialize<TaskItem>(jsonfiletext)!;
                    CompletedTasks.Add(task);
                }catch (Exception ex)
                {
                    // when files get loaded that don't have all information needed
                    Console.WriteLine(ex.Message);
                }
            }

            //gets test tasks
            string[] testFile = FileManager.LoadTaskFileTest(dirName);
            foreach (string file in testFile)
            {
                try {
                    jsonfiletext = File.ReadAllText(file);
                    //Console.WriteLine(jsonfiletext);
                    TaskItem task = JsonConvert.DeserializeObject<TaskItem>(jsonfiletext);
                    TaskListTest.Add(task);

                    if (task.TimeList != null)
                    {
                        Console.WriteLine("--------------------------------");
                        Console.WriteLine("--------------------------------");
                        Console.WriteLine("Writing out task times");
                        foreach (TaskItemTime tasktime in task.TimeList)
                        {
                            Console.WriteLine("Time Start" + tasktime.start);
                            Console.WriteLine("TimeStop" + tasktime.stop);
                            Console.WriteLine("Timespanned" + tasktime.span);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // when files get loaded that don't have all information needed
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Turns record of hours and minutes and makes them doubles
        /// </summary>
        /// <param name="hours"></param>
        /// <param name="minutes"></param>
        /// <returns></returns>
        public double SumTimes(int hours, int minutes)
        {
            // make sure minutes are below 60
            if(minutes >= 60)
            {
                // take 60 out of minutes and add to hours
                while(minutes >= 60)
                {
                    minutes -= 60;
                    hours++;
                }
            }
            // turn minutes into decimals
            double decimalMins = (double)minutes / 60;
            // return hours.minutes
            return (double)hours + decimalMins;
        }

        // Count the number of total tasks due today
        public int NumTasksDueToday()
        {
            int numTasksDue = NumTasksCompletedToday();
            foreach(TaskItem task in TaskList)
            {
                if (task.DueTime.Date == DateTime.Today)
                {
                    numTasksDue++;
                }
            }
            return numTasksDue;
        }

        // Count the number of task completed that were due today
        public int NumTasksCompletedToday()
        {
            int numCompleted = 0;
            foreach(TaskItem task in CompletedTasks)
            {
                if(task.DueTime.Date == DateTime.Today)
                {
                    numCompleted++;
                }
            }
            return numCompleted;
        }

        public ObservableCollection<TaskItem> TaskList { get; private set; }
        private ObservableCollection<TaskItem> CompletedTasks { get; set; }
        public ObservableCollection<TaskItem> TaskListTest { get; private set; }

        //This constructor will create the normal TaskList and the list for
        //completed tasks, CompletedTasks
        public TaskDataManager()
        {
            TaskList = new ObservableCollection<TaskItem>();
            CompletedTasks = new ObservableCollection<TaskItem>();
            TaskListTest = new ObservableCollection<TaskItem>();
        } 

        //Method checks what task was added to the list last
        public TaskItem GetLastTask()
        {
            //Set to arbitrary task
            TaskItem latestTask = TaskList.Last();

            //Checking each item in the current task list
            foreach (TaskItem task in TaskList)
            {
                /* Check if the time span was just added
                and saves it as the latest Task */
                if (task.DateNow >= latestTask.DateNow)
                {
                    latestTask = task;
                }
            }
            //return latest added task
            return latestTask;
        }

    }
}