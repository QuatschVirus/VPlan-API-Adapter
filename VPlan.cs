using System.Text;
using System.Xml.Linq;

namespace VPlan_API_Adapter.Server
{
    // TODO: Create better modells for client-side

    public class VPlan
    {
        #region --- Management Stuff ---

        private DateTime lastPulled;
        private readonly DateOnly referenceDate;
        private readonly TimeSpan expiration;

        private readonly string fullBaseUrl;

        private readonly HttpClient client;

        public DateTime LastPulled => lastPulled;

        public bool UpdateDataIfExpired()
        {
            if (DateTime.Now - lastPulled > expiration)
            {
                return UpdateData();
            }
            return true;
        }

        public bool UpdateData()
        {
            var res = client.GetAsync("PlanKl" + referenceDate.ToString("yyyyMMdd") + ".xml").Result;

            if (res.IsSuccessStatusCode)
            {
                XDocument doc = XDocument.Parse(res.Content.ReadAsStringAsync().Result);
                DataUpdate(doc.Root!);
                lastPulled = DateTime.Now;
                return true;
            } else
            {
                return false;
            }
        }

        public VPlan(DateOnly refDate, TimeSpan expiration, string url, string username, string password)
        {
            referenceDate = refDate;

            client = new()
            {
                BaseAddress = new(url)
            };
            client.DefaultRequestHeaders.Authorization = new("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));

            this.expiration = expiration;
        }

        #endregion

        #region --- Plan Data ---

        private void DataUpdate(XElement root)
        {
            var ts = DateTime.Parse(root.Element("Kopf")!.Element("zeitstempel")!.Value);
            if (TimeStamp.GetValueOrDefault() != ts) {
                TimeStamp = ts;
                OffDays = root.Element("FreieTage")!.Elements()!.Select(e => DateOnly.ParseExact(e.Value, "yyMMdd")).ToList();
                Infos = root.Element("ZusatzInfo")?.Elements()?.Select(e => e.Value).ToList() ?? [];

                var classElements = root.Element("Klassen")!.Elements().Select(e => (e.Element("Kurz")!.Value, e));
                foreach (var (name, e) in classElements)
                {
                    if (classes.TryGetValue(name, out var c))
                    {
                        c.UpdateIfContainsChanges(e);
                    } else
                    {
                        Class cs = new(e);
                        cs.DataUpdated += ClassUpdated;
                        classes.Add(name, cs);
                    }
                }
            }
        }

        private void ClassUpdated(Class c)
        {
            foreach (Lesson l in c.Lessons)
            {
                if (teachers.TryGetValue(l.Subject.TeacherSH, out var t)) {
                    t.PushNewLesson(l);
                }
                if (teachers.TryGetValue(l.DefaultSubject?.TeacherSH ?? "", out var td))
                {
                    td.PushNewLesson(l);
                }
                if (rooms.TryGetValue(l.Room, out var r))
                {
                    r.PushNewLesson(l);
                }
            }
        }

        public DateTime? TimeStamp { get; private set; }
        public List<DateOnly>? OffDays { get; private set; }
        public List<string>? Infos { get; private set; }


        public int? DaysPerWeek { get; private set; }

        private Dictionary<string, Class> classes = [];
        private Dictionary<string, Teacher> teachers = [];
        private Dictionary<string, Room> rooms = [];

        public Class? GetClass(string name, bool allowUpdates = false)
        {
            if (!UpdateDataIfExpired()) return null;
            if (classes.TryGetValue(name, out var c)) return c;
            else
            {
                if (allowUpdates)
                {
                    UpdateData();
                    return GetClass(name, false);
                }
            }
            return null;
        }

        public Teacher? GetTeacher(string shorthand, bool allowUpdates = false)
        {
            if (!UpdateDataIfExpired()) return null;
            if (teachers.TryGetValue(shorthand, out var t)) return t;
            else
            {
                var lessons = from c in classes.Values
                              where c.HasTeacher(shorthand)
                              from l in c.Lessons
                              where l.DefaultSubject?.TeacherSH == shorthand || l.Subject.TeacherSH == shorthand
                              select l;
                Teacher tn = new(shorthand, lessons.ToList());
                teachers.Add(shorthand, tn);
                return tn;
            }
        }

        public Room? GetRoom(string name)
        {
            if (!UpdateDataIfExpired()) return null;
            if (rooms.TryGetValue(name, out var r)) return r;
            else
            {
                var lessons = from c in classes.Values
                              from l in c.Lessons
                              where l.Room == name
                              select l;
                Room rn = new(name, lessons.ToList());
                rooms.Add(name, rn);
                return rn;
            }
        }

        #endregion
    }

