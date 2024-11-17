using VPlan_API_Adapter.Server;

namespace VPlan_API_Adapter.Client
{
    public class VPlan
    {
    }

    public class Class
    {
        public Class(Server.Class @class)
        {
            Name = @class.Name;
            Periods = @class.PeriodTimes.ToDictionary(p => p.Id, p => new TimeOnly[] {p.Start, p.End});
            Lessons = @class.Lessons.Select(l => new Lesson(l)).ToList();
        }

        public string Name { get; set; }
        public Dictionary<int, TimeOnly[]> Periods { get; set; }
        public List<Lesson> Lessons { get; set; }
    }

    public class Lesson
    {
        public Lesson(Server.Lesson lesson)
        {
            ClassName = lesson.ClassName;
            Period = lesson.Period?.Id;
            Time = lesson.Period != null ? [lesson.Period.Start, lesson.Period.End] : [];

            Teacher = lesson.Subject.TeacherSH;
            Subject = lesson.Subject.SubjectSH;
            Course = lesson.Subject.CourseSH;

            DefaultTeacher = lesson.DefaultSubject?.TeacherSH;
            DefaultSubject = lesson.DefaultSubject?.SubjectSH;
            DefaultCourse = lesson.DefaultSubject?.CourseSH;

            Room = lesson.Room;
            Info = lesson.Info;

            Changes = new()
            {
                Teacher = lesson.Changes.HasFlag(Server.Changes.Teacher),
                Subject = lesson.Changes.HasFlag(Server.Changes.Subject),
                Room = lesson.Changes.HasFlag(Server.Changes.Room),
                Canceled = lesson.Changes.HasFlag(Server.Changes.Cancelled)
            };
        }

        public string ClassName { get; set; }
        public int? Period { get; set; }
        public TimeOnly[] Time { get; set; }

        public string Teacher { get; set; }
        public string? DefaultTeacher { get; set; }

        public string Subject { get; set; }
        public string? DefaultSubject { get; set; }

        public string? Course { get; set; }
        public string? DefaultCourse { get; set; }

        public string Room { get; set; }
        public string? Info { get; set; }

        public Changes Changes { get; set; }
    }

    public struct Changes
    {
        public bool Teacher { get; set; }
        public bool Subject { get; set; }
        public bool Room { get; set; }
        public bool Canceled { get; set; }

    }
}
