﻿using DevExpress.Maui.Scheduler;
using DevExpress.Maui.Scheduler.Internal;
using DevExpress.Web.ASPxScheduler.Forms;
using DevExpress.XamarinAndroid.Scheduler;
using DevExpress.XtraScheduler.Native;
using StudyN.Common;
using StudyN.Models; //Calls Calendar Data
using StudyN.Utilities;
using StudyN.ViewModels;
using System.ComponentModel;
using System.Globalization;
using static StudyN.Utilities.StudynEvent;

namespace StudyN.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]


    public partial class CalendarPage : ContentPage, StudynSubscriber
    {
        // Flag to prevent multiple child pages opening
        bool isChildPageOpening = false;

        readonly CalendarDataView _calendarDataView;
        public CalendarPage()
        {
            InitializeComponent(); 
            ViewModel = new CalendarViewModel();
            BindingContext = _calendarDataView = new CalendarDataView(); //Use to pull data of CalendarData under Models
            dailyButton.BackgroundColor = Color.FromRgba(255, 255, 255, 255);
            EventBus.Subscribe(this);

            

            // Reuse data storage between all the views
            weekView.DataStorage = dayView.DataStorage;
            monthView.DataStorage = dayView.DataStorage;

            // set Calendar properties                        
            dayView.ShowWorkTimeOnly = false; // Visible Time can only be set if this is false
            weekView.ShowWorkTimeOnly = false;

            // dayView.VisibleTime = TimeSpanRange.Day; // this works, but time gets cut off... following is workaround
            // NOTE from estepanek:
            // Even after I moved the tabs to a flyout and made room at the footer area,
            // an hour and a half of the scheduler was still cut off, so the following adds
            // two extra hours in the TimeSpanRange for VisibleTime to compensate for that
            // set to show 12am to 2am the next day, but some will get cut off and will
            // only be able to scroll to approximately 12:30am
            // This is probably due to the application header bar potentially moving the 
            // DevExpress Scheduler down and consequently off the screen
            TimeSpanRange visibleTimeSpanRange = new TimeSpanRange(TimeSpan.FromHours(0), TimeSpan.FromDays(1).Add(TimeSpan.FromHours(2))); 
            dayView.VisibleTime = visibleTimeSpanRange;
            weekView.VisibleTime = visibleTimeSpanRange;
            
            Console.WriteLine("***** Just set dayView.VisibleTime = " + dayView.VisibleTime.ToString());
        }

        CalendarViewModel ViewModel { get; }


        void OnDailyClicked(object sender, EventArgs args)
        {
            dayView.IsVisible = true;
            weekView.IsVisible = false;
            monthView.IsVisible = false;
            dailyButton.BackgroundColor = Color.FromRgba(255, 255, 255, 255);
            weeklyButton.BackgroundColor = Color.FromRgba(255, 255, 255, 0);
            monthlyButton.BackgroundColor = Color.FromRgba(255, 255, 255, 0);
        }

        void OnWeeklyClicked(object sender, EventArgs args)
        {
            dayView.IsVisible = false;
            weekView.IsVisible = true;
            monthView.IsVisible = false;
            dailyButton.BackgroundColor = Color.FromRgba(255, 255, 255, 0);
            weeklyButton.BackgroundColor = Color.FromRgba(255, 255, 255, 255);
            monthlyButton.BackgroundColor = Color.FromRgba(255, 255, 255, 0);

        }

        void OnMonthlyClicked(object sender, EventArgs args)
        {
            dayView.IsVisible = false;
            weekView.IsVisible = false;
            monthView.IsVisible = true;
            dailyButton.BackgroundColor = Color.FromRgba(255, 255, 255, 0);
            weeklyButton.BackgroundColor = Color.FromRgba(255, 255, 255, 0);
            monthlyButton.BackgroundColor = Color.FromRgba(255, 255, 255, 255);
        }

        protected override void OnAppearing()
        {
            Console.WriteLine("CalendarPage OnAppearing");
            SchedulerStorage.RefreshData();
            //SchedulerStorage.AppointmentItems.Refresh(); //https://supportcenter.devexpress.com/ticket/details/q320528/slow-scheduler-refresh //https://supportcenter.devexpress.com/ticket/details/t615692/how-to-programmatically-refresh-scheduler
            InvalidateMeasure();

            isChildPageOpening = false;

            HandelSleepTime();
            var notes = SchedulerStorage.GetAppointments(new DateTimeRange(DateTime.Now, DateTime.Now.AddDays(7)));
            CalendarDataView.LoadDataForNotification(notes.ToList());
            base.OnAppearing();
            Console.WriteLine("*****dayView.VisibleTime=" + dayView.VisibleTime.ToString());            
            Console.WriteLine("*****dayView.ActualVisibleTime=" + dayView.ActualVisibleTime.ToString());
            Console.WriteLine("*****dayView.WorkTime=" + dayView.WorkTime.ToString());
            Console.WriteLine("*****dayView.ShowWorkTimeOnly=" + dayView.ShowWorkTimeOnly.ToString());
        }

        private void ShowAppointmentEditPage(AppointmentItem appointment)
        {
            if (!isChildPageOpening)
            {
                isChildPageOpening = true;
                AppointmentEditPage appEditPage = new(appointment, SchedulerStorage);
                Navigation.PushAsync(appEditPage);
            }
        }

        private void ShowNewAppointmentEditPage(IntervalInfo info)
        {
            if (!isChildPageOpening)
            {
                isChildPageOpening = true;
                AppointmentEditPage appEditPage = new(info.Start, info.End, info.AllDay, SchedulerStorage);
                Navigation.PushAsync(appEditPage);
            }
        }

        private void HandelSleepTime()
        {            
            if(File.Exists(FileSystem.AppDataDirectory + "/sleepTime.json"))
            {
                // Make the work time start at the end and end at the start of sleep time
                TimeSpan startTime = GlobalAppointmentData.CalendarManager.SleepTime.StartTime - 
                    GlobalAppointmentData.CalendarManager.SleepTime.StartTime.Date;
                TimeSpan endTime = GlobalAppointmentData.CalendarManager.SleepTime.EndTime - 
                    GlobalAppointmentData.CalendarManager.SleepTime.EndTime.Date;
                Color workColor = Color.FromArgb("#fffffe");
                Color sleepColor = Color.FromArgb("#f1f1f1");
                dayView.CellStyle = new DayViewCellStyle();
                weekView.CellStyle = new DayViewCellStyle();
                if(startTime > endTime)
                {
                    // make sure the colors are correct
                    dayView.WorkTime = new TimeSpanRange(endTime, startTime);
                    dayView.CellStyle.WorkTimeBackgroundColor = workColor;
                    dayView.CellStyle.BackgroundColor = sleepColor;
                    weekView.WorkTime = new TimeSpanRange(endTime, startTime);
                    weekView.CellStyle.WorkTimeBackgroundColor = workColor;
                    weekView.CellStyle.BackgroundColor = sleepColor;
                }
                else
                {
                    // make work time sleep time
                    dayView.WorkTime = new TimeSpanRange(startTime, endTime);
                    dayView.CellStyle.WorkTimeBackgroundColor = sleepColor;
                    dayView.CellStyle.BackgroundColor = workColor;
                    weekView.WorkTime = new TimeSpanRange(startTime, endTime);
                    weekView.CellStyle.WorkTimeBackgroundColor = sleepColor;
                    weekView.CellStyle.BackgroundColor = workColor;
                }
               
            }
            else
            {
                // Make every minute work time
                dayView.WorkTime = TimeSpanRange.Day;
                weekView.WorkTime = TimeSpanRange.Day;
            }
        }

        private async void OnCalendarTap(object sender, SchedulerGestureEventArgs e)
        {
            if (e.IntervalInfo != null)
            {
                if (e.AppointmentInfo == null)
                {
                    ShowNewAppointmentEditPage(e.IntervalInfo);
                    return;
                }
                
                AppointmentItem appointment = e.AppointmentInfo.Appointment;
                bool answer = await DisplayAlert("Are you sure?", appointment.Subject + " should be edited.", "Yes", "No");
                
                if (answer == true)
                {
                    ShowAppointmentEditPage(appointment);
                }
            }
        }

        private async void OnCalendarHold(object sender, SchedulerGestureEventArgs e)
        {
            if (e.AppointmentInfo != null)
            {
                AppointmentItem appointment = e.AppointmentInfo.Appointment;
                bool answer = await DisplayAlert("Are you sure?",
                    appointment.Subject + " will be deleted.", "Yes", "No");

                if (answer == true)
                {
                    SchedulerStorage.RemoveAppointment(appointment);
                }
            }
        }

        public void OnNewStudynEvent(StudynEvent sEvent)
        {
            Console.WriteLine("in CalendarPage.OnNewStudynEvent");
            // On any appointment event, refresh the data
            if (sEvent.EventType == StudynEventType.AppointmentAdd
                || sEvent.EventType == StudynEventType.AppointmentEdit
                || sEvent.EventType == StudynEventType.AppointmentDelete)
            {
                //SchedulerStorage.RefreshData(); //Not sure if this is crashing the app causing an "index out of range" or "handler being used elsewhere" error. The calendarpage does SchedulerStorage.RefreshData() every time it appears anyway, so im going to comment this out for now
            }
        }

        //View of events 
        public class CalendarDataView : INotifyPropertyChanged
        {
            readonly CalendarManager data;

            public event PropertyChangedEventHandler PropertyChanged;
            public static DateTime StartDate { get { return CalendarManager.BaseDate; } }

            public IReadOnlyList<Appointment> Appointments { get => data.Appointments; }
            public IReadOnlyList<AppointmentCategory> AppointmentCategories { get => data.AppointmentCategories; }
            public IReadOnlyList<AppointmentStatus> AppointmentStatuses { get => data.AppointmentStatuses; }

            public CalendarDataView()
            {
                data = GlobalAppointmentData.CalendarManager;
            }

            /// <summary>
            /// Uses static class DataAccess to load the notification database with AppointmentItems
            /// </summary>
            public void LoadDataForNotification()
            {
                LoadDataForNotification(Appointments);
            }

            /// <summary>
            /// Takes a collection of AppointmentItems and loads the notification database using static class DataAccess
            /// </summary>
            /// <param name="appointments"></param>
            public static void LoadDataForNotification(IReadOnlyList<AppointmentItem> appointments)
            {
                DataAccess.LoadData(appointments);
            }

            protected void RaisePropertyChanged(string name)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                    LoadDataForNotification();
                }
            }
        }

        void onExportButtonTap(object sender, EventArgs args)
        {
            // Gets appointment data
            var appointments = GlobalAppointmentData.CalendarManager.Appointments;

            // Heading of ICS file
            String fileBody = "BEGIN:VCALENDAR\n";
            fileBody += "VERSION:2.0\n";
            fileBody += "PRODID:StudyN\n";


            // index of appointments
            int i = 0;

            // Runs for each appointment on the calendar
            foreach (Appointment appointment in appointments)
            {
                // Increases index
                i += 1;

                DateTime startTime = TimeZoneInfo.ConvertTimeToUtc(appointment.Start);
                DateTime endTime = TimeZoneInfo.ConvertTimeToUtc(appointment.End);

                // Populates event information
                fileBody += "BEGIN:VEVENT\n";
                fileBody += "SUMMARY:" + appointment.Subject + "\n";
                fileBody += "DTSTART:" + String.Format("{0:yyyyMMdd}T{0:HHmmss}Z\n", startTime);
                fileBody += "DTEND:" + String.Format("{0:yyyyMMdd}T{0:HHmmss}Z\n", endTime);
                fileBody += "UID:StudyN-appointment-" + i + "\n";
                fileBody += "END:VEVENT\n";
            }

            // Last line of ICS file
            fileBody += "END:VCALENDAR";

            // Writes all information to ICS file
            createICS(fileBody, "StudyN_ExportedCalendar.ics");
        }

        async void createICS(string text, string targetFileName)
        {
            // Write the file content to the app data directory
            string targetFile = System.IO.Path.Combine(FileSystem.Current.AppDataDirectory, targetFileName);
            using FileStream outputStream = System.IO.File.OpenWrite(targetFile);
            using StreamWriter streamWriter = new StreamWriter(outputStream);
            await streamWriter.WriteAsync(text);
            await DisplayAlert("Done!", "Calendar successfully exported.", "Ok");
        }
        private async void OnSleepButtonTap(object sender, EventArgs e)
        {
            Routing.RegisterRoute(nameof(Views.SleepTimePage), typeof(Views.SleepTimePage));
            await Shell.Current.GoToAsync(nameof(Views.SleepTimePage));
        }
    }
} 