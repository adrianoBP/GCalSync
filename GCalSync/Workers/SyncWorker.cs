using GCalSync.Helpers;
using Google.Apis.Calendar.v3.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GCalSync.Workers
{
    public class SyncWorker
    {
        public void StartSync()
        {
            try
            {
                LoggerHelper.AddLog("Sync started", LoggerHelper.Severity.DEBUG);

                CalendarAPIHelper.ValidateAuthToken();

                CalendarAPIHelper fromCalendarAPI = new(true);
                var fromCalendarListItems = fromCalendarAPI.GetCalendarList().Items
                    .Where(x => ApplicationSettings.FromAccountIdsSync.Contains(x.Id)).ToList();
                List<Event> fromEvents = fromCalendarAPI.GetEventsFromCalendars(fromCalendarListItems, ApplicationSettings.MAX_NUMBER_OF_EVENTS);

                CalendarAPIHelper toCalendarAPI = new(false);
                var toCalendarListItems = toCalendarAPI.GetCalendarList().Items
                    .Where(x => x.Id == ApplicationSettings.ToAccountIdSync).ToList();
                List<Event> toEvents = toCalendarAPI.GetEventsFromCalendars(toCalendarListItems,
                    ApplicationSettings.MAX_NUMBER_OF_EVENTS * 3);   // Make sure all available avents are returned

                var lastEventFrom = fromEvents.OrderBy(e => e.Start.DateTime).Last();
                var lastEventTo = toEvents.OrderBy(e => e.Start.DateTime).Last();

                if (lastEventFrom.Start.DateTime < lastEventTo.Start.DateTime)
                    toEvents = toEvents.Where(e => e.Start.DateTime <= lastEventFrom.Start.DateTime).ToList();
                else
                    fromEvents = fromEvents.Where(e => e.Start.DateTime <= lastEventTo.Start.DateTime).ToList();

                var (eventsToAdd, eventsToDelete, eventsToUpdate) = GetEventActions(fromEvents, toEvents);

                AddEvents(eventsToAdd, toCalendarAPI);
                UpdateEvents(eventsToUpdate, toCalendarAPI);
                DeleteEvents(eventsToDelete, toCalendarAPI);

                LoggerHelper.AddLog("Sync completed", LoggerHelper.Severity.DEBUG);
            }
            catch (Exception ex)
            {
                LoggerHelper.AddLog(ex.Message, LoggerHelper.Severity.ERROR, ex);
                APIHelper.MakeRequest<object>("https://watzonservices.ddns.net:18200/setLedsColour", RestSharp.Method.POST, new
                {
                    red = 255,
                    green = 0,
                    blue = 0
                });
            }
        }

        private static (List<Event> eventsToAdd, List<Event> eventsToDelete, List<Event> eventsToUpdate) GetEventActions(List<Event> fromEvents, List<Event> toEvents)
        {
            List<Event> eventsToAdd = new();
            List<Event> eventsToDelete = new();
            List<Event> eventsToUpdate = new();


            Dictionary<string, Event> fromEventReferenceToEvent = fromEvents.ToDictionary(e => BuildEventReference(e), e => e);
            Dictionary<string, Event> toEventReferenceToEvent =
                toEvents.ToDictionary(e => BuildEventReference(e), e => e);

            // Get sync events
            foreach (var fromEvent in fromEvents)
            {
                string fromEventReference = BuildEventReference(fromEvent);

                if (!toEventReferenceToEvent.ContainsKey(fromEventReference))
                {
                    // If there are no attendees (custom) OR
                    // All the attendees matching the account did not decline
                    if (fromEvent.Attendees == null || fromEvent.Attendees.Count == 0 ||
                        fromEvent.Attendees.Where(att => ApplicationSettings.FromAccountIdsSync.Contains(att.Email))
                            .All(att => att.ResponseStatus != "declined"))
                        eventsToAdd.Add(fromEvent);
                }
                else
                {
                    // Check if Start and End dates are the same
                    var toEvent = toEventReferenceToEvent[fromEventReference];
                    if (fromEvent.Start.DateTime != toEvent.Start.DateTime || fromEvent.End.DateTime != toEvent.End.DateTime)
                    {
                        toEvent.Start = fromEvent.Start;
                        toEvent.End = fromEvent.End;
                        eventsToUpdate.Add(toEvent);
                    }
                }
            }

            // If an event is deleted from the "from" account, make sure to delete it in the "to" account
            foreach (var toEvent in toEvents)
            {
                string toEventReference = BuildEventReference(toEvent);
                if (toEvent.Summary.StartsWith(ApplicationSettings.Prefix) && // Only delete relevant items
                    !fromEventReferenceToEvent.ContainsKey(toEventReference))
                    eventsToDelete.Add(toEvent);
            }

            return (eventsToAdd.Distinct().ToList(),
                eventsToDelete.Distinct().ToList(),
                eventsToUpdate.Distinct().ToList());
        }

        private static string BuildEventReference(Event @event)
        {
            return $"{@event.Summary.Replace(ApplicationSettings.Prefix, "")}{@event.Start.DateTime:HHmmddMMyy}";
        }

        private static void DeleteEvents(List<Event> events, CalendarAPIHelper calendarAPIHelper)
        {
            foreach (var @event in events)
            {
                calendarAPIHelper.DeleteEvent(@event, ApplicationSettings.ToAccountIdSync);
            }
        }

        private static void UpdateEvents(List<Event> events, CalendarAPIHelper calendarAPIHelper)
        {
            foreach (var @event in events)
            {
                calendarAPIHelper.UpdateEvent(@event, ApplicationSettings.ToAccountIdSync);
            }
        }

        private static void AddEvents(List<Event> events, CalendarAPIHelper calendarAPIHelper)
        {
            foreach (var @event in events)
            {
                calendarAPIHelper.AddEvent(@event, ApplicationSettings.ToAccountIdSync);
            }
        }
    }
}
