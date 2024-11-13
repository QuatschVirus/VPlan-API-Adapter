using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace VPlan_API_Adapter
{
    public class VPlan
    {
        #region --- Management Stuff ---

        private DateTime lastPulled;
        private readonly DateOnly referenceDate;
        private readonly TimeSpan expiration;

        private readonly string fullBaseUrl;
        private readonly List<string> cachedClasses;
        private readonly List<string> cachedTeachers;
        private readonly List<string> cachedRooms;

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
            HttpClient c = new()
            {
                BaseAddress = new(fullBaseUrl + referenceDate.ToString("yyyyMMdd") + ".xml"),
            };

            var res = c.GetAsync("").Result;
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

        public VPlan(DateOnly refDate, TimeSpan expiration, string url, string username, string password, List<string> cachedClasses, List<string> cachedTeachers, List<string> cachedRooms)
        {
            referenceDate = refDate;
            fullBaseUrl = $"https://https://{username}:{password}@{url}";
            this.cachedClasses = cachedClasses;
            this.cachedTeachers = cachedTeachers;
            this.cachedRooms = cachedRooms;
            this.expiration = expiration;
        }

        #endregion

        #region --- Plan Data ---

        private void DataUpdate(XElement root)
        {
            TimeStamp = DateTime.Parse(root.Element("Kopf")!.Element("Zeitstempel")!.Value);
            OffDays = root.Element("FreieTage")!.Elements()!.Select(e => DateOnly.ParseExact(e.Value, "yyMMdd")).ToList();
            Infos = root.Element("ZusatzInfo")?.Elements()?.Select(e => e.Value).ToList() ?? [];

            classes = root.Element("Klassen")!.Elements().Where(e => cachedClasses.Contains(e.Element("Kurz")!.Value)).Select(e => new Class(e)).ToDictionary(c => c.Name);
            
            
        }

        public DateTime? TimeStamp { get; private set; }
        public List<DateOnly>? OffDays { get; private set; }
        public List<string>? Infos { get; private set; }


        public int? DaysPerWeek { get; private set; }

        private Dictionary<string, Class> classes = [];
        private Dictionary<string, 

        public Class? GetClass(string name, bool allowUpdates)
        {
            if (!UpdateDataIfExpired()) return null;
            if (classes.TryGetValue(name, out var c)) return c;
            else
            {
                if (allowUpdates)
                {
                    cachedClasses.Add(name);
                    UpdateData();
                    return GetClass(name, false);
                }
            }
            return null;
        }

        #endregion
    }

    public class Class
    {
        public Class(XElement classRoot)
        {
            Name = classRoot.Element("Kurz")!.Value;
            PeriodTimes = classRoot.Element("KlStunden")!.Elements().Select(e => new PeriodTime(e)).ToList();
            Subjects = classRoot.Element("Unterricht")!.Elements().Select(e => new SubjectRecord(e.Element("UeNr")!)).ToDictionary(s => s.ID);
            Lessons = classRoot.Element("Pl")!.Elements().Select(e => new Lesson(e, this)).ToList();
        }

        public string Name { get; }
        public IReadOnlyList<PeriodTime> PeriodTimes { get; }
        public IReadOnlyDictionary<int, SubjectRecord> Subjects { get; }
        public IReadOnlyList<Lesson> Lessons { get; }
    }

    public class Teacher {
        public Teacher(XElement classesRoot) {
            
        }

        public string ShortHand { get; }
        public IReadOnlyList<Lesson> Lessons { get; }
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
            int periodID = int.Parse(lessonRoot.Element("St")!.Value);
            Period = (@class.PeriodTimes.Count >= periodID) ? @class.PeriodTimes[periodID] : null;
            int subjectID = int.Parse(lessonRoot.Element("Nr")!.Value);
            DefaultSubject = @class.Subjects.ContainsKey(subjectID) ? @class.Subjects[subjectID] : null;
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
            if (lessonRoot.Element("Le")!.Attribute("FaAe") != null) Changes |= Changes.Teacher;
            if (lessonRoot.Element("Ra")!.Attribute("FaAe") != null) Changes |= Changes.Room;

            if (Changes.HasFlag(Changes.Subject | Changes.Teacher | Changes.Room))
            {
                if (lessonRoot.Element("Fa")!.Value == "---" && lessonRoot.Element("Le")!.Value == "" && lessonRoot.Element("Ra")!.Value == "") Changes |= Changes.Cancelled;
            }

        }

        public PeriodTime? Period { get; }
        public SubjectRecord? DefaultSubject { get; }
        public SubjectRecord Subject { get; }
        public string Room { get; }
        public string? Info { get; }

        public bool IsChanged => Subject != DefaultSubject;

        public Changes Changes { get; } = Changes.None;
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