    public class Class
    {
        public Class(XElement classRoot)
        {
            hash = classRoot.Hash();
            Name = classRoot.Element("Kurz")!.Value;
            PeriodTimes = classRoot.Element("KlStunden")!.Elements().Select(e => new PeriodTime(e)).ToList();
            Subjects = classRoot.Element("Unterricht")!.Elements().Select(e => new SubjectRecord(e.Element("UeNr")!)).ToDictionary(s => s.ID);
            Lessons = classRoot.Element("Pl")!.Elements().Select(e => new Lesson(e, this)).ToList();
        }

        public string Name { get; }
        public List<PeriodTime> PeriodTimes { get; private set; }
        public Dictionary<int, SubjectRecord> Subjects { get; }
        public List<Lesson> Lessons { get; }

        private byte[] hash;

        public event Action<Class>? DataUpdated;

        public List<Lesson> GetLessonsWithTeacher(string shorthand, bool includeSubbedLessons = true, bool includeSubbingLessons = true) {
            List<Lesson> lessons = [];
            foreach (Lesson l in Lessons)
            {
                if (l.Changes.HasFlag(Changes.Teacher))
                {
                    if (includeSubbedLessons && l.DefaultSubject?.TeacherSH == shorthand) lessons.Add(l);
                    if (includeSubbingLessons && l.Subject.TeacherSH == shorthand) lessons.Add(l);
                } else
                {
                    if (l.Subject.TeacherSH == shorthand) lessons.Add(l);
                }
            }
            return lessons;
        }

        public bool HasTeacher(string shorthand, bool includeSubbedLessons = true, bool includeSubbingLessons = true) {
            return GetLessonsWithTeacher(shorthand, includeSubbedLessons, includeSubbingLessons).Count != 0;
        }

        public bool ContainsChanges(XElement newCRoot)
        {
            return newCRoot.Hash() != hash;
        }

        public void UpdateIfContainsChanges(XElement newCRoot)
        {
            if (ContainsChanges(newCRoot))
            {
                var lessonStubs = newCRoot.Element("Pl")!.Elements().ToDictionary(Lesson.GetID);
                foreach (var l in Lessons)
                {
                    l.UpdateIfContainsChanges(lessonStubs[l.ID], this);
                }
                hash = newCRoot.Hash();
                DataUpdated?.Invoke(this);
            }
        }
    }

    public class Teacher
    {
        public Teacher(string shortHand, List<Lesson> lessons)
        {
            ShortHand = shortHand;
            Lessons = lessons;
            foreach (var l in Lessons)
            {
                l.DataChanged += LessonUpdated;
            }
        }

        public string ShortHand { get; }
        public List<Lesson> Lessons { get; }

        private void LessonUpdated(Lesson l)
        {
            if (!(l.Subject.TeacherSH == ShortHand || l.DefaultSubject?.TeacherSH == ShortHand))
            {
                Lessons.Remove(l);
            }
        }

        public void PushNewLesson(Lesson l)
        {
            if (!Lessons.Contains(l)) Lessons.Add(l);
        }

        public List<Lesson> GetSortedLessons()
        {
            return [.. Lessons.OrderBy(l => l.Period)];
        }

    }

    public class Room
    {
        public Room(string name, List<Lesson> lessons)
        {
            Name = name;
            Lessons = lessons;
            foreach (var l in Lessons)
            {
                l.DataChanged += LessonUpdated;
            }
        }

        public string Name { get; }
        public List<Lesson> Lessons { get; }

        private void LessonUpdated(Lesson l)
        {
            if (!(l.Subject.TeacherSH == Name || l.DefaultSubject?.TeacherSH == Name))
            {
                Lessons.Remove(l);
            }
        }

        public void PushNewLesson(Lesson l)
        {
            if (!Lessons.Contains(l)) Lessons.Add(l);
        }

        public List<Lesson> GetSortedLessons()
        {
            return [.. Lessons.OrderBy(l => l.Period)];
        }
    }

    public class PeriodTime(XElement timeRoot)
    {
        public TimeOnly Start { get; } = TimeOnly.ParseExact(timeRoot.Attribute("ZeitVon")!.Value, "HH:mm");
        public TimeOnly End { get; } = TimeOnly.ParseExact(timeRoot.Attribute("ZeitBis")!.Value, "HH:mm");
        public int Id { get; } = int.Parse(timeRoot.Value);
    }

    public class SubjectRecord
    {
        public SubjectRecord(XElement subjectRoot)
        {
            TeacherSH = subjectRoot.Attribute("UeLe")!.Value;
            SubjectSH = subjectRoot.Attribute("UeFa")!.Value;
            CourseSH = subjectRoot.Attribute("UeGr")?.Value;
            ID = int.Parse(subjectRoot.Value);
        }

        public SubjectRecord(string teacherSH, string subjectSH)
        {
            TeacherSH = teacherSH;
            SubjectSH = subjectSH;
            ID = -1;
        }

        public SubjectRecord(string teacherSH, string subjectSH, string courseSH)
        {
            TeacherSH = teacherSH;
            SubjectSH = subjectSH;
            CourseSH = courseSH;
            ID = -1;
        }

