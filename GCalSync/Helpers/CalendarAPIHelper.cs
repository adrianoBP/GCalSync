using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace GCalSync.Helpers
{
    public class CalendarAPIHelper
    {
        private class AccountServiceDetails
        {
            public CalendarService CalendarService { get; set; }
            public UserCredential UserCredential { get; set; }

            public void UpdateCalendarService()
            {
                CalendarService = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = UserCredential,
                    ApplicationName = ApplicationSettings.ApplicationName
                });
            }

            public void RefreshToken()
            {
                UserCredential.RefreshTokenAsync(CancellationToken.None);
                UpdateCalendarService();
            }
        }

        private static AccountServiceDetails _fromAccountService { get; set; } = new AccountServiceDetails();
        private static AccountServiceDetails _toAccountService { get; set; } = new AccountServiceDetails();


        private static DateTime _lastAuthDate = DateTime.MinValue;

        private AccountServiceDetails _currentAccountDetails = null;

        public CalendarAPIHelper(bool isFrom)
        {
            if (isFrom)
                _currentAccountDetails = _fromAccountService;
            else
                _currentAccountDetails = _toAccountService;
        }

        public static void Init()
        {
            InitializeCredentials(true);
            InitializeCredentials(false);
        }

        private static void InitializeCredentials(bool isFromAccount)
        {
            using var credentialsStream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);

            AccountServiceDetails accountServiceDetails = _fromAccountService;

            if (!isFromAccount)
                accountServiceDetails = _toAccountService;


            string credPath = $"{(isFromAccount ? "from" : "to")}_acc_token";
            accountServiceDetails.UserCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(credentialsStream).Secrets,
                ApplicationSettings.CalendarScopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true)).Result;

            accountServiceDetails.UpdateCalendarService();
        }

        public static void ValidateAuthToken()
        {
            // For now refresh token every 24 hours - If it fails, then we need to add
            // a check for when the token expires and refresh the token at that time
            if ((DateTime.UtcNow - _lastAuthDate).TotalHours > 24)
            {
                _fromAccountService.RefreshToken();
                _toAccountService.RefreshToken();
            }
        }

        public void AddEvent(Event @event, string calendarId)
        {
            // All-day events cannot have a DateTime object, just a Date instead
            if (
                ((DateTime)@event.Start.DateTime).ToString("HH:mm:ss") == "00:00:00" &&
                ((DateTime)@event.End.DateTime).ToString("HH:mm:ss") == "00:00:00")
            {
                @event.Start.DateTime = null;
                @event.End.DateTime = null;
            }

            _toAccountService.CalendarService.Events.Insert(new Event()
            {
                Start = @event.Start,
                End = @event.End,
                Summary = $"{ApplicationSettings.Prefix}{@event.Summary}",
                EventType = "outOfOffice",
                Reminders = new()
                {
                    UseDefault = false
                },
                Description = @event.Description,
                ColorId = "3"
            }, calendarId).Execute();
        }

        public void DeleteEvent(Event @event, string calendarId)
        {
            _currentAccountDetails.CalendarService.Events.Delete(calendarId, @event.Id).Execute();
        }

        public void UpdateEvent(Event @event, string calendarId)
        {
            _currentAccountDetails.CalendarService.Events.Update(@event, calendarId, @event.Id).Execute();
        }

        public CalendarList GetCalendarList()
        {
            return _currentAccountDetails.CalendarService.CalendarList.List().Execute();
        }

        public Events GetEvents(string eventList, int numberOfEvents)
        {
            EventsResource.ListRequest request = _currentAccountDetails.CalendarService.Events.List(eventList);
            request.TimeMin = DateTime.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = numberOfEvents;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            return request.Execute();
        }

        public List<Event> GetEventsFromCalendars(List<CalendarListEntry> calendarListItems, int numberOfEvents)
        {
            List<Event> events = new();
            foreach (CalendarListEntry calendarListItem in calendarListItems)
            {
                var calendarItems = GetEvents(calendarListItem.Id, numberOfEvents).Items;
                foreach (Event item in calendarItems)
                {
                    if (item.Start.DateTime == null)
                    {
                        item.Start.DateTime = DateTime.ParseExact(item.Start.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    if (item.End.DateTime == null)
                    {
                        item.End.DateTime = DateTime.ParseExact(item.End.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    events.Add(item);
                }
            }
            return events.OrderBy(e => e.Start.DateTime).ToList();
        }
    }
}