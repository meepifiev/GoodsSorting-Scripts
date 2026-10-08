using System.Collections.Generic;

namespace _Project.Core.Analytics.Events
{
    public class DesignEvent : IAnalyticsEvent
    {
        private readonly string _name;
        private readonly Dictionary<string, string> _data;

        public DesignEvent(string name)
        {
            _name = name;
            _data = null;
        }

        public DesignEvent(string name, Dictionary<string, string> data)
        {
            _name = name;
            _data = data;
        }

        public void Report(IAnalyticsReporter reporter)
        {
            if (_data == null)
            {
                reporter.ReportDesignEvent(_name);
                return;
            }

            reporter.ReportDesignEvent(_name, _data);
        }
    }
}
