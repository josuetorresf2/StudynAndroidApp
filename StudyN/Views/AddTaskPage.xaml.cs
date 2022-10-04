namespace StudyN.Views;

using Microsoft.Maui.Animations;
using StudyN.Models;
using StudyN.ViewModels;

public partial class AddTaskPage : ContentPage
{
    bool EditButtonsVisible;
	public AddTaskPage()
	{
		InitializeComponent();
        if (UIGlobal.ToEdit != null)
        {
            LoadValues();
            BindingContext = new EditTaskViewModel();
            EditButtonsVisible = true;
        }
        else
        {
            EditButtonsVisible=false; 
            SetValues();
        }
        DeleteTaskButton.IsVisible = EditButtonsVisible;
        CompleteTaskButton.IsVisible = EditButtonsVisible;
	}

    //calls delete task
    private async void HandleDeleteTaskClicked(object sender, EventArgs args)
    {
        UIGlobal.ToEdit.Parent.DeleteTask(UIGlobal.ToEdit.TaskId);
        await Shell.Current.GoToAsync("..");

    }

    private async void HandleCompleteTaskClicked(object sender, EventArgs args)
    {
        UIGlobal.ToEdit.Parent.CompleteTask(UIGlobal.ToEdit.TaskId);
        await Shell.Current.GoToAsync("..");
    }

    void HandleSliderValueChanged(object sender, ValueChangedEventArgs args)
    {
        double value = args.NewValue;
        displayLabel.Text = String.Format("Priority");
    }

    private async void HandleAddTaskButton(object sender, EventArgs e)
    {
        UIGlobal.MainData.AddTask(this.name.Text, this.description.Text, this.date.Date.Value.AddMilliseconds(this.time.Time.Value.TimeOfDay.TotalMilliseconds), (int)this.priority.Value, (int)this.tSpent.Value, (int)this.tComplete.Value);

        await Shell.Current.GoToAsync("..");
    }
    void LoadValues()
    {
        this.name.Text = UIGlobal.ToEdit.Name;
        this.description.Text = UIGlobal.ToEdit.Description;
        this.date.Date = (UIGlobal.ToEdit.DueTime.Date);
        this.time.Time = UIGlobal.ToEdit.DueTime;
        this.priority.Value = (UIGlobal.ToEdit.Priority);
        this.tComplete.Value = UIGlobal.ToEdit.TotalTimeNeeded;
        this.tSpent.Value = UIGlobal.ToEdit.CompletionProgress;
    }

    void SetValues()
    {
        this.date.Date = DateTime.Now;
        this.time.Time = DateTime.Now;
    }
}