        public string TeacherSH { get; } 
        public string SubjectSH { get; }
        public string? CourseSH { get; }

        public int ID { get; }

        public bool IsChange => ID < 0;

        public override bool Equals(object? obj)
        {
            if (obj is SubjectRecord other)
            {
                return other.TeacherSH == TeacherSH && other.SubjectSH == SubjectSH;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class Lesson
    {
        public Lesson(XElement lessonRoot, Class @class)
        {
            hash = lessonRoot.Hash();

            ClassName = @class.Name;
            int periodID = int.Parse(lessonRoot.Element("St")!.Value);
            Period = @class.PeriodTimes.ElementAtOrDefault(periodID);
            int subjectID = int.Parse(lessonRoot.Element("Nr")?.Value ?? "-1");
            DefaultSubject = @class.Subjects.GetValueOrDefault(subjectID);
            if (lessonRoot.Element("Ku2") != null)
            {
                Subject = new(lessonRoot.Element("Le")!.Value, DefaultSubject?.SubjectSH ?? "", lessonRoot.Element("Ku2")!.Value);
            } else
            {
                Subject = new(lessonRoot.Element("Le")!.Value, lessonRoot.Element("Fa")!.Value);
            }
            
            Room = lessonRoot.Element("Ra")!.Value;
            Info = string.IsNullOrWhiteSpace(lessonRoot.Element("If")!.Value) ? null : lessonRoot.Element("If")!.Value;

            if (lessonRoot.Element("Fa")!.Attribute("FaAe") != null) Changes |= Changes.Subject;
            if (lessonRoot.Element("Le")!.Attribute("LeAe") != null) Changes |= Changes.Teacher;
            if (lessonRoot.Element("Ra")!.Attribute("RaAe") != null) Changes |= Changes.Room;

            if (Changes.HasFlag(Changes.Subject | Changes.Teacher | Changes.Room))
            {
                if (lessonRoot.Element("Fa")!.Value == "---" && lessonRoot.Element("Le")!.Value == "" && lessonRoot.Element("Ra")!.Value == "") Changes |= Changes.Cancelled;
            }

        }

        public static string GetID(XElement lessonRoot)
        {
            return lessonRoot.Element("St")!.Value + "|" + int.Parse(lessonRoot.Element("Nr")!.Value);
        }

        public bool ContainsChanges(XElement newLRoot)
        {
            return newLRoot.Hash() != hash;
        }

        public void UpdateIfContainsChanges(XElement newLRoot, Class @class)
        {
            if (ContainsChanges(newLRoot))
            {
                int periodID = int.Parse(newLRoot.Element("St")!.Value);
                Period = @class.PeriodTimes.ElementAtOrDefault(periodID);
                int subjectID = int.Parse(newLRoot.Element("Nr")!.Value);
                DefaultSubject = @class.Subjects.GetValueOrDefault(subjectID);
                if (newLRoot.Element("Ku2") != null)
                {
                    Subject = new(newLRoot.Element("Le")!.Value, DefaultSubject?.SubjectSH ?? "", newLRoot.Element("Ku2")!.Value);
                }
                else
                {
                    Subject = new(newLRoot.Element("Le")!.Value, newLRoot.Element("Fa")!.Value);
                }

                Room = newLRoot.Element("Ra")!.Value;
                Info = string.IsNullOrWhiteSpace(newLRoot.Element("If")!.Value) ? null : newLRoot.Element("If")!.Value;

                if (newLRoot.Element("Fa")!.Attribute("FaAe") != null) Changes |= Changes.Subject;
                if (newLRoot.Element("Le")!.Attribute("FaAe") != null) Changes |= Changes.Teacher;
                if (newLRoot.Element("Ra")!.Attribute("FaAe") != null) Changes |= Changes.Room;

                if (Changes.HasFlag(Changes.Subject | Changes.Teacher | Changes.Room))
                {
                    if (newLRoot.Element("Fa")!.Value == "---" && newLRoot.Element("Le")!.Value == "" && newLRoot.Element("Ra")!.Value == "") Changes |= Changes.Cancelled;
                }
                hash = newLRoot.Hash();

                DataChanged?.Invoke(this);
            }
        }

        public string ClassName { get; }
        public PeriodTime? Period { get; private set; }
        public SubjectRecord? DefaultSubject { get; private set; }
        public SubjectRecord Subject { get; private set; }
        public string Room { get; private set; }
        public string? Info { get; private set; }

        private byte[] hash;

        public string ID => Period?.Id + "|" + DefaultSubject?.ID;

        public bool IsChanged => Subject != DefaultSubject;

        public Changes Changes { get; private set; } = Changes.None;

        public event Action<Lesson>? DataChanged;
    }

    [Flags]
    public enum Changes
    {
        None = 0,
        Teacher = 1 << 0,
        Subject = 1 << 1,
        Room = 1 << 2,
        Cancelled = 1 << 3,
    }
}
