namespace BstServer.Models;

public struct TimeRange
{
    public TimeRange(int time)
    {
        StartTime = time;
        EndTime = time;
    }

    public TimeRange(int startTime, int endTime) : this()
    {
        StartTime = startTime;
        EndTime = endTime;
    }

    public int StartTime { get; set; }
    public int EndTime { get; set; }

    public static implicit operator TimeRange(int time) => new TimeRange(time);

    public override string ToString()
    {
        return StartTime == EndTime ? StartTime.ToString() : $"[{StartTime}, {EndTime}]";
    }
}