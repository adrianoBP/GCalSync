using GCalSync.Helpers;
using Google.Apis.Calendar.v3.Data;
using Newtonsoft.Json;
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

                CalendarAPIHelper fromCalendarAPI = new(true);
                var fromCalendarListItems = fromCalendarAPI.GetCalendarList().Items
                    .Where(x => ApplicationSettings.FromAccountIdsSync.Contains(x.Id)).ToList();
                List<Event> fromEvents = fromCalendarAPI.GetEventsFromCalendars(fromCalendarListItems);

                CalendarAPIHelper toCalendarAPI = new(false);
                var toCalendarListItems = toCalendarAPI.GetCalendarList().Items
                    .Where(x => x.Id == ApplicationSettings.ToAccountIdSync).ToList();
                List<Event> toEvents = toCalendarAPI.GetEventsFromCalendars(toCalendarListItems);

                var (eventsToAdd, eventsToDelete, eventsToUpdate) = GetEventActions(fromEvents, toEvents);

                AddEvents(eventsToAdd, toCalendarAPI);
                UpdateEvents(eventsToUpdate, toCalendarAPI);
                DeleteEvents(eventsToDelete, toCalendarAPI);

                LoggerHelper.AddLog("Sync completed", LoggerHelper.Severity.DEBUG);
            }
            catch (Exception ex)
            {
                LoggerHelper.AddLog(ex.Message, LoggerHelper.Severity.ERROR);
                APIHelper.MakeRequest<object>("https://watzonservices.ddns.net:18200/setLedsColour", RestSharp.Method.POST, new
                {
                    red = 255,
                    green = 0,
                    blue = 0
                });
            }
        }

        private (List<Event> eventsToAdd, List<Event> eventsToDelete, List<Event> eventsToUpdate) GetEventActions(List<Event> fromEvents, List<Event> toEvents)
        {
            List<Event> eventsToAdd = new();
            List<Event> eventsToDelete = new();
            List<Event> eventsToUpdate = new();


            Dictionary<string, Event> toEventIdToEvent =
                toEvents.ToDictionary(e => e.Id, e => e);
            Dictionary<string, Event> fromEventIdToEvent = fromEvents.ToDictionary(e => e.Id, e => e);

            // Get sync events
            foreach (var fromEvent in fromEvents)
            {
                if (!toEventIdToEvent.ContainsKey(fromEvent.Id))
                    eventsToAdd.Add(fromEvent);
                else
                {
                    // Check if Start and End dates are the same
                    var toEvent = toEventIdToEvent[fromEvent.Id];
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
                if (toEvent.Summary.StartsWith(ApplicationSettings.LeadingText) && // Only delete relevant items
                    !fromEventIdToEvent.ContainsKey(toEvent.Id))
                    eventsToDelete.Add(toEvent);
            }

            return (eventsToAdd.Distinct().ToList(),
                eventsToDelete.Distinct().ToList(),
                eventsToUpdate.Distinct().ToList());
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
