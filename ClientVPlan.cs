using System.Xml.Linq;
using VPlan_API_Adapter.Server;

namespace VPlan_API_Adapter.Client
{
    public class VPlan
    {
    }

    public class Class : IXMLSerializable
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

        public XElement ToXML()
        {
            XElement root = new("Class");
            root.Add(new XElement("Name", Name));

            XElement periods = new("Periods");
            foreach (var kv in Periods)
            {
                XElement p = new("Period", kv.Key);
                p.SetAttributeValue("Start", kv.Value[0]);
                p.SetAttributeValue("End", kv.Value[1]);
                periods.Add(p);
            }
            root.Add(periods);

            XElement lessons = new("Lessons");
            foreach (var l in Lessons)
            {
                lessons.Add(l.ToXML());
            }
            root.Add(lessons);

            return root;
        }
    }

    public class Lesson : IXMLSerializable
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

        public XElement ToXML()
        {
            XElement root = new("Lesson");
            root.Add(new XElement("ClassName", ClassName));

            XElement period = new("Period", Period);
            period.SetAttributeValue("Start", Time[0]);
            period.SetAttributeValue("End", Time[1]);
            root.Add(period);

            XElement teacher = new("Teacher", Teacher);
            teacher.SetAttributeValue("Default", DefaultTeacher);
            root.Add(teacher);

            XElement subject = new("Subject", Subject);
            subject.SetAttributeValue("Default", DefaultSubject);
            root.Add(subject);

            XElement course = new("Course", Course);
            course.SetAttributeValue("Default", DefaultCourse);
            root.Add(course);

            root.Add(new XElement("Room", Room));
            root.Add(new XElement("Info", Info));

            XElement changes = new("Changes");
            if (Changes.Teacher) changes.Add(new XElement("Teacher"));
            if (Changes.Subject) changes.Add(new XElement("Subject"));
            if (Changes.Room) changes.Add(new XElement("Room"));
            if (Changes.Canceled) changes.Add(new XElement("Canceled"));
            root.Add(changes);

            return root;
        }
    }

    public class Teacher : IXMLSerializable
    {
        public Teacher(Server.Teacher teacher)
        {
            ShortHand = teacher.ShortHand;
            Lessons = teacher.Lessons.Select(l => new Lesson(l)).ToList();
        }

        public string ShortHand { get; set; }
        public List<Lesson> Lessons { get; set; }

        public XElement ToXML()
        {
            XElement root = new("Teacher");
            root.Add(new XElement("ShortHand", ShortHand));
            root.Add(XMLSerializeableList<Lesson>.From(Lessons, "Lessons").ToXML());
            return root;
        }
    }

    public class Room : IXMLSerializable
    {
        public Room(Server.Room room)
        {
            ShortHand = room.Name;
            Lessons = room.Lessons.Select(l => new Lesson(l)).ToList();
        }

        public string ShortHand { get; set; }
        public List<Lesson> Lessons { get; set; }

        public XElement ToXML()
        {
            XElement root = new("Teacher");
            root.Add(new XElement("ShortHand", ShortHand));
            root.Add(XMLSerializeableList<Lesson>.From(Lessons, "Lessons").ToXML());
            return root;
        }
    }

    public struct Changes
    {
        public bool Teacher { get; set; }
        public bool Subject { get; set; }
        public bool Room { get; set; }
        public bool Canceled { get; set; }

    }
}
