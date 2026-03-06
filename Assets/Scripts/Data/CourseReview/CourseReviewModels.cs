using System;
using System.Collections.Generic;

[Serializable]
public class CourseReviewApiResponse
{
    public bool status;
    public List<LmsCourseReviewItem> data;
    public ReviewStatistics statistics;
}

[Serializable]
public class LmsCourseReviewItem
{
    public string _id;
    public List<string> files;
    public bool isActive;
    public bool isPrioritize;
    public string content;
    public int stars;
    public ReviewAuthor author;
    public string courseId;
    public string createdAt;
    public string updatedAt;
}

[Serializable]
public class ReviewAuthor
{
    public string _id;
    public string fullName;
    public string avatar;
}

[Serializable]
public class ReviewStatistics
{
    public int total;
    public float rate;
    public ReviewStarCounts starCounts;
}

[Serializable]
public class ReviewStarCounts
{
    public int _1;
    public int _2;
    public int _3;
    public int _4;
    public int _5;
}