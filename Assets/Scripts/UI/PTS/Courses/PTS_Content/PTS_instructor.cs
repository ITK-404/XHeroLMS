using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class PTS_Instructor : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text txtInstructorName;
    [SerializeField] private TMP_Text txtLearners;
    [SerializeField] private TMP_Text txtTotalCourses;
    [SerializeField] private TMP_Text txtDescription;

    [Header("Options")]
    [SerializeField] private bool formatNumbers = true;
    [SerializeField] private bool tidyDescription = true;

    void OnEnable()
    {
        CourseDetailStaticStore.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        CourseDetailStaticStore.OnChanged -= Refresh;
    }

    void Refresh()
    {
        if (!CourseDetailStaticStore.HasData) return;

        var course = CourseDetailStaticStore.CurrentCourse;
        if (course == null || course.instructor == null) return;

        var ins = course.instructor;

        if (txtInstructorName != null)
            txtInstructorName.text = ins.fullName ?? "";

        if (txtLearners != null)
            txtLearners.text = FormatNumber(ins.learners) + " học viên";

        if (txtTotalCourses != null)
            txtTotalCourses.text = FormatNumber(ins.courses) + " khóa học";

        if (txtDescription != null)
        {
            string plain = HtmlToPlainTextWithNewlines(ins.description);
            if (tidyDescription)
                plain = TidyLines(plain);

            txtDescription.text = plain;
        }
    }

    // =========================
    // HTML -> Plain text
    // =========================
    static string HtmlToPlainTextWithNewlines(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";

        string s = html;

        s = Regex.Replace(s, @"<\s*br\s*/?\s*>", "\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<\s*/?\s*(p|div|li)\b[^>]*>", "\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<\s*/?\s*(ul|ol)\b[^>]*>", "\n", RegexOptions.IgnoreCase);

        s = Regex.Replace(s, @"<[^>]+>", "");

        s = DecodeHtmlEntities(s);

        s = s.Replace("\r\n", "\n").Replace("\r", "\n");

        return s;
    }

    static string DecodeHtmlEntities(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        s = s.Replace("&nbsp;", " ");
        s = s.Replace("&amp;", "&");
        s = s.Replace("&lt;", "<");
        s = s.Replace("&gt;", ">");
        s = s.Replace("&quot;", "\"");
        s = s.Replace("&#39;", "'");

        return s;
    }

    static string TidyLines(string s)
    {
        var lines = s.Split('\n');

        for (int i = 0; i < lines.Length; i++)
            lines[i] = lines[i].Trim();

        string joined = string.Join("\n", lines);

        joined = Regex.Replace(joined, @"\n{3,}", "\n\n");

        return joined.Trim();
    }

    string FormatNumber(int number)
    {
        if (!formatNumbers) return number.ToString();
        return number.ToString("N0");
    }
}