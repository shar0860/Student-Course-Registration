using AcademicManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

public class RegistrationModel : PageModel
{
    public List<Student> Students { get; set; } = new List<Student>();
    public List<SelectListItem> StudentList { get; set; }

    [BindProperty]
    public string? SelectedStudentId { get; set; } = string.Empty;

    public List<Course> Courses { get; set; } = new List<Course>(); 
    public List<(string CourseCode, string CourseTitle, string? Grade)> RegisteredCourses { get; set; } = new List<(string, string, string?)>();

    [BindProperty]
    public List<string> SelectedCourses { get; set; } = new List<string>(); 

    [BindProperty]
    public Dictionary<string, string?> CourseGrades { get; set; } = new Dictionary<string, string?>(); 

    [BindProperty]
    public string? SortColumn { get; set; } = string.Empty; 

    [BindProperty]
    public string? SortOrder { get; set; } = string.Empty; 

    public string ErrorMessage { get; set; } = string.Empty; 
    public string SuccessMessage { get; set; } = string.Empty; 

    public void OnGet()
    {
        LoadStudents();
    }

    public IActionResult OnPostStudentSelected()
    {
        if (string.IsNullOrEmpty(SelectedStudentId))
        {
            ErrorMessage = "Please select a student.";
            LoadStudents();
            return Page();
        }

        var academicRecords = DataAccess.GetAcademicRecordsByStudentId(SelectedStudentId);

        if (academicRecords == null || academicRecords.Count == 0)
        {
            
            Courses = DataAccess.GetAllCourses();
            RegisteredCourses = null; 
            ErrorMessage = null; 
        }
        else
        {
           
            Courses = DataAccess.GetAllCourses();

            foreach (var record in academicRecords)
            {
                var course = Courses.FirstOrDefault(c => c.CourseCode == record.CourseCode);
                if (course != null)
                {
                    string? grade = (record.Grade == default(double) || record.Grade == -100.00) ? null : record.Grade.ToString("F2");
                    RegisteredCourses.Add((course.CourseCode, course.CourseTitle, grade));
                    CourseGrades[course.CourseCode] = grade ?? "";
                }
            }

            RegisteredCourses = SortRegisteredCourses(RegisteredCourses, SortColumn, SortOrder);
        }

        LoadStudents();
        return Page();
    }

    public IActionResult OnPostRegister()
    {
        if (string.IsNullOrEmpty(SelectedStudentId))
        {
            ErrorMessage = "Please select a student.";
            LoadStudents();
            return Page();
        }

        if (SelectedCourses == null || SelectedCourses.Count == 0)
        {
            ErrorMessage = "Please select at least one course.";
            LoadStudents();
            Courses = DataAccess.GetAllCourses();
            return Page();
        }

        foreach (var courseCode in SelectedCourses)
        {
            var academicRecord = new AcademicRecord(SelectedStudentId, courseCode);
            DataAccess.AddAcademicRecord(academicRecord);
        }

        SuccessMessage = "Registration successful!";
        return OnPostStudentSelected(); 
    }

    public IActionResult OnPostSubmitGrades()
    {
        if (string.IsNullOrEmpty(SelectedStudentId))
        {
            ErrorMessage = "Please select a student.";
            LoadStudents();
            return Page();
        }

        
        foreach (var entry in CourseGrades)
        {
            var courseCode = entry.Key;
            var grade = entry.Value;

            if (!string.IsNullOrEmpty(grade))
            {
                if (double.TryParse(grade, out double gradeValue))
                {
                    SaveGradeForCourse(SelectedStudentId, courseCode, gradeValue);
                }
            }
        }

        SuccessMessage = "Grades successfully updated!";
        return OnPostStudentSelected(); 
    }

    private void LoadStudents()
    {
        Students = DataAccess.GetAllStudents();
        StudentList = Students.Select(s => new SelectListItem
        {
            Value = s.StudentId,
            Text = s.StudentName
        }).ToList();
    }

    private List<(string CourseCode, string CourseTitle, string? Grade)> SortRegisteredCourses(
        List<(string CourseCode, string CourseTitle, string? Grade)> courses,
        string? sortColumn,
        string? sortOrder)
    {
        if (string.IsNullOrEmpty(sortColumn)) return courses;

        if (sortColumn == "CourseCode")
        {
            return sortOrder == "asc" ? courses.OrderBy(c => c.CourseCode).ToList() : courses.OrderByDescending(c => c.CourseCode).ToList();
        }
        if (sortColumn == "CourseTitle")
        {
            return sortOrder == "asc" ? courses.OrderBy(c => c.CourseTitle).ToList() : courses.OrderByDescending(c => c.CourseTitle).ToList();
        }
        if (sortColumn == "Grades")
        {
            return sortOrder == "asc" ? courses.OrderBy(c => double.TryParse(c.Grade, out var g) ? g : 0).ToList() :
                                        courses.OrderByDescending(c => double.TryParse(c.Grade, out var g) ? g : 0).ToList();
        }

        return courses;
    }

    private void SaveGradeForCourse(string studentId, string courseCode, double gradeValue)
    {
        var academicRecords = DataAccess.GetAcademicRecordsByStudentId(studentId);
        foreach (var record in academicRecords)
        {
            if (record.CourseCode == courseCode)
            {
                record.Grade = gradeValue;
            }
        }
    }
}
