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
        private CalendarService Service { get; set; }

        public CalendarAPIHelper(bool isFrom)
        {
            using var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);
            string credPath = $"{(isFrom ? "from" : "to")}_acc_token";
            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                ApplicationSettings.CalendarScopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true)).Result;

            Service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationSettings.ApplicationName
            });
        }

        public void AddEvent(Event @event, string calendarId)
        {
            TimeZoneInfo cst = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            var offset = cst.GetUtcOffset((DateTime)@event.Start.DateTime);

            Service.Events.Insert(new Event()
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
            Service.Events.Delete(calendarId, @event.Id).Execute();
        }

        public void UpdateEvent(Event @event, string calendarId)
        {
            Service.Events.Update(@event, calendarId, @event.Id).Execute();
        }

        public CalendarList GetCalendarList()
        {
            return Service.CalendarList.List().Execute();
        }

        public Events GetEvents(string eventList, int numberOfEvents)
        {
            EventsResource.ListRequest request = Service.Events.List(eventList);
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