using System;

namespace monitor_queues
{
    public class QueueInfo
    {
        public string Name { get; set; }
        public int FirstCount { get; set; }
        public int CurrentCount { get; set; }
        public DateTime StartTime { get; set; } = DateTime.Now;

        private string _lastRemainingTime = "";

        public int ProcessedMessages => FirstCount - CurrentCount;
        TimeSpan _t;

        public string RemainingTime 
        {
            get
            {
                if(FirstCount == 0)
                {
                    _lastRemainingTime = "";
                }
                else
                {
                    if(ProcessedMessages > 0)
                    {
                        _t = (((DateTime.Now - StartTime) / ProcessedMessages) * CurrentCount);
                        _lastRemainingTime = _t.Ticks == 0 ? "" : $"{(int)_t.TotalHours:0}:{_t.Minutes:00}:{_t.Seconds:00}";
                    }
                } 

                return _lastRemainingTime;
            }
        }

        public string EstimatedConclusion => _t.Ticks == 0 ? "" : DateTime.Now.Add(_t).ToString("dd/MM HH:mm");

        private string _lastSpeed = "";

        public string Speed
        {
            get
            {
                if (FirstCount == 0)
                {
                    _lastSpeed = "";
                }
                else
                {
                    var totalSeconds = (int)(DateTime.Now - StartTime).TotalSeconds;
                    if (totalSeconds > 0)
                    {
                        var s = ProcessedMessages / totalSeconds;
                        _lastSpeed = s == 0 ? "" : $"{s}/s";
                    }
                }

                return _lastSpeed;
            }
        }

        public bool IncreasedCount { get; set; }
        public bool DecreasedCount { get; set; }
        public bool IsImportant { get; set; }

        public string GroupName { get; set; }
        public int GroupOrder { get; set; }
    }
}
