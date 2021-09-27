using GCalSync.Helpers;

namespace GCalSync.Workers
{
    public class AccountWorker
    {
        public void AddFromAccount()
        {
            _ = new CalendarAPIHelper(true);
        }

        public void AddToAccount()
        {
            _ = new CalendarAPIHelper(false);
        }
    }
}
