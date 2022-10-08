﻿using StudyN.Utilities;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace StudyN.Models
{
    public class TaskItem
    {
        public TaskItem(string name,
                        string description,
                        DateTime dueTime,
                        int priority,
                        int completionProgress,
                        int totalTimeNeeded)
        {
            this.Name = name;
            this.Description = description;
            this.DueTime = dueTime;
            this.Priority = priority;
            this.CompletionProgress = completionProgress;
            this.TotalTimeNeeded = totalTimeNeeded;
        }

        public bool Completed { get; set; } = false;
        public Guid TaskId { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; } = "";
        public DateTime DueTime { get; set; }
        public int CompletionProgress { get; set; } = 0;
        public int TotalTimeNeeded { get; set; } = 0;
        public int Priority { get; set; } = 3;
        public TaskDataManager Parent { get; set; } = null;
    }

    public class TaskDataManager
    {
        public TaskItem AddTask(string name,
                                string description,
                                DateTime dueTime,
                                int priority,
                                int CompletionProgress,
                                int TotalTimeNeeded,
                                bool updateFile = true)
        {
            TaskItem newTask  = new TaskItem(name,
                                            description,
                                            dueTime,
                                            priority,
                                            CompletionProgress,
                                            TotalTimeNeeded);

            TaskList.Add(newTask);

            sendFileUpdate(FileManager.Operation.AddTask, newTask.TaskId, updateFile);

            return newTask;
        }

        public void CompleteTask(Guid taskId, bool updateFile = true)
        {
            foreach (TaskItem task in TaskList)
            {
                if (task.TaskId == taskId)
                {
                    task.Completed = true;
                    CompletedTasks.Add(task);
                    TaskList.Remove(task);

                    sendFileUpdate(FileManager.Operation.CompleteTask, taskId, updateFile);

                    return;
                }
            }
        }

        public void DeleteTask(Guid taskId, bool updateFile = true)
        {
            foreach (TaskItem task in TaskList)
            {
                if (task.TaskId == taskId)
                {
                    TaskList.Remove(task);

                    sendFileUpdate(FileManager.Operation.AddTask, taskId, updateFile);

                    return;
                }
            }
        }

        public void DeleteListOfTasks(List<Guid> taskIds, bool updateFile = true)
        {
            foreach (Guid id in taskIds)
            {
                DeleteTask(id, false);
            }

            sendFileUpdate(FileManager.Operation.DeleteTask, taskIds, updateFile);
        }


        public void sendFileUpdate(FileManager.Operation op, Guid taskId, bool updateFile)
        {
            List<Guid> taskIdList = new List<Guid>();
            taskIdList.Add(taskId);

            sendFileUpdate(op, taskIdList, updateFile);
        }
        public void sendFileUpdate(FileManager.Operation op, List<Guid> taskIds, bool updateFile)
        {
            if (updateFile)
            {
                // Send update to Filemanager
                FileManager.FILE_OP_QUEUE.Enquue(
                    new FileManager.FileOperation(op, taskIds));
            }
        }

        public ObservableCollection<TaskItem> TaskList { get; private set; }
        private ObservableCollection<TaskItem> CompletedTasks { get; set; }

        public TaskDataManager()
        {
            TaskList = new ObservableCollection<TaskItem>();
            CompletedTasks = new ObservableCollection<TaskItem>();
            //GlobalTaskData.TaskManager = this;
            AddTask("test", "", DateTime.Now, 3, 0, 10);
            AddTask("test2", "", DateTime.Now, 3, 0, 10);
            AddTask("test3", "", DateTime.Now, 3, 0, 10);
            AddTask("test4", "", DateTime.Now, 3, 0, 10);
            AddTask("test5", "", DateTime.Now, 3, 0, 10);
            AddTask("test6", "", DateTime.Now, 3, 0, 10);
            AddTask("test7", "", DateTime.Now, 3, 0, 10);
        }
    }

    public static class GlobalTaskData
    {
        public static TaskDataManager TaskManager { get; set; }
        public static TaskItem ToEdit { get; set; }
    }
}